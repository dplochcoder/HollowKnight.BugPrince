using BugPrince.Data;
using BugPrince.Rando;
using BugPrince.UI;
using BugPrince.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerMod.IC;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BugPrince.IC;

public class BugPrinceModule : ItemChanger.Modules.Module
{
    // Setup
    public RandomizationSettings Settings = new();
    public Dictionary<string, CostGroup> CostGroups = [];
    public Dictionary<string, string> CostGroupsByScene = [];
    public HashSet<string> RandomizedTransitions = [];

    // Inventory
    public int Coins = 0;
    public int DiceTotems = 0;
    public int Gems = 0;
    public int PushPins = 0;
    public string? PinnedScene;

    // Progression
    public HashSet<Transition> ResolvedEnteredTransitions = [];
    public HashSet<Transition> ResolvedExitedTransitions = [];
    public HashSet<string> PaidCostGroups = [];  // Cost groups already purchased. This set forms an unordered prefix of CostGroupProgression.
    public List<string> CostGroupProgression = [];  // List of all cost groups, in progression order.
    public Dictionary<string, int> RefreshCounters = [];  // Scene -> num picks until refresh

    public static BugPrinceModule Get() => ItemChangerMod.Modules.Get<BugPrinceModule>()!;

    public override void Initialize()
    {
        Events.OnEnterGame += DoLateInitialization;

        BugPrinceMod.StartDebugLog();
    }

    public override void Unload()
    {
        Events.OnEnterGame -= DoLateInitialization;
        On.GameManager.BeginSceneTransition -= SelectRandomizedTransition;
    }

    private static RandomizerSettings RS() => RandomizerMod.RandomizerMod.RS;

    private static RandoModContext RandoCtx() => RS().Context;

    private static TransitionSettings TransitionSettings() => RandomizerMod.RandomizerMod.RS.GenerationSettings.TransitionSettings;

    private static Dictionary<Transition, ITransition> TransitionOverrides() => ItemChanger.Internal.Ref.Settings.TransitionOverrides;

    private ICostGroupProgressionProvider? cachedProvider;
    internal ICostGroupProgressionProvider AsProgressionProvider() => cachedProvider ??= new ModuleCostGroupProgressionProvider(this);

    private bool GetCostGroupByScene(string scene, out string groupName, out CostGroup costGroup) => AsProgressionProvider().GetCostGroupByScene(scene, out groupName, out costGroup);

    private void DoLateInitialization()
    {
        RandoInterop.LS = null;

        // Must hook after ItemChanger.
        On.GameManager.BeginSceneTransition += SelectRandomizedTransition;

        UpdateTransitionPlacements();
        RandomizerMod.RandomizerMod.RS.TrackerData.Reset();
        RandomizerMod.RandomizerMod.RS.TrackerDataWithoutSequenceBreaks.Reset();
    }

    private void PayCosts(string scene)
    {
        if (!GetCostGroupByScene(scene, out var groupName, out var group)) return;
        if (PaidCostGroups.Contains(groupName)) return;

        if (CostGroupProgression[PaidCostGroups.Count] != groupName)
        {
            // Reorder progression.
            CostGroupProgression.Remove(groupName);
            CostGroupProgression.Insert(PaidCostGroups.Count, groupName);

            // This technically speaking can break logic because a shop previously accessible can become inaccessible after progression order changes.
            // However this should be invisible to the player (and tracker in general) because it only affects shops the player hasn't yet visited, which are hidden behind unexplored transitions.
            // However however it does mean that a shop previously available in the purchase pool may disappear from it for some time, until the player buys other shops necessary to pay the newly increased progression cost.
            cachedProvider = null;  // Reset logic variable caches.
        }

        if (group.Type == CostType.Coins) Coins -= group.Cost;
        else Gems -= group.Cost;
        PaidCostGroups.Add(groupName);
    }

    private bool IsExitOnly(Transition transition) => !TransitionOverrides().ContainsKey(transition);

    private static bool IsMatchingPair(Transition src, Transition dst, bool doorToDoor)
    {
        var a = src.GetDirection();
        var b = dst.GetDirection();
        if (a == GateDirection.Door && b == GateDirection.Door) return doorToDoor;
        else if (b == GateDirection.Door) return a == GateDirection.Left || b == GateDirection.Right;
        else if (a == GateDirection.Door) return b == GateDirection.Left || b == GateDirection.Right;
        else return a == b.Opposite();
    }

