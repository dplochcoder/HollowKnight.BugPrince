using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Rando;

internal class RequestModifier
{
    internal static void Setup()
    {
        RequestBuilder.OnUpdate.Subscribe(3000f, ModifyRequest);
        ProgressionInitializer.OnCreateProgressionInitializer += AddTolerances;
    }

    private static IEnumerable<string> GetRandomizedTransitions(RequestBuilder rb)
    {
        foreach (var group in rb.EnumerateTransitionGroups())
        {
            if (group is TransitionGroupBuilder tgb)
            {
                foreach (var name in tgb.Sources.EnumerateDistinct()) yield return name;
                foreach (var name in tgb.Targets.EnumerateDistinct()) yield return name;
            }
            else if (group is SelfDualTransitionGroupBuilder sdtgb)
            {
                foreach (var name in sdtgb.Transitions.EnumerateDistinct()) yield return name;
            }
            else if (group is SymmetricTransitionGroupBuilder stgb)
            {
                foreach (var name in stgb.Group1.EnumerateDistinct()) yield return name;
                foreach (var name in stgb.Group2.EnumerateDistinct()) yield return name;
            }
        }
    }

    private static void ProvideRequestInfo(RequestBuilder rb)
    {

        rb.EditItemRequest(CoinItem.ITEM_NAME, info => info.getItemDef = CoinItem.ItemDef);
        rb.EditItemRequest(DiceTotemItem.ITEM_NAME, info => info.getItemDef = DiceTotemItem.ItemDef);
        rb.EditItemRequest(GemItem.ITEM_NAME, info => info.getItemDef = GemItem.ItemDef);
        rb.EditItemRequest(PushPinItem.ITEM_NAME, info => info.getItemDef = PushPinItem.ItemDef);

        foreach (var location in Locations.GetLocations())
        {
            var (name, loc) = (location.Key, location.Value);
            rb.EditLocationRequest(name, info => info.getLocationDef = loc.LocationDef);
        }
    }

    private static void ModifyRequest(RequestBuilder rb)
    {
        if (!RandoInterop.IsEnabled) return;
        ProvideRequestInfo(rb);

        HashSet<string> randomizedScenes = [];
        foreach (var transition in GetRandomizedTransitions(rb))
        {
            if (!transition.ToTransition(out var t)) throw new ArgumentException($"Invalid transition: '{t}'");

            RandoInterop.LS!.RandomizedTransitions.Add(t);
            randomizedScenes.Add(t.SceneName);
        }

        if (randomizedScenes.Count == 0)
        {
            // No items.
            foreach (var location in Locations.GetLocations())
            {
                var (name, loc) = (location.Key, location.Value);
                if (loc.IsEnabled(RandoInterop.RS)) rb.AddLocationByName(location.Key);
            }
            return;
        }

        foreach (var scene in randomizedScenes)
        {
            var stream = typeof(BugPrinceMod).Assembly.GetManifestResourceStream($"BugPrince.Resources.Sprites.Scenes.{scene}.png");
            if (stream == null) BugPrinceMod.Instance?.LogError($"Missing scene: {scene}");
        }

        Dictionary<string, (string, CostGroup)> groups = [];
        foreach (var e in CostGroup.GetProducers())
        {
            var groupName = e.Key;
            if (!e.Value.ProduceCostGroup(rb.gs, randomizedScenes.Contains, out var costGroup)) continue;

            var priority = costGroup.Priority;
            foreach (var scene in costGroup.SceneNames)
            {
                if (groups.TryGetValue(scene, out var pair))
                {
                    var (existingName, existingGroup) = pair;
                    var existingPriority = existingGroup.Priority;
                    if (priority == existingPriority) throw new ArgumentException($"Cost groups '{existingName}' and '{groupName}' conflict on scene '{scene}'");
                    else if (priority > existingPriority) groups[scene] = (groupName, costGroup);
                }
                else groups[scene] = (groupName, costGroup);
            }
        }

        foreach (var e in groups)
        {
            var scene = e.Key;
            var (name, group) = e.Value;
            RandoInterop.LS!.CostGroups[name] = group;
            RandoInterop.LS.CostGroupsByScene[scene] = name;
        }

        List<(string, CostGroup)> ordered = [.. RandoInterop.LS!.CostGroups.Select(e => (e.Key, e.Value)).OrderBy(p => p.Key)];

        Random r = new(rb.gs.Seed + 17);
        WeightedRandomSort(ordered, r);
        RandoInterop.LS.CostGroupProgression = [.. ordered.Select(p => p.Item1)];

        Locations.GetLocations().Values.ForEach(loc => loc.AddToRequestBuilder(RandoInterop.RS, rb));

        var RS = RandoInterop.RS;
        if (rb.gs.PoolSettings.Relics)
        {
            for (int i = 0; i < RS.NumDiceTotems - RS.StartingDiceTotems; i++) rb.AddItemByName(DiceTotemItem.ITEM_NAME);
            for (int i = 0; i < RS.NumPushPins - RS.StartingPushPins; i++) rb.AddItemByName(PushPinItem.ITEM_NAME);
        }

        if (rb.gs.PoolSettings.Keys && RS.CostsEnabled)
        {
            var coins = RandoInterop.LS.GetItemCount(CostType.Coins) + RandoInterop.RS.CoinTolerance;
            for (int i = 0; i < coins; i++) rb.AddItemByName(CoinItem.ITEM_NAME);
            for (int i = 0; i < RandoInterop.RS.CoinDuplicates; i++) rb.AddItemByName($"{PlaceholderItem.Prefix}{CoinItem.ITEM_NAME}");

            var gems = RandoInterop.LS.GetItemCount(CostType.Gems) + RandoInterop.RS.GemTolerance;
            for (int i = 0; i < gems; i++) rb.AddItemByName(GemItem.ITEM_NAME);
            for (int i = 0; i < RandoInterop.RS.GemDuplicates; i++) rb.AddItemByName($"{PlaceholderItem.Prefix}{GemItem.ITEM_NAME}");
        }
    }

    private static void AddTolerances(LogicManager lm, GenerationSettings gs, ProgressionInitializer pi)
    {
        if (!RandoInterop.AreCostsEnabled) return;

        pi.Setters.Add(new(CostType.Coins.GetTerm(lm), -RandoInterop.RS.CoinTolerance));
        pi.Setters.Add(new(CostType.Gems.GetTerm(lm), -RandoInterop.RS.GemTolerance));
    }

    private static void WeightedRandomSort(List<(string, CostGroup)> list, Random r)
    {
        List<(int, float)> ranks = [];
        for (int i = 0; i < list.Count; i++)
        {
            var avg = list[i].Item2.SkewedAverage;

            var rank = (float)r.NextDouble();
            if (rank >= 0.5f) rank = avg + (1 - avg) * (rank - 0.5f) * 2;
            else rank = avg - avg * (0.5f - rank) * 2;

            ranks.Add((i, rank));
        }
        ranks.StableSort((p1, p2) => p1.Item2.CompareTo(p2.Item2));

        List<(string, CostGroup)> shuffled = [];
        foreach (var (i, _) in ranks) shuffled.Add(list[i]);
        list.Clear();
        list.AddRange(shuffled);
    }
}
