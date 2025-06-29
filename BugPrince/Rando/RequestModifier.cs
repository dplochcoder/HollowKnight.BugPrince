using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using ItemChanger;
using PurenailCore.SystemUtil;
using RandomizerCore.Extensions;
using RandomizerCore.Logic;
using RandomizerCore.Randomization;
using RandomizerMod.RandomizerData;
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
        ProgressionInitializer.OnCreateProgressionInitializer += AddTolerances;
        PurenailCore.RandoUtil.TransitionInjector.AddInjector(AddTransitions);
        RequestBuilder.OnUpdate.Subscribe(3000f, AddItemsAndLocations);
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

    private static IEnumerable<(string, TransitionDef)> AddTransitions(RequestBuilder rb)
    {
        foreach (var e in Transitions.GetTransitions())
        {
            if (!BugPrinceMod.RS.IsLocationPoolEnabled(e.Value.LocationPool)) continue;
            yield return (e.Key, e.Value.Def!);
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
            rb.EditLocationRequest(name, info =>
            {
                info.getLocationDef = loc.LocationDef;

                if (loc.LocationPool == LocationPool.MapShop)
                {
                    info.onRandoLocationCreation += (randoFactory, randoLocation) =>
                    {
                        randoLocation.AddCost(new LogicGeoCost(randoFactory.lm, -1));
                        randoLocation.AddCost(new SimpleCost(randoFactory.lm.GetTermStrict("MAPS"), randoFactory.rng.Next(BugPrinceMod.RS.MinimumMaps, BugPrinceMod.RS.MaximumMaps + 1)));
                    };
                    info.onRandomizerFinish += placement =>
                    {
                        if (placement.Location is not RandoModLocation rl || placement.Item is not RandoModItem ri || rl.costs == null) return;
                        foreach (var cost in rl.costs.OfType<LogicGeoCost>()) cost.GeoAmount = GetShopCost(rb.rng, ri);
                    };
                }
            });
        }

        rb.CostConverters.Subscribe(0f, TryConvertMapCost);
    }

    // TODO: Make public in Randomizer 4?
    private static int GetShopCost(Random rng, RandoModItem item)
    {
        double pow = 1.2; // setting?

        int cap = item.ItemDef is not null ? item.ItemDef.PriceCap : 500;
        if (cap <= 100) return cap;
        if (item.Required) return rng.PowerLaw(pow, 100, Math.Min(cap, 500)).ClampToMultipleOf(5);
        return rng.PowerLaw(pow, 100, cap).ClampToMultipleOf(5);
    }

    private static bool TryConvertMapCost(LogicCost logicCost, out Cost cost)
    {
        if (logicCost is SimpleCost simple && simple.term.Name == "MAPS")
        {
            cost = new MapCost(simple.threshold);
            return true;
        }

        cost = default;
        return false;
    }

    private static void AddItemsAndLocations(RequestBuilder rb)
    {
        if (!BugPrinceMod.RS.IsEnabled) return;

        // Prevent self-loops, which break the swap mechanism.
        if (BugPrinceMod.RS.EnableTransitionChoices && rb.gs.TransitionSettings.Coupled)
        {
            DefaultGroupPlacementStrategy.Constraint noSelfLoops = new(
                (item, location) => item.Name != location.Name,
                (_, _) => throw new RandomizerCore.Exceptions.OutOfLocationsException("BugPrince: No self-loops"),
                "BugPrince-NoSelfLoops");
            foreach (var group in rb.EnumerateTransitionGroups()) if (group.strategy is DefaultGroupPlacementStrategy dgps) dgps.ConstraintList.Add(noSelfLoops);
        }

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
                loc.AddToRequestBuilder(BugPrinceMod.RS, rb);
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

        Locations.GetLocations().Values.ForEach(loc => loc.AddToRequestBuilder(BugPrinceMod.RS, rb));

        var RS = BugPrinceMod.RS;
        if (rb.gs.PoolSettings.Relics)
        {
            rb.AddItemByName(DiceTotemItem.ITEM_NAME, RS.TotalDiceTotems - RS.StartingDiceTotems);
            rb.AddItemByName(PushPinItem.ITEM_NAME, RS.TotalPushPins - RS.StartingPushPins);
        }

        if (rb.gs.PoolSettings.Keys && RS.EnableCoinsAndGems)
        {
            var neededCoins = RandoInterop.LS.GetItemCount(CostType.Coins);
            if (neededCoins > 0)
            {
                rb.AddItemByName(CoinItem.ITEM_NAME, neededCoins + BugPrinceMod.RS.CoinTolerance);
                rb.AddItemByName($"{PlaceholderItem.Prefix}{CoinItem.ITEM_NAME}", BugPrinceMod.RS.CoinDuplicates);
            }

            var neededGems = RandoInterop.LS.GetItemCount(CostType.Gems);
            if (neededGems > 0)
            {
                rb.AddItemByName(GemItem.ITEM_NAME, neededGems + BugPrinceMod.RS.GemTolerance);
                rb.AddItemByName($"{PlaceholderItem.Prefix}{GemItem.ITEM_NAME}", BugPrinceMod.RS.GemDuplicates);
            }
        }
    }

    private static void AddTolerances(LogicManager lm, GenerationSettings gs, ProgressionInitializer pi)
    {
        if (BugPrinceMod.RS.AreCostsEnabled)
        {
            pi.Setters.Add(new(CostType.Coins.GetTerm(lm), -BugPrinceMod.RS.CoinTolerance));
            pi.Setters.Add(new(CostType.Gems.GetTerm(lm), -BugPrinceMod.RS.GemTolerance));
        }
        if (BugPrinceMod.RS.MapShop) pi.Setters.Add(new(lm.GetTermStrict("MAPS"), -BugPrinceMod.RS.MapTolerance));
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