    private bool IsValidSwap(Transition src1, Transition dst1, Transition src2, Transition dst2)
    {
        if (src1 == src2 && dst1 == dst2) return true;
        if (IsExitOnly(dst1) != IsExitOnly(dst2)) return false;

        var matchingMode = TransitionSettings().TransitionMatching;
        bool matching = matchingMode != RandomizerMod.Settings.TransitionSettings.TransitionMatchingSetting.NonmatchingDirections;
        bool doorToDoor = matchingMode != RandomizerMod.Settings.TransitionSettings.TransitionMatchingSetting.MatchingDirectionsAndNoDoorToDoor;

        if (!matching) return true;
        if (!IsMatchingPair(src1, dst2, doorToDoor)) return false;
        if (!IsMatchingPair(src2, dst1, doorToDoor)) return false;
        return true;
    }

    private static bool IsCompletable(ProgressionManager pm)
    {
        List<ItemPlacement> unclaimed = [.. RandoCtx().itemPlacements];

        // TODO: Ensure all transitions are reachable as well.
        while (unclaimed.Count > 0)
        {
            List<ItemPlacement> reachable = [];
            List<ItemPlacement> newUnclaimed = [];
            foreach (var placement in unclaimed)
            {
                if (placement.Location.CanGet(pm)) reachable.Add(placement);
                else newUnclaimed.Add(placement);
            }
            if (reachable.Count == 0) return false;

            pm.mu.StopUpdating();
            foreach (var placement in reachable) pm.Add(placement.Item, placement.Location);
            pm.mu.StartUpdating();

            unclaimed = newUnclaimed;
        }

        return true;
    }

    private bool CanSwapTransitions(
        Transition src1, Transition dst1, Transition src2, Transition dst2)
    {
        if (src1 == src2 && dst1 == dst2) return true;

        var ctx = RandoCtx();
        var lm = ctx.LM;

        Action? onDone = null;
        if (GetCostGroupByScene(dst1.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName) && CostGroupProgression[PaidCostGroups.Count] != groupName)
        {
            // Check that we can bump this shop forward in progression order.
            if (!lm.VariableResolver.TryGetInner<BugPrinceVariableResolver>(out var inner)) throw new ArgumentException("Missing CostGroupVariableResolver");

            List<string> reordered = [.. CostGroupProgression];
            reordered.Remove(groupName);
            reordered.Insert(PaidCostGroups.Count, groupName);

            OverlaidCostGroupProgressionProvider provider = new(AsProgressionProvider(), reordered);
            inner.OverrideProgressionProvider(provider);
            onDone = () => inner.OverrideProgressionProvider(null);
        }

        try
        {
            ProgressionManager pm = new(lm, ctx);
            var mu = pm.mu;

            mu.AddWaypoints(lm.Waypoints);
            mu.AddTransitions(lm.TransitionLookup.Values);
            mu.AddPlacements(ctx.Vanilla);

            List<UpdateEntry> updates = [];
            var coupled = TransitionSettings().Coupled;
            foreach (var e in TransitionOverrides())
            {
                var src = e.Key;
                var dst = e.Value.ToStruct();

                if (src == src1) dst = dst2;
                else if (src == src2) dst = dst1;
                else if (coupled)
                {
                    if (src == dst1) dst = src2;
                    else if (src == dst2) dst = src1;
                }

                TransitionPlacement t = new()
                {
                    Source = RandoTransition(src),
                    Target = RandoTransition(dst),
                };
                updates.Add(new PrePlacedItemUpdateEntry(t));
            }
            mu.AddEntries(updates);

            mu.StartUpdating();
            mu.SetLongTermRevertPoint();

            return IsCompletable(pm);
        }
        finally { onDone?.Invoke(); }
    }


    private void RecordEnterExit(Transition src, Transition dst)
    {
        ResolvedEnteredTransitions.Add(src);
        ResolvedExitedTransitions.Add(dst);
    }

    private static readonly FieldInfo trackerUpdateTransitionLookupField = typeof(TrackerUpdate).GetField("transitionLookup", BindingFlags.NonPublic | BindingFlags.Instance);

    private Dictionary<Transition, int>? srcTransitionIndices;
    private int SrcTransitionIndex(Transition transition)
    {
        if (srcTransitionIndices == null)
        {
            srcTransitionIndices = [];
            var ctx = RandoCtx();
            for (int i = 0; i < ctx.transitionPlacements.Count; i++) srcTransitionIndices[ctx.transitionPlacements[i].Source.ToStruct()] = i;
        }

        return srcTransitionIndices[transition];
    }

    private Dictionary<Transition, RandoModTransition>? randoTransitions;
    private RandoModTransition RandoTransition(Transition transition)
    {
        if (randoTransitions == null)
        {
            randoTransitions = [];
            var ctx = RandoCtx();

            // Targets take priority.
            foreach (var p in ctx.transitionPlacements) randoTransitions[p.Source.ToStruct()] = p.Source;
            foreach (var p in ctx.transitionPlacements) randoTransitions[p.Target.ToStruct()] = p.Target;
        }

        return randoTransitions[transition];
    }

