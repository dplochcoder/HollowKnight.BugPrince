using BugPrince.Data;
using BugPrince.ItemSyncInterop;
using BugPrince.Rando;
using BugPrince.UI;
using BugPrince.Util;
using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using Modding;
using Newtonsoft.Json;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerMod.IC;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace BugPrince.IC;

public class TransitionSelectionModule : ItemChanger.Modules.Module, ICostGroupProgressionProvider
{
    // Setup
    public RandomizationSettings Settings = new();
    public Dictionary<string, CostGroup> CostGroups = [];
    public Dictionary<string, string> CostGroupsByScene = [];
    public HashSet<Transition> RandomizedTransitions = [];

    // Inventory
    public int Coins = 0;
    public int DiceTotems = 0;
    public int Gems = 0;
    public int PushPins = 0;
    public string? PinnedScene;

    // Inventory Tracking
    public int TotalGems = 0;
    public int TotalCoins = 0;
    public int TotalPushPins = 0;

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
    private bool needRandoMapModUpdate = false;

    private static TransitionSelectionModule? instance;

    public static TransitionSelectionModule? Get() => instance;

    private Thread? precomputeThread;

    public override void Initialize()
    {
        Events.OnEnterGame += DoLateInitialization;
        Events.OnSceneChange += ResetPrecomputersNewScene;
        if (ModHooks.GetMod("RandoMapMod") is Mod) HookMapChanger();

        precomputeThread = new(UpdatePrecomputers) { Priority = System.Threading.ThreadPriority.BelowNormal };
        precomputeThread.Start();

        BugPrinceMod.StartDebugLog();

        instance = this;
    }

    public override void Unload()
    {
        Events.OnEnterGame -= DoLateInitialization;
        Events.OnSceneChange -= ResetPrecomputersNewScene;
        On.GameManager.BeginSceneTransition -= SelectRandomizedTransition;
        if (ModHooks.GetMod("RandoMapMod") is Mod) UnhookMapChanger();

        precomputeThread?.Abort();

        instance = null;
    }

    private void HookMapChanger()
    {
        MapChanger.Events.OnWorldMap += MaybeRebuildMap;
        MapChanger.Events.OnQuickMap += MaybeRebuildMap;
    }

    private void UnhookMapChanger()
    {
        MapChanger.Events.OnWorldMap -= MaybeRebuildMap;
        MapChanger.Events.OnQuickMap -= MaybeRebuildMap;
    }

    private void MaybeRebuildMap(GameMap worldMap)
    {
        if (needRandoMapModUpdate && BugPrinceMod.GS.EnablePathfinderUpdates)
        {
            RandoMapCore.RandoMapCoreMod.Rebuild();
            needRandoMapModUpdate = false;
        }
    }
    private void MaybeRebuildMap(GameMap gameMap, MapZone mapZone) => MaybeRebuildMap(gameMap);

    private static RandomizerSettings RS() => RandomizerMod.RandomizerMod.RS;

    private static RandoModContext RandoCtx() => RS().Context;

    private static List<TransitionPlacement> RandoTransitionPlacements() => RandoCtx().transitionPlacements;

    private static TransitionSettings TransitionSettings() => RandomizerMod.RandomizerMod.RS.GenerationSettings.TransitionSettings;

    internal bool MatchingTransitions() => TransitionSettings().TransitionMatching != RandomizerMod.Settings.TransitionSettings.TransitionMatchingSetting.NonmatchingDirections;

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

