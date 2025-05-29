using BugPrince.Data;
using BugPrince.Rando;
using BugPrince.UI;
using BugPrince.Util;
using ItemChanger;
using Newtonsoft.Json;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerMod.IC;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
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

    // Interop.
    [JsonConverter(typeof(Transition.TransitionDictConverter<Transition>))]
    public Dictionary<Transition, Transition> UnsyncedRandoPlacements = [];

    // Caches.
    private readonly Dictionary<Transition, RandoModTransition> sourceRandoTransitions = [];
    private readonly Dictionary<Transition, RandoModTransition> targetRandoTransitions = [];

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

    private static List<TransitionPlacement> RandoTransitionPlacements() => RandoCtx().transitionPlacements;

    private static TransitionSettings TransitionSettings() => RandomizerMod.RandomizerMod.RS.GenerationSettings.TransitionSettings;

    private ICostGroupProgressionProvider? cachedProvider;
    internal ICostGroupProgressionProvider AsProgressionProvider() => cachedProvider ??= new ModuleCostGroupProgressionProvider(this);

    private bool GetCostGroupByScene(string scene, out string groupName, out CostGroup costGroup) => AsProgressionProvider().GetCostGroupByScene(scene, out groupName, out costGroup);

    private void DoLateInitialization()
    {
        RandoInterop.LS = null;

        // Must hook after ItemChanger.
        On.GameManager.BeginSceneTransition += SelectRandomizedTransition;

        foreach (var p in RandoTransitionPlacements())
        {
            sourceRandoTransitions[p.Source.ToStruct()] = p.Source;
            targetRandoTransitions[p.Target.ToStruct()] = p.Target;
        }

        // Set up rando placement storage.
        if (UnsyncedRandoPlacements.Count == 0) UnsyncedRandoPlacements = RandoTransitionPlacements().ToDictionary(p => p.Source.ToStruct(), p => p.Target.ToStruct());
        else
        {
            SyncTransitionPlacements();
            ResetTrackers();
        }
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

            cachedProvider = null;  // Reset logic variable caches.
        }

        if (group.Type == CostType.Coins) Coins -= group.Cost;
        else Gems -= group.Cost;
        PaidCostGroups.Add(groupName);
    }

    private bool IsExitOnly(Transition transition) => !UnsyncedRandoPlacements.ContainsKey(transition);

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

        // Verify all vanilla and all transitions are reachable.
        foreach (var placement in RandoCtx().Vanilla) if (!placement.Location.CanGet(pm)) return false;
        foreach (var transition in RandoCtx().transitionPlacements) if (!transition.Source.CanGet(pm)) return false;

        return true;
    }

    private static V Get<K, V>(IDictionary<K, V> dict, K key) => dict.TryGetValue(key, out V value) ? value : throw new ArgumentException($"Missing key: {key}");

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
            foreach (var p in RandoTransitionPlacements())
            {
                var source = p.Source.ToStruct();
                var target = p.Target.ToStruct();

                if (source == src1) target = dst2;
                else if (source == src2) target = dst1;
                else if (coupled)
                {
                    if (source == dst1) target = src2;
                    else if (source == dst2) target = src1;
                }

                TransitionPlacement t = new()
                {
                    Source = Get(sourceRandoTransitions, source),
                    Target = Get(targetRandoTransitions, target)
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

    private void SyncTransitionPlacements()
    {
        var placements = RandoTransitionPlacements();
        var trackerUpdate = ItemChangerMod.Modules.Get<TrackerUpdate>();
        var transitionLookup = (Dictionary<string, string>)trackerUpdateTransitionLookupField.GetValue(trackerUpdate);

        // Update tracker and rando context.
        for (int i = 0; i < placements.Count; i++)
        {
            var p = placements[i];
            var source = p.Source.ToStruct();
            var target = p.Target.ToStruct();
            if (UnsyncedRandoPlacements.TryGetValue(source, out var newTarget) && target != newTarget)
            {
                placements[i] = new(p.Source, targetRandoTransitions[newTarget]);
                transitionLookup[source.ToString()] = newTarget.ToString();
            }
        }
    }

    private void SwapTransitions(Transition src1, Transition src2)
    {
        var dst1 = UnsyncedRandoPlacements[src1];
        var dst2 = UnsyncedRandoPlacements[src2];
        UnsyncedRandoPlacements[src1] = dst2;
        UnsyncedRandoPlacements[src2] = dst1;
        RecordEnterExit(src1, dst2);

        // ItemChanger gets updated differently due to things like JijiJinnPassage.
        Dictionary<Transition, Transition> icUpdates = [];
        icUpdates[dst1] = dst2;
        icUpdates[dst2] = dst1;

        if (TransitionSettings().Coupled)
        {
            if (UnsyncedRandoPlacements.ContainsKey(dst1))
            {
                UnsyncedRandoPlacements[dst1] = src2;
                icUpdates[src1] = src2;
            }
            if (UnsyncedRandoPlacements.ContainsKey(dst2))
            {
                UnsyncedRandoPlacements[dst2] = src1;
                RecordEnterExit(dst2, src1);
                icUpdates[src2] = src1;
            }
        }
        SyncTransitionPlacements();

        // Update ItemChanger targets. Map targets specifically to work with JijiJinnPassage.
        List<(Transition, Transition)> newKVs = [];
        var icOverrides = ItemChanger.Internal.Ref.Settings.TransitionOverrides;
        foreach (var e in icOverrides)
        {
            var orig = e.Value.ToStruct();
            if (icUpdates.TryGetValue(orig, out var newTarget) && orig != newTarget) newKVs.Add((e.Key, newTarget));
        }
        foreach (var (k, v) in newKVs) icOverrides[k] = v;

        // Update seed.
        Seed += dst1.ToString().GetStableHashCode() ^ 0x5CCBB22C;
        Seed += dst2.ToString().GetStableHashCode() ^ 0x2FDBD6F4;
    }

    private void ResetTracker(TrackerData tracker)
    {
        List<int> items = [.. tracker.obtainedItems];
        List<(string, string)> transitions = [.. tracker.visitedTransitions.Select(e => (e.Key, e.Value))];

        tracker.obtainedItems.Clear();
        tracker.outOfLogicObtainedItems.Clear();
        tracker.visitedTransitions.Clear();
        tracker.outOfLogicVisitedTransitions.Clear();
        tracker.Reset();

        var itemPlacements = RandoCtx().itemPlacements;
        foreach (var id in items) tracker.OnItemObtained(id, itemPlacements[id].Item.Name, itemPlacements[id].Location.Name);
        foreach (var (src, target) in transitions) tracker.OnTransitionVisited(src, target);
    }

    private void ResetTrackers()
    {
        var rs = RandomizerMod.RandomizerMod.RS;
        ResetTracker(rs.TrackerData);
        ResetTracker(rs.TrackerDataWithoutSequenceBreaks);
    }

    public int Seed = 0;

    private List<SceneChoiceInfo> CalculateSceneChoices(Transition src, Transition target, List<SceneChoiceInfo>? previous = null)
    {
        // Decrement refresh counters
        Dictionary<string, int> newDict = [];
        foreach (var e in RefreshCounters) newDict[e.Key] = e.Value - 1;
        RefreshCounters = newDict;

        List<(Transition, Transition)> potentialTargets = [];
        foreach (var p in RandoTransitionPlacements())
        {
            var cSource = p.Source.ToStruct();
            var cTarget = p.Target.ToStruct();
            if (cSource == src && cTarget == target) continue;

            if (ResolvedEnteredTransitions.Contains(cSource)) continue;
            if (ResolvedExitedTransitions.Contains(cTarget)) continue;
            if (!IsValidSwap(src, target, cSource, cTarget)) continue;

            potentialTargets.Add((cSource, cTarget));
        }

        HashSet<string> tempChosenScenes = [];
        List<(Transition, Transition)> rerollBackfill = [];
        foreach (var info in previous ?? [])
        {
            if (info.Pinned) continue;
            tempChosenScenes.Add(info.Target.SceneName);
            rerollBackfill.Add((info.OrigSrc, info.Target));
        }

        rerollBackfill.Shuffle(new(Seed + 93));
        potentialTargets.Shuffle(new(Seed + 17));

        SortedDictionary<int, List<(Transition, Transition)>> backfillDict = [];
        SortedDictionary<int, List<(Transition, Transition)>> shopBackfillDict = [];
        List<(Transition, Transition)> pinBackfill = [];
        bool haveShop = false;
        Wrapped<bool> startedBackfill = new(false);
        Wrapped<bool> startedShopBackfill = new(false);

        IEnumerator<(Transition, Transition)> EnumerateCandidates()
        {
            yield return (src, target);
            foreach (var p in potentialTargets) yield return p;
            startedBackfill.Value = true;
            foreach (var list in backfillDict.Values) foreach (var p in list) yield return p;
            startedShopBackfill.Value = true;
            foreach (var list in shopBackfillDict.Values) foreach (var p in list) yield return p;
            tempChosenScenes.Clear();
            foreach (var p in rerollBackfill) yield return p;
        };
        var iter = EnumerateCandidates();

        List<SceneChoiceInfo> choices = [];
        HashSet<string> chosenScenes = [];
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
        while (iter.MoveNext())
        {
            if (choices.Count < Settings.NumChoices && firstPassRemaining > 0) --firstPassRemaining;
            var (newSrc, newTarget) = iter.Current;

            if (newTarget.SceneName == PinnedScene)
            {
                pinBackfill.Add((newSrc, newTarget));
                continue;
            }
            else if (choices.Count == Settings.NumChoices) continue;
            if (chosenScenes.Contains(newTarget.SceneName) || tempChosenScenes.Contains(newTarget.SceneName)) { ++dupeScenes; continue; }

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

        // Shuffle to hide the default transition.
        choices.Shuffle(new(Seed + 23));

        // Sort the shops last.
        choices.StableSort((c1, c2) =>
        {
            var shop1 = c1.Cost.HasValue;
            var shop2 = c2.Cost.HasValue;

            if (c1.Cost.HasValue && c2.Cost.HasValue)
            {
                var (t1, v1) = c1.Cost.Value;
                var (t2, v2) = c2.Cost.Value;

                if (v1 != v2) return v1 - v2;
                else return t1.CompareTo(t2);
            }
            else if (!c1.Cost.HasValue && !c2.Cost.HasValue) return 0;
            else return c2.Cost.HasValue ? -1 : 1;
        });

        // Reset refresh count for all selected choices.
        foreach (var choice in choices) RefreshCounters[choice.Target.SceneName] = Settings.RefreshCycle;
        // Add the pinned scene if we can.
        foreach (var (newSrc, newTarget) in pinBackfill) if (MaybeAdd(newSrc, newTarget)) break;

        BugPrinceMod.DebugLog($"CALCULATE_SCENE_CHOICES: (illogicalSwaps={illogicalSwaps.Value}, dupeScenes={dupeScenes}, backfills={backfills}, remaining={firstPassRemaining})");
        return choices;
    }

    private void MaybeSelectNewPin(string? scene)
    {
        if (scene == null || PinnedScene != null || PushPins < 1) return;

        PinnedScene = scene;
        PushPins--;

        Seed += scene.GetStableHashCode() ^ 0x37270AFE;
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
        // FIXME: No choices.

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
                PayCosts(choice.Target.SceneName, out var changedProgressionOrder);
                SwapTransitions(src, choice.OrigSrc);
                ResetTrackers();

                UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                orig(self, info);
            },
            decision =>
            {
                DiceTotems--;
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