    private void UpdateTransitionPlacements(List<Transition>? srcs = null)
    {
        var overrides = TransitionOverrides();
        var ctx = RandoCtx();

        var trackerUpdate = ItemChangerMod.Modules.Get<TrackerUpdate>();
        var transitionLookup = (Dictionary<string, string>)trackerUpdateTransitionLookupField.GetValue(trackerUpdate);

        if (srcs != null)
        {
            foreach (var src in srcs)
            {
                var idx = SrcTransitionIndex(src);
                var dst = overrides[src].ToStruct();

                ctx.transitionPlacements[idx] = new(RandoTransition(dst), RandoTransition(src));
                transitionLookup[src.ToString()] = dst.ToString();
            }
        }
        else
        {
            List<TransitionPlacement> updatedPlacements = [];
            transitionLookup.Clear();
            foreach (var p in ctx.transitionPlacements)
            {
                var src = p.Source;
                var target = RandoTransition(overrides[p.Source.ToStruct()].ToStruct());
                updatedPlacements.Add(new(target, p.Source));
                transitionLookup[src.Name] = target.Name;
            }
            ctx.transitionPlacements.Clear();
            ctx.transitionPlacements.AddRange(updatedPlacements);
        }
    }

    private void SwapTransitions(Transition src1, Transition src2)
    {
        var overrides = TransitionOverrides();
        var dst1 = overrides[src1].ToStruct();
        var dst2 = overrides[src2].ToStruct();
        RecordEnterExit(src1, dst2);
        (overrides[src1], overrides[src2]) = (overrides[src2], overrides[src1]);

        List<Transition> toUpdate = [src1, src2];
        if (TransitionSettings().Coupled)
        {
            if (overrides.ContainsKey(dst1))
            {
                overrides[dst1] = src2;
                toUpdate.Add(dst1);
            }
            if (overrides.ContainsKey(dst2))
            {
                overrides[dst2] = src1;
                RecordEnterExit(dst2, src1);
                toUpdate.Add(dst2);
            }
        }

        UpdateTransitionPlacements(toUpdate);
    }

    private bool FindTarget(string scene, string gate, out (Transition, Transition) pair)
    {
        Transition target = new(scene, gate);

        foreach (var e in TransitionOverrides())
        {
            if (e.Value.ToStruct() == target)
            {
                pair = (e.Key, target);
                return true;
            }
        }
        return false;
    }