        if (!IsHost || IsRealHost)
        {
            EnsureSyncer();
            GetTransitionSwapUpdates(
                new GetTransitionSwapUpdatesRequest() { LastKnownSequenceNumber = TransitionSwapUpdates.Count - 1 },
                response => ApplyTransitionSwapUpdates(response.Updates));
        }
    }

    private void EnsureSyncer() => ItemChangerMod.Modules.GetOrAdd<TransitionSelectionSyncer>();

    private int GetResourceCount(int total, CostType type, string? tempCostScene = null)
    {
        foreach (var paid in PaidCostGroups)
        {
            var paidGroup = CostGroups[paid];
            if (paidGroup.Type == type) total -= paidGroup.Cost;
        }
        if (tempCostScene != null && this.GetCostGroupByScene(tempCostScene, out var groupName, out var group) && group.Type == type && !PaidCostGroups.Contains(groupName)) total -= group.Cost;
        return total;
    }

    internal int GetGems(string? tempCostScene = null) => GetResourceCount(TotalGems, CostType.Gems, tempCostScene);

    internal int GetCoins(string? tempCostScene = null) => GetResourceCount(TotalCoins, CostType.Coins, tempCostScene);

    internal int GetPushPins(PinReceipt? tempPinReceipt = null)
    {
        Dictionary<int, int> spend = [];
        foreach (var e in NumPinReceipts) spend.Add(e.Key, e.Value);
        if (tempPinReceipt != null) spend[tempPinReceipt.RequestingPlayerID] = Math.Max(spend.GetOrDefault(tempPinReceipt.RequestingPlayerID), tempPinReceipt.ReceiptNumber);

        return TotalPushPins - spend.Values.Sum();
    }

    internal bool CanPayCosts(string scene)
    {
        if (!this.GetCostGroupByScene(scene, out var groupName, out var group)) return true;
        if (PaidCostGroups.Contains(groupName)) return true;

        return group.Type switch { CostType.Coins => Coins >= group.Cost, CostType.Gems => Gems >= group.Cost, _ => throw group.Type.InvalidEnum() };
    }

    private void PayCosts(string scene)
    {
        if (!this.GetCostGroupByScene(scene, out var groupName, out var group)) return;
        if (PaidCostGroups.Contains(groupName)) return;

        if (CostGroupProgression[PaidCostGroups.Count] != groupName)
        {
            // Reorder progression.
            CostGroupProgression.Remove(groupName);
            CostGroupProgression.Insert(PaidCostGroups.Count, groupName);

            gen = CostGroupProgressionProviderGeneration.NextGen(); // Reset logic variable caches.
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
        else if (b == GateDirection.Door) return a == GateDirection.Left || a == GateDirection.Right;
        else if (a == GateDirection.Door) return b == GateDirection.Left || b == GateDirection.Right;
        else return a == b.Opposite();
    }

    private bool IsValidSwap(Transition src1, Transition dst1, Transition src2, Transition dst2)
    {
        if (src1 == src2 && dst1 == dst2) return true;

        bool oneWay1 = IsExitOnly(dst1);
        bool oneWay2 = IsExitOnly(dst2);
        if (TransitionSettings().Coupled && oneWay1 != oneWay2) return false;
        if (oneWay1 && oneWay2) return true;

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

    private bool CanSwapTransitions(Transition src1, Transition src2)
    {
        var target1 = UnsyncedRandoPlacements[src1];
        var target2 = UnsyncedRandoPlacements[src2];

        var ctx = RandoCtx();
        var lm = ctx.LM;

        Action? onDone = null;
        if (this.GetCostGroupByScene(target1.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName) && CostGroupProgression[PaidCostGroups.Count] != groupName)
        {
            // Check that we can bump this shop forward in progression order.
            if (!lm.VariableResolver.TryGetInner<BugPrinceVariableResolver>(out var inner)) throw new ArgumentException("Missing BugPrinceVariableResolver");

            List<string> reordered = [.. CostGroupProgression];
            reordered.Remove(groupName);
            reordered.Insert(PaidCostGroups.Count, groupName);

            OverlaidCostGroupProgressionProvider provider = new(this, reordered);
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
            var swap = src1 != src2;
            foreach (var p in RandoTransitionPlacements())
            {
                var source = p.Source.ToStruct();
                var target = p.Target.ToStruct();

                if (swap)
                {
                    if (source == src1) target = target2;
                    else if (source == src2) target = target1;
                    else if (coupled)
                    {
                        if (source == target1) target = src2;
                        else if (source == target2) target = src1;
                    }
                }

                TransitionPlacement t = new()
                {
                    Source = sourceRandoTransitions[source],
                    Target = targetRandoTransitions[target]
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
                placements[i] = new()
                {
                    Source = p.Source,
                    Target = targetRandoTransitions[newTarget]
                };
                transitionLookup[source.ToString()] = newTarget.ToString();
            }
        }
    }

    public int Seed = 0;

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
            var origTarget = e.Value.ToStruct();
            if (icUpdates.TryGetValue(origTarget, out var newTarget)) newKVs.Add((e.Key, newTarget));
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

    private IEnumerator<List<SceneChoiceInfo>?> CalculateSceneChoicesIterator(Transition src, List<SceneChoiceInfo>? previous = null)
    {
        var target = UnsyncedRandoPlacements[src];

        Dictionary<string, int> tempRefreshCounters = [];
        foreach (var e in RefreshCounters)
        {
            if (e.Value > 1) tempRefreshCounters[e.Key] = e.Value - 1;
        }

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
            if (!CanSwapTransitions(src, newSrc))
            {
                illogicalSwaps.Value++;
                return false;
            }

            (CostType, int)? cost = null;
            if (this.GetCostGroupByScene(newTarget.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName)) cost = (group.Type, group.Cost);
            SceneChoiceInfo info = new(newSrc, newTarget, cost, newTarget.SceneName == PinnedScene);

            choices.Add(info);
            chosenScenes.Add(newTarget.SceneName);
            return true;
        }

        int dupeScenes = 0;
        int backfills = 0;
        int firstPassRemaining = potentialTargets.Count;
        while (iter.MoveNext())
        {
            if (choices.Count < Settings.NumRoomChoices && firstPassRemaining > 0) --firstPassRemaining;
            var (newSrc, newTarget) = iter.Current;

            if (newTarget.SceneName == PinnedScene)
            {
                pinBackfill.Add((newSrc, newTarget));
                continue;
            }
            else if (choices.Count == Settings.NumRoomChoices) continue;
            if (chosenScenes.Contains(newTarget.SceneName) || tempChosenScenes.Contains(newTarget.SceneName)) { ++dupeScenes; continue; }

            bool isShop = this.GetCostGroupByScene(newTarget.SceneName, out var groupName, out var group) && !PaidCostGroups.Contains(groupName);
            if (!tempRefreshCounters.TryGetValue(newTarget.SceneName, out int refreshCount)) refreshCount = 0;

            bool isRedundantShop = haveShop && isShop;
            if ((refreshCount == 0 || startedBackfill.Value) && (!isRedundantShop || startedShopBackfill.Value))
            {
                if (MaybeAdd(newSrc, newTarget) && isShop) haveShop = true;
                yield return null;
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

        // Add the pinned scene if we can.
        foreach (var (newSrc, newTarget) in pinBackfill)
        {
            bool added = MaybeAdd(newSrc, newTarget);
            yield return null;

            if (added) break;
        }

        BugPrinceMod.DebugLog($"CALCULATE_SCENE_CHOICES: (illogicalSwaps={illogicalSwaps.Value}, dupeScenes={dupeScenes}, backfills={backfills}, remaining={firstPassRemaining})");
        yield return choices;
    }

    private List<SceneChoiceInfo> CalculateSceneChoices(Transition src, List<SceneChoiceInfo>? previous = null)
    {
        var iter = CalculateSceneChoicesIterator(src, previous);
        
        while (true)
        {
            iter.MoveNext();
            if (iter.Current != null) return iter.Current;
        }
    }

    private void UpdateRefreshCounters(List<string> scenes)
    {
        // Decrement existing counters.
        Dictionary<string, int> temp = [];
        foreach (var e in RefreshCounters)
        {
            if (e.Value > 1) temp[e.Key] = e.Value - 1;
        }
        RefreshCounters = temp;

        // Reset refresh count for all selected choices.
        scenes.ForEach(s => RefreshCounters[s] = Settings.RefreshCycle);
    }

    private void MaybeReleaseObsoletePin()
    {
        if (PinnedScene == null) return;

        // If transitions are coupled and we unlocked all reverse transitions, free the pin.
        bool anyTransition = false;
        foreach (var p in RandoTransitionPlacements())
        {
            var dst = p.Target.ToStruct();
            if (dst.SceneName != PinnedScene) continue;
            if (ResolvedExitedTransitions.Contains(dst)) continue;

            anyTransition = true;
            break;
        }

        if (!anyTransition) PinnedScene = null;
    }

    private bool MapToPlacement(ref Transition src, ref Transition target)
    {
        foreach (var p in RandoTransitionPlacements())
        {
            var pSrc = p.Source.ToStruct();
            var pTarget = p.Target.ToStruct();

            if (target == pTarget)
            {
                // Handle Jiji-jinn bridge.
                src = pSrc;
                return true;
            }
        }

        return false;
    }

    private readonly AutoResetEvent precomputersEvent = new(false);
    private readonly Dictionary<Transition, ChoicePrecomputer> precomputers = [];

    private void RebuildActivePrecomputers(Dictionary<Transition, ChoicePrecomputer> dict)
    {
        lock (precomputers)
        {
            foreach (var e in dict) precomputers.Add(e.Key, e.Value);
            if (precomputers.Count > 0) precomputersEvent.Set();
        }
    }

    private void ResetPrecomputersNewScene(Scene scene)
    {
        if (!BugPrinceMod.GS.EnablePrecomputation) return;

        lock (precomputers) { precomputers.Clear(); }

        Dictionary<Transition, ChoicePrecomputer> newPrecomputers = [];
        Dictionary<Transition, Transition> targetToSrc = [];
        foreach (var p in RandoTransitionPlacements()) targetToSrc[p.Target.ToStruct()] = p.Source.ToStruct();

        foreach (var e in ItemChanger.Internal.Ref.Settings.TransitionOverrides)
        {
            if (e.Key.SceneName != scene.name) continue;

            var target = e.Value.ToStruct();
            if (targetToSrc.TryGetValue(target, out var src) && !ResolvedEnteredTransitions.Contains(src) && !ResolvedExitedTransitions.Contains(target)) newPrecomputers.Add(src, new(CalculateSceneChoicesIterator(src)));
        }

        RebuildActivePrecomputers(newPrecomputers);
    }

    private Action DeferredResetPrecomputersSameScene()
    {
        if (!BugPrinceMod.GS.EnablePrecomputation) return () => { };

        HashSet<Transition> updatedTransitions = [];
        lock (precomputers)
        {
            updatedTransitions = [.. precomputers.Keys.Where(t => !ResolvedEnteredTransitions.Contains(t))];
            precomputers.Clear();
        }

        return () =>
        {
            Dictionary<Transition, ChoicePrecomputer> updatedPrecomputers = [];
            foreach (var src in updatedTransitions) updatedPrecomputers.Add(src, new(CalculateSceneChoicesIterator(src)));

            RebuildActivePrecomputers(updatedPrecomputers);
        };
    }

    private void UpdatePrecomputers()
    {
        while (true)
        {
            List<ChoicePrecomputer> candidates = [];
            lock (precomputers) { candidates = [.. precomputers.Values]; }

            if (candidates.Count == 0) precomputersEvent.WaitOne();
            else
            {
                candidates.OrderBy(p => p.Tests());
                candidates[0].Advance();
            }
        }
    }

    private static readonly bool IsItemSyncInstalled = ModHooks.GetMod("ItemSyncMod") is Mod;

    private static bool ItemSyncIsHost(bool real)
    {
        var s = ItemSyncMod.ItemSyncMod.ISSettings;
        return real ? (s.IsItemSync && s.MWPlayerId == 0 && s.nicknames.Count > 1) : (!s.IsItemSync || s.MWPlayerId == 0);
    }
    private static int ItemSyncPlayerId()
    {
        var s = ItemSyncMod.ItemSyncMod.ISSettings;
        return s.IsItemSync ? s.MWPlayerId : 0;
    }
    private bool IsHost => !IsItemSyncInstalled || ItemSyncIsHost(false);
    private bool IsRealHost => IsItemSyncInstalled && ItemSyncIsHost(true);
    private int PlayerId => IsItemSyncInstalled ? ItemSyncPlayerId() : 0;

    // Permanent record of all transition sync updates chronologically.
    public List<TransitionSwapUpdate> TransitionSwapUpdates = [];

    // Permanent record of push pin usages, keyed by player id.
    public Dictionary<int, int> NumPinReceipts = [];

    private void ForceApplyTransitionSwapUpdate(TransitionSwapUpdate update, bool resetTrackers)
    {
        if (update.PinReceipt != null)
        {
            --PushPins;
            NumPinReceipts[update.PinReceipt.RequestingPlayerID] = update.PinReceipt.ReceiptNumber;
            BugPrinceMod.DebugLog("SPENT_PIN");
        }

        update.RefreshCounterUpdates.ForEach(UpdateRefreshCounters);
        if (update.Swap != null)
        {
            PayCosts(update.Swap.Target2.SceneName);
            SwapTransitions(update.Swap.Source1, update.Swap.Source2);
            if (update.Swap.Source1 != update.Swap.Source2) needRandoMapModUpdate = true;
        }
        if (resetTrackers) ResetTrackers();

        TransitionSwapUpdates.Add(update);
        if (IsRealHost)
        {
            if (BugPrinceMod.GS.AutoSaveChoices) GameManager.instance.SaveGame();
            Send(update);
        }

        if (update.Swap != null)
        {
            MaybeReleaseObsoletePin();
            BugPrinceMod.DebugLog($"CHOSE: {update.Swap.Source1} -> {UnsyncedRandoPlacements[update.Swap.Source1]}");
        }
    }

    private bool SalvageTransitionSwap(ref TransitionSwap swap)
    {
        // Check if the transition has been chosen already.
        if (ResolvedEnteredTransitions.Contains(swap.Source1)) return false;
        if (TransitionSettings().Coupled && ResolvedExitedTransitions.Contains(swap.Source1)) return false;

        // Check if we can still afford it.
        if (!CanPayCosts(swap.Target2.SceneName)) return false;

        // Check if this transition is still logically permissible.
        foreach (var p in RandoTransitionPlacements())
        {
            if (p.Target.ToStruct() == swap.Target2)
            {
                swap.Source2 = p.Source.ToStruct();
                break;
            }
        }
        if (!CanSwapTransitions(swap.Source1, swap.Source2)) return false;

        return true;
    }

    // Attempt to apply a transiton swap update. Returns true if successful, false if rejected.
    private bool MaybeApplyTransitionSwapUpdate(ref TransitionSwapUpdate update)
    {
        // All incoming requests should have swaps.
        if (update.Swap == null) return false;

        if (update.SequenceNumber == TransitionSwapUpdates.Count)
        {
            ForceApplyTransitionSwapUpdate(update, true);
            return true;
        }

        // There's a race condition, yippee!
        update.SequenceNumber = TransitionSwapUpdates.Count;

        // Validate the PinReceipt.
        if (update.PinReceipt != null && PushPins == 0) update.PinReceipt = null;

        // See if we can salvage the requested swap.
        bool canSwap = SalvageTransitionSwap(ref update.Swap);
        if (!canSwap) update.Swap = null;

        // Commit regardless, to update refresh trackers and pin count.
        ForceApplyTransitionSwapUpdate(update, canSwap);
        return canSwap;
    }

    private void Send(SwapTransitionsRequest request, Action<SwapTransitionsResponse> callback) => TransitionSelectionSyncer.Get()!.Send(request, callback);

    private void Send(TransitionSwapUpdate update) => TransitionSelectionSyncer.Get()!.Send(update);

    private void Send(GetTransitionSwapUpdatesRequest request, Action<GetTransitionSwapUpdatesResponse> callback) => TransitionSelectionSyncer.Get()!.Send(request, callback);

    internal void SwapTransitions(SwapTransitionsRequest request, Action<SwapTransitionsResponse> callback)
    {
        if (!IsHost)
        {
            Send(request, callback);
            return;
        }

        int origSequenceNumber = request.Update.SequenceNumber;
        callback(new()
        {
            Nonce = request.Nonce,
            Accepted = MaybeApplyTransitionSwapUpdate(ref request.Update),
            AcceptedPin = request.Update.PinReceipt != null,
            Updates = new() { Updates = GetTransitionSwapUpdatesSince(origSequenceNumber - 1) }
        });
    }

    private List<TransitionSwapUpdate> GetTransitionSwapUpdatesSince(int seq)
    {
        List<TransitionSwapUpdate> ret = [];
        for (int i = seq + 1; i < TransitionSwapUpdates.Count; i++) ret.Add(TransitionSwapUpdates[i]);
        return ret;
    }

    internal void GetTransitionSwapUpdates(GetTransitionSwapUpdatesRequest request, Action<GetTransitionSwapUpdatesResponse> callback)
    {
        if (!IsHost)
        {
            Send(request, callback);
            return;
        }

        callback(new() { Nonce = request.Nonce, Updates = GetTransitionSwapUpdatesSince(request.LastKnownSequenceNumber) });
    }

    internal void ApplyTransitionSwapUpdates(List<TransitionSwapUpdate> updates)
    {
        if (IsHost) return;

        Action deferred = updates.Any(u => u.SequenceNumber == TransitionSwapUpdates.Count) ? DeferredResetPrecomputersSameScene() : () => { };
        foreach (var update in updates)
        {
            if (update.SequenceNumber > TransitionSwapUpdates.Count)
            {
                Send(
                    new GetTransitionSwapUpdatesRequest() { LastKnownSequenceNumber = TransitionSwapUpdates.Count - 1 },
                    response => ApplyTransitionSwapUpdates(response.Updates));
                break;
            }
            else if (update.SequenceNumber == TransitionSwapUpdates.Count) ForceApplyTransitionSwapUpdate(update, update.SequenceNumber == updates[updates.Count - 1].SequenceNumber);
        }
        deferred();
    }

    private void SelectRandomizedTransition(On.GameManager.orig_BeginSceneTransition orig, GameManager self, GameManager.SceneLoadInfo info)
    {
        if (RoomSelectionUI.uiPresent) return;

        if (!TransitionInferenceUtil.GetSrcTarget(self, info, out var src, out var target) || !MapToPlacement(ref src, ref target) || ResolvedEnteredTransitions.Contains(src))
        {
            orig(self, info);
            return;
        }

        LaunchUI(src, () => orig(self, info));
    }

    private void TinkTinkTink()
    {
        static IEnumerator Routine()
        {
            GameObject obj = new("oops");
            var audio = obj.AddComponent<AudioSource>();
            obj.transform.position = HeroController.instance.transform.position;

            audio.PlayOneShot(SoundCache.FailedMenu);
            yield return new WaitForSeconds(0.35f);
            audio.PlayOneShot(SoundCache.FailedMenu);
            yield return new WaitForSeconds(0.35f);
            audio.PlayOneShot(SoundCache.FailedMenu);
        }

        GameManager.instance.StartCoroutine(Routine());
    }

    internal PinReceipt NextPinReceipt() => new()
    {
        RequestingPlayerID = PlayerId,
        ReceiptNumber = NumPinReceipts.GetOrDefault(PlayerId) + 1
    };

    private void LaunchUI(Transition src, Action done)
    {
        ChoicePrecomputer? precomputer;
        lock (precomputers)
        {
            precomputers.TryGetValue(src, out precomputer);
            precomputers.Clear();

            // Keep this here in to retrigger our next roll if selection fails.
            if (precomputer != null) precomputers.Add(src, precomputer);
        }
        var choices = precomputer?.GetResult() ?? CalculateSceneChoices(src);
        Wrapped<RoomSelectionDecision?> selectionDecision = new(null);

        SwapTransitionsRequest swapRequest = new();
        var update = swapRequest.Update;
        update.SequenceNumber = TransitionSwapUpdates.Count;
        var swap = update.Swap!;
        swap.Source1 = src;
        update.RefreshCounterUpdates.Add([.. choices.Select(i => i.Target.SceneName)]);

        Wrapped<RoomSelectionUI?> wrapped = new(null);
        wrapped.Value = RoomSelectionUI.Create(
            this,
            src.GetDirection(),
            choices,
            (decision, cb) =>
            {
                if (decision.chosen is not SceneChoiceInfo choice)
                {
                    UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                    Benchwarp.ChangeScene.WarpToRespawn();
                    BugPrinceMod.DebugLog($"NO_CHOICES: {src}");
                    return;
                }

                swap.Source2 = choice.OrigSrc;
                swap.Target2 = choice.Target;
                if (decision.newPin != null) update.PinReceipt = NextPinReceipt();

                selectionDecision.Value = decision;
                SwapTransitions(swapRequest, cb);
            },
            response =>
            {
                ApplyTransitionSwapUpdates(response.Updates.Updates);

                // See if push pin was accepted.
                var decision = selectionDecision.Value!;
                if (response.AcceptedPin) PinnedScene = decision.newPin?.Target.SceneName;

                // See if transition swap was accepted.
                if (!response.Accepted)
                {
                    if (ResolvedEnteredTransitions.Contains(src))
                    {
                        // We lost the race to set this transition's target.
                        if (UnsyncedRandoPlacements[src].SceneName != decision.chosen!.Target.SceneName) TinkTinkTink();
                        done();
                    }
                    else
                    {
                        // Selection failed; retry.
                        RoomSelectionUI.uiPresent = false;
                        UnityEngine.Object.Destroy(wrapped.Value?.gameObject);

                        TinkTinkTink();
                        LaunchUI(src, done);
                    }
                    return;
                }

                // Free our pin if we selected the room for it.
                if (PinnedScene == decision.chosen!.Target.SceneName) PinnedScene = null;

                UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                done();
            },
            (out List<SceneChoiceInfo> newChoices) =>
            {
                // Check if we can reroll anymore.
                newChoices = [];
                if (ResolvedEnteredTransitions.Contains(src))
                {
                    TinkTinkTink();
                    return false;
                }

                BugPrinceMod.DebugLog("USED_DICE_TOTEM");
                DiceTotems--;

                newChoices = CalculateSceneChoices(src, choices);
                update.SequenceNumber = TransitionSwapUpdates.Count;
                update.RefreshCounterUpdates.Add([.. newChoices.Select(i => i.Target.SceneName)]);

                return true;
            },
            () =>
            {
                UnityEngine.Object.Destroy(wrapped.Value?.gameObject);
                done();
            });
    }

    private int gen = CostGroupProgressionProviderGeneration.NextGen();

    public int Generation() => gen;

    public IReadOnlyDictionary<string, CostGroup> GetCostGroups() => CostGroups;

    public IReadOnlyDictionary<string, string> GetCostGroupsByScene() => CostGroupsByScene;

    public bool IsRandomizedTransition(Transition transition) => RandomizedTransitions.Contains(transition);

    public IReadOnlyList<string> GetCostGroupProgression() => CostGroupProgression;
}

internal class ChoicePrecomputer
{
    private readonly IEnumerator<List<SceneChoiceInfo>?> generator;
    private List<SceneChoiceInfo>? result;
    private int tests = 0;

    internal ChoicePrecomputer(IEnumerator<List<SceneChoiceInfo>?> generator) => this.generator = generator;

    internal int Tests()
    {
        lock (this) { return tests; }
    }

    internal bool Advance()
    {
        lock (this)
        {
            if (result != null) return false;

            generator.MoveNext();
            tests++;

            if (generator.Current != null) result = generator.Current;
            return true;
        }
    }

    internal List<SceneChoiceInfo> GetResult()
    {
        lock (this)
        {
            while (result == null)
            {
                generator.MoveNext();
                tests++;

                if (generator.Current != null) result = generator.Current;
            }

            return result;
        }
    }
}
