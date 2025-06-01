using BugPrince.IC.Items;
using ItemChanger;
using RandomizerMod.RandomizerData;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;

namespace BugPrince.Data;

internal enum ItemType
{
    Coin,
    DiceTotem,
    Gem,
    PushPin
}

internal static class ItemTypeExtensions
{
    internal static string ItemName(this ItemType self) => self switch
    {
        ItemType.Coin => CoinItem.ITEM_NAME,
        ItemType.DiceTotem => DiceTotemItem.ITEM_NAME,
        ItemType.Gem => GemItem.ITEM_NAME,
        ItemType.PushPin => PushPinItem.ITEM_NAME
    };
}

internal enum LocationState
{
    Skip,
    Preplaced,
    Randomized
}

internal record LocationData
{
    public ItemType Type;
    public int Count;
    public AbstractLocation? Location;
    public bool FullFlexible;
    public string Logic = "";

    internal LocationDef LocationDef() => new()
    {
        AdditionalProgressionPenalty = FullFlexible,
        FlexibleCount = FullFlexible,
        Name = Location?.name ?? "",
        SceneName = Location?.sceneName ?? ""
    };

    private LocationState GetLocationState(GenerationSettings gs, RandomizationSettings rs)
    {
        if (!rs.CustomLocations) return LocationState.Skip;

        switch (Type)
        {
            case ItemType.Coin:
            case ItemType.Gem:
                return (rs.EnableCoinsAndGems && !gs.PoolSettings.Keys) ? LocationState.Preplaced : LocationState.Randomized;
            case ItemType.DiceTotem:
            case ItemType.PushPin:
                return gs.PoolSettings.Relics ? LocationState.Randomized : LocationState.Preplaced;
            default: throw new ArgumentException($"Bad type: {Type}");
        }
    }

    internal void AddToRequestBuilder(RandomizationSettings rs, RequestBuilder rb)
    {
        switch (GetLocationState(rb.gs, rs))
        {
            case LocationState.Skip: break;
            case LocationState.Preplaced:
                for (int i = 0; i < Count; i++) rb.AddToPreplaced(new(Type.ItemName(), Location!.name));
                break;
            case LocationState.Randomized:
                rb.AddLocationByName(Location!.name);
                break;
        }
    }

    internal void AddVanillaToItemChanger(GenerationSettings gs, RandomizationSettings rs)
    {
        var state = GetLocationState(gs, rs);
        if (state != LocationState.Preplaced) return;

        ItemChangerMod.CreateSettingsProfile(false);
        var placement = Location!.Wrap();
        for (int i = 0; i < Count; i++) placement.Add(Finder.GetItem(Type.ItemName())!);
    }
}

internal class Locations
{
    private static SortedDictionary<string, LocationData>? data;
    public static IReadOnlyDictionary<string, LocationData> GetLocations() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, LocationData>>("BugPrince.Resources.Data.locations.json"));
}