    private List<SceneChoiceInfo>? CalculateSceneChoices(Transition src, Transition target, List<SceneChoiceInfo>? previous = null)
    {
        // Decrement refresh counters
        Dictionary<string, int> newDict = [];
        foreach (var e in RefreshCounters) newDict[e.Key] = e.Value - 1;
        RefreshCounters = newDict;

        List<(Transition, Transition)> potentialTargets = [];
        foreach (var e in ItemChanger.Internal.Ref.Settings.TransitionOverrides)
        {
            var cSrc = e.Key;
            Transition cDst = e.Value.ToStruct();

            if (ResolvedEnteredTransitions.Contains(cSrc)) continue;
            if (ResolvedExitedTransitions.Contains(cDst)) continue;
            if (!IsValidSwap(src, target, cSrc, cDst)) continue;

            potentialTargets.Add((cSrc, cDst));
        }

        potentialTargets.Shuffle(new());  // FIXME: Seed

        SortedDictionary<int, List<(Transition, Transition)>> backfillDict = [];
        SortedDictionary<int, List<(Transition, Transition)>> shopBackfillDict = [];
        List<(Transition, Transition)> pinBackfill = [];
        bool haveShop = false;
        Wrapped<bool> startedBackfill = new(false);
        Wrapped<bool> startedShopBackfill = new(false);

        IEnumerator<(Transition, Transition)> EnumerateCandidates()
        {
#if DEBUG
            if (FindTarget(SceneNames.Room_shop, "left1", out var pair)) yield return pair;
            if (FindTarget(SceneNames.Room_temple, "left1", out pair)) yield return pair;
            if (FindTarget(SceneNames.Deepnest_Spider_Town, "left1", out pair)) yield return pair;
#endif
            foreach (var p in potentialTargets) yield return p;
            startedBackfill.Value = true;
            foreach (var list in backfillDict.Values) foreach (var p in list) yield return p;
            startedShopBackfill.Value = true;
            foreach (var list in shopBackfillDict.Values) foreach (var p in list) yield return p;
        };
        var iter = EnumerateCandidates();

        List<SceneChoiceInfo> choices = [];
        HashSet<string> chosenScenes = [];
        // FIXME: Handle previous for rerolls
        Wrapped<int> illogicalSwaps = new(0);
        bool MaybeAdd(Transition newSrc, Transition newTarget)
        {
            if (!CanSwapTransitions(src, target, newSrc, newTarget)) { illogicalSwaps.Value++; return false; }

            SceneChoiceInfo info = new()
            {
                OrigSrc = newSrc,
                Pinned = newTarget.SceneName == PinnedScene,
                Target = newTarget
            };
            if (GetCostGroupByScene(newTarget.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName)) info.Cost = (group.Type, group.Cost);

            choices.Add(info);
            chosenScenes.Add(newTarget.SceneName);
            return true;
        }

        int dupeScenes = 0;
        int backfills = 0;
        int firstPassRemaining = potentialTargets.Count;
        while (choices.Count < Settings.NumChoices && iter.MoveNext())
        {
            if (firstPassRemaining > 0) --firstPassRemaining;
            var (newSrc, newTarget) = iter.Current;

            if (newTarget.SceneName == PinnedScene)
            {
                pinBackfill.Add((newSrc, newTarget));
                continue;
            }
            if (chosenScenes.Contains(newTarget.SceneName)) { ++dupeScenes; continue; }

            bool isShop = GetCostGroupByScene(newTarget.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName);
            if (!RefreshCounters.TryGetValue(newTarget.SceneName, out int refreshCount)) refreshCount = 0;

            bool isRedundantShop = haveShop && isShop;
            if ((refreshCount == 0 || startedBackfill.Value) && (!isRedundantShop || startedShopBackfill.Value))
            {
                if (MaybeAdd(newSrc, newTarget) && isShop) haveShop = true;
            }
            else
            {
                ++backfills;
                var backfill = startedBackfill.Value ? backfillDict : shopBackfillDict;
                backfill.GetOrAddNew(refreshCount).Add((newSrc, newTarget));
            }
        }

        // Sort the shops last.
        choices.StableSort((c1, c2) =>
        {
            var shop1 = c1.Cost.HasValue;
            var shop2 = c2.Cost.HasValue;

            if (shop1 == shop2) return 0;
            else return shop2 ? -1 : 1;
        });

        // Reset refresh count for all selected choices.
        foreach (var choice in choices) RefreshCounters[choice.Target.SceneName] = Settings.RefreshCycle;
        // Add the pinned scene if we can.
        foreach (var (newSrc, newTarget) in pinBackfill) if (MaybeAdd(newSrc, newTarget)) break;

        BugPrinceMod.DebugLog($"CALCULATE_SCENE_CHOICES: (illogicalSwaps={illogicalSwaps.Value}, dupeScenes={dupeScenes}, backfills={backfills}, remaining={firstPassRemaining})");

        // TODO: Compare to previous.
        if (choices.Count == 0) throw new ArgumentException($"BugPrince mod found no viable choices for {src} -> {target}");
        return choices;
    }

    private void MaybeSelectNewPin(string? scene)
    {
        if (scene == null || PinnedScene != null || PushPins < 1) return;

        PinnedScene = scene;
        PushPins--;
    }

    private void SelectRandomizedTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
    {
        if (RoomSelectionUI.uiPresent) return;

        if (!TransitionInferenceUtil.GetSrcTarget(self, info, out var src, out var target) || ResolvedEnteredTransitions.Contains(src))
        {
            orig(self, info);
            return;
        }

        var choices = CalculateSceneChoices(src, target)!;
        Wrapped<RoomSelectionUI?> wrapped = new(null);
        wrapped.Value = RoomSelectionUI.Create(
            this,
            src.GetDirection(),
            choices,
            decision =>
            {
                if (decision.chosen is not SceneChoiceInfo choice)
                {
                    // FIXME: Rejection.
                    UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                    return;
                }

                MaybeSelectNewPin(decision.newPin?.Target.SceneName);
                PayCosts(choice.Target.SceneName);
                SwapTransitions(src, choice.OrigSrc);

                UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                orig(self, info);
            },
            decision =>
            {
                MaybeSelectNewPin(decision.newPin?.Target.SceneName);
                return CalculateSceneChoices(src, target, choices);
            });
    }
}

internal class ModuleCostGroupProgressionProvider : ICostGroupProgressionProvider
{
    private readonly BugPrinceModule module;

    internal ModuleCostGroupProgressionProvider(BugPrinceModule module) => this.module = module;

    public IReadOnlyDictionary<string, CostGroup> CostGroups() => module.CostGroups;

    public IReadOnlyDictionary<string, string> CostGroupsByScene() => module.CostGroupsByScene;

    public bool IsRandomizedTransition(string transition) => module.RandomizedTransitions.Contains(transition);

    public IReadOnlyList<string> CostGroupProgression() => module.CostGroupProgression;
}
