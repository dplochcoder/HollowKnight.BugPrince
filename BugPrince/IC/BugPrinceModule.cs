using BugPrince.Data;
using BugPrince.UI;
using BugPrince.Util;
using ItemChanger;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;

namespace BugPrince.IC;

public class BugPrinceModule : ItemChanger.Modules.Module
{
    // Setup
    public RandomizationSettings Settings = new();
    public List<CostGroup> CostGroups = [];

    // Inventory
    public int Coins = 0;
    public int TotalCoins = 0;
    public int DiceTotems = 0;
    public int Gems = 0;
    public int TotalGems = 0;
    public int PushPins = 0;
    public string? PinnedScene;

    // Progression
    public HashSet<Transition> ResolvedEnteredTransitions = [];
    public HashSet<Transition> ResolvedExitedTransitions = [];
    public HashSet<string> PaidCostGroups = [];
    public Dictionary<string, int> ProgressiveCostGroupCosts = [];
    public Dictionary<string, int> RefreshCounters = [];  // Scene -> num picks until refresh

    // Internal
    private readonly Dictionary<string, CostGroup> costGroupByScene = [];

    public static BugPrinceModule Get() => ItemChangerMod.Modules.Get<BugPrinceModule>()!;

    public override void Initialize()
    {
        Index();

        Events.OnEnterGame += RegisterRoomSelector;
    }

    internal void Index()
    {
        foreach (var costGroup in CostGroups)
        {
            foreach (var scene in costGroup.SceneNames)
            {
                if (costGroupByScene.TryGetValue(scene, out var existingGroup))
                {
                    if (existingGroup.Priority == costGroup.Priority) throw new ArgumentException($"Conflicting cost-groups for scene {scene}, Priority={existingGroup.Priority}");
                    if (existingGroup.Priority < costGroup.Priority) costGroupByScene[scene] = costGroup;
                }
                else costGroupByScene.Add(scene, costGroup);
            }
        }
    }

    public override void Unload()
    {
        Events.OnEnterGame -= RegisterRoomSelector;
        On.GameManager.BeginSceneTransition -= SelectRandomizedTransition;
    }

    private void RegisterRoomSelector() => On.GameManager.BeginSceneTransition += SelectRandomizedTransition;

    private static RandoModContext RandoCtx => RandomizerMod.RandomizerMod.RS.Context;

    private static TransitionSettings TransitionSettings => RandomizerMod.RandomizerMod.RS.GenerationSettings.TransitionSettings;

    private bool IsEligibleGroup(CostGroup group)
    {
        int total = group.Type == CostType.Coins ? TotalCoins : TotalGems;
        return !ProgressiveCostGroupCosts.TryGetValue(group.Name, out int required) || total >= required;
    }

    private void PayCosts(string scene)
    {
        if (!costGroupByScene.TryGetValue(scene, out var group)) return;
        if (PaidCostGroups.Contains(group.Name)) return;

        if (group.Type == CostType.Coins) Coins -= group.Cost;
        else Gems -= group.Cost;
        PaidCostGroups.Add(group.Name);
    }

    private bool IsValidSwap(Transition src1, Transition dst1, Transition src2, Transition dst2)
    {
        // FIXME
        return true;
    }

    private static bool IsCompletable(ProgressionManager pm)
    {
        var unclaimed = RandoCtx.itemPlacements;
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
        IReadOnlyDictionary<Transition, RandoModTransition> transitionDefs,
        Transition src1, Transition dst1, Transition src2, Transition dst2)
    {
        if (src1 == src2 && dst1 == dst2) return true;

        var lm = RandoCtx.LM;
        ProgressionManager pm = new(lm, RandoCtx);
        var mu = pm.mu;

        mu.AddWaypoints(lm.Waypoints);
        mu.AddTransitions(lm.TransitionLookup.Values);
        mu.AddPlacements(RandoCtx.Vanilla);

        List<UpdateEntry> updates = [];
        foreach (var e in ItemChanger.Internal.Ref.Settings.TransitionOverrides)
        {
            var src = e.Key;

            Transition dst;
            if (src == src1) dst = dst2;
            else if (src == src2) dst = dst1;
            else dst = e.Value.ToStruct();

            TransitionPlacement t = new()
            {
                Source = transitionDefs[src],
                Target = transitionDefs[dst],
            };
            updates.Add(new PrePlacedItemUpdateEntry(t));
        }
        mu.AddEntries(updates);

        mu.StartUpdating();
        mu.SetLongTermRevertPoint();

        return IsCompletable(pm);
    }

    private void SwapTransitions(Transition src1, Transition src2)
    {
        var overrides = ItemChanger.Internal.Ref.Settings.TransitionOverrides;
        var dst1 = overrides[src1].ToStruct();
        var dst2 = overrides[src2].ToStruct();
        (overrides[src1], overrides[src2]) = (overrides[src2], overrides[src1]);

        if (TransitionSettings.Coupled)
        {
            if (overrides.ContainsKey(dst1)) overrides[dst1] = src2;
            if (overrides.ContainsKey(dst2)) overrides[dst2] = src1;
        }
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
            if (!ResolvedEnteredTransitions.Contains(cSrc) && !ResolvedExitedTransitions.Contains(cDst)) potentialTargets.Add((cSrc, cDst));
        }

