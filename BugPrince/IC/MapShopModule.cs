using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Items;
using Modding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.IC;

internal class MapShopModule : ItemChanger.Modules.Module
{
    private static readonly List<(string, string)> VANILLA_MAPS =
    [
        (LocationNames.Crossroads_Map, ItemNames.Crossroads_Map),
        (LocationNames.Greenpath_Map, ItemNames.Greenpath_Map),
        (LocationNames.Fungal_Wastes_Map, ItemNames.Fungal_Wastes_Map),
        (LocationNames.Howling_Cliffs_Map, ItemNames.Howling_Cliffs_Map),
        (LocationNames.City_of_Tears_Map, ItemNames.City_of_Tears_Map),
        (LocationNames.Crystal_Peak_Map, ItemNames.Crystal_Peak_Map),
        (LocationNames.Resting_Grounds_Map, ItemNames.Resting_Grounds_Map),
        (LocationNames.Deepnest_Map_Right, ItemNames.Deepnest_Map),
        (LocationNames.Deepnest_Map_Upper, ItemNames.Deepnest_Map),
        (LocationNames.Fog_Canyon_Map, ItemNames.Fog_Canyon_Map),
        (LocationNames.Kingdoms_Edge_Map, ItemNames.Kingdoms_Edge_Map),
        (LocationNames.Ancient_Basin_Map, ItemNames.Ancient_Basin_Map),
        (LocationNames.Queens_Gardens_Map, ItemNames.Queens_Gardens_Map)
    ];

    internal static void PlaceVanillaMaps()
    {
        ItemChangerMod.CreateSettingsProfile(false);
        ItemChangerMod.AddPlacements(VANILLA_MAPS.Select(pair => Finder.GetLocation(pair.Item1)!.Wrap().Add(Finder.GetItem(pair.Item2)!)));
    }

    private static readonly FsmID FSM_ID = new("Shop Region", "Shop Region");

    // Store these separately so RandoMapMod still works.
    public HashSet<string> RealFieldNames = [];

    [JsonIgnore]
    public int NumMaps => RealFieldNames.Count;

    public override void Initialize() => AbstractItem.ModifyItemGlobal += MaybeUpgradeMap;

    public override void Unload() => AbstractItem.ModifyItemGlobal -= MaybeUpgradeMap;

    private void MaybeUpgradeMap(GiveEventArgs args)
    {
        if (args.Item is MapItem mapItem) args.Item = new MapShopMapItem(mapItem);
    }
}

// Wrapper around MapItem to allow use of RandoMapMod.
internal class MapShopMapItem : AbstractItem
{
    public MapItem mapItem;

    [JsonConstructor]
    private MapShopMapItem() { }

    public MapShopMapItem(MapItem mapItem)
    {
        this.mapItem = mapItem;
        UIDef = mapItem.UIDef;
    }

    public override void GiveImmediate(GiveInfo info)
    {
        mapItem.GiveImmediate(info);
        ItemChangerMod.Modules.Get<MapShopModule>()?.RealFieldNames.Add(mapItem.fieldName);
    }

    public override bool Redundant() => ItemChangerMod.Modules.Get<MapShopModule>()?.RealFieldNames.Contains(mapItem.fieldName) ?? mapItem.Redundant();
}

internal record MapCost : Cost
{
    public int Cost;

    public MapCost(int cost) => Cost = cost;

    public override bool CanPay() => (ItemChangerMod.Modules.Get<MapShopModule>()?.NumMaps ?? 13) >= Cost;

    public override string GetCostText() => $"Requires {Cost} maps owned.";

    public override bool HasPayEffects() => false;

    public override void OnPay() { }
}