        potentialTargets.Shuffle(new());  // FIXME: Seed

        Dictionary<Transition, RandoModTransition> transitionDefs = [];
        foreach (var t in RandoCtx.transitionPlacements)
        {
            transitionDefs[t.Source.ToStruct()] = t.Source;
            transitionDefs[t.Target.ToStruct()] = t.Target;
        }

        SortedDictionary<int, List<(Transition, Transition)>> backfillDict = [];
        SortedDictionary<int, List<(Transition, Transition)>> shopBackfillDict = [];
        SortedDictionary<int, List<(Transition, Transition)>> ineligibleShopBackfillDict = [];
        List<(Transition, Transition)> pinBackfill = [];
        bool haveShop = false;
        Wrapped<bool> startedBackfill = new(false);
        Wrapped<bool> startedShopBackfill = new(false);
        Wrapped<bool> startedIneligibleShopBackfill = new(false);

        IEnumerator<(Transition, Transition)> EnumerateCandidates()
        {
            foreach (var p in potentialTargets) yield return p;
            startedBackfill.Value = true;
            foreach (var list in backfillDict.Values) foreach (var p in list) yield return p;
            startedShopBackfill.Value = true;
            foreach (var list in shopBackfillDict.Values) foreach (var p in list) yield return p;
            startedIneligibleShopBackfill.Value = true;
            foreach (var list in ineligibleShopBackfillDict.Values) foreach (var p in list) yield return p;
        };
        var iter = EnumerateCandidates();

        List<SceneChoiceInfo> choices = [];
        HashSet<string> chosenScenes = [];
        // FIXME: Handle previous for rerolls
        bool MaybeAdd(Transition newSrc, Transition newTarget)
        {
            if (!CanSwapTransitions(transitionDefs, src, target, newSrc, newTarget)) return false;

            SceneChoiceInfo info = new()
            {
                OrigSrc = newSrc,
                Pinned = newTarget.SceneName == PinnedScene,
                Target = newTarget
            };
            if (costGroupByScene.TryGetValue(newTarget.SceneName, out var group) && !PaidCostGroups.Contains(group.Name)) info.Cost = (group.Type, group.Cost);

            choices.Add(info);
            chosenScenes.Add(newTarget.SceneName);
            return true;
        }

        while (choices.Count < Settings.Choices && iter.MoveNext())
        {
            var (newSrc, newTarget) = iter.Current;
            if (chosenScenes.Contains(newTarget.SceneName)) continue;

            bool isShop = costGroupByScene.TryGetValue(newTarget.SceneName, out var group) && !PaidCostGroups.Contains(group.Name);
            if (!RefreshCounters.TryGetValue(newTarget.SceneName, out int refreshCount)) refreshCount = 0;

            if (!startedBackfill.Value)
            {
                if (!IsValidSwap(src, target, newSrc, newTarget)) continue;
                if (newTarget.SceneName == PinnedScene)
                {
                    pinBackfill.Add((newSrc, newTarget));
                    continue;
                }
                if (isShop && !IsEligibleGroup(group))
                {
                    ineligibleShopBackfillDict.GetOrAddNew(refreshCount).Add((newSrc, newTarget));
                    continue;
                }
            }

            bool isRedundantShop = haveShop && isShop;
            if ((refreshCount == 0 || startedBackfill.Value) && (!isRedundantShop || startedIneligibleShopBackfill.Value))
            {
                if (MaybeAdd(newSrc, newTarget) && isShop) haveShop = true;
            }
            else
            {
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
        if (!TransitionInferenceUtil.GetSrcTarget(self, info, out var src, out var target) || ResolvedEnteredTransitions.Contains(src))
        {
            orig(self, info);
            return;
        }

        var choices = CalculateSceneChoices(src, target)!;
        RoomSelectionUI.Create(
            this,
            choices,
            decision =>
            {
                if (decision.chosen is not SceneChoiceInfo choice)
                {
                    // FIXME: Rejection.
                    return;
                }

                MaybeSelectNewPin(decision.newPin?.Target.SceneName);
                PayCosts(choice.Target.SceneName);
                SwapTransitions(src, choice.OrigSrc);

                orig(self, info);
            },
            decision =>
            {
                MaybeSelectNewPin(decision.newPin?.Target.SceneName);
                return CalculateSceneChoices(src, target, choices);
            });
    }
}

// Maybe delete
internal class BugPrinceSceneLoadInfo : GameManager.SceneLoadInfo
{
    internal BugPrinceSceneLoadInfo(GameManager.SceneLoadInfo orig)
    {
        IsFirstLevelForPlayer = orig.IsFirstLevelForPlayer;
        SceneName = orig.SceneName;
        HeroLeaveDirection = orig.HeroLeaveDirection;
        EntryGateName = orig.EntryGateName;
        EntryDelay = orig.EntryDelay;
        PreventCameraFadeOut = orig.PreventCameraFadeOut;
        WaitForSceneTransitionCameraFade = orig.WaitForSceneTransitionCameraFade;
        Visualization = orig.Visualization;
        AlwaysUnloadUnusedAssets = orig.AlwaysUnloadUnusedAssets;
        forceWaitFetch = orig.forceWaitFetch;
    }
}
