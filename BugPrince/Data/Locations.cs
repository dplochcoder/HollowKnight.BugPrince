using BugPrince.IC;
using BugPrince.IC.Items;
using BugPrince.Util;
using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Tags;
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
        ItemType.PushPin => PushPinItem.ITEM_NAME,
        _ => throw self.InvalidEnum()
    };

    internal static string PoolName(this ItemType selF) => selF switch
    {
        ItemType.Coin or ItemType.Gem => "Keys",
        ItemType.DiceTotem or ItemType.PushPin => "Relics",
        _ => throw selF.InvalidEnum()
    };
}

internal enum LocationState
{
    Skip,
    Preplaced,
    Randomized
}

internal record PinLocation
{
    public bool IsWorld = true;
    public string SceneName = "";
    public float X;
    public float Y;

    public PinLocation(string SceneName, float X, float Y)
    {
        this.SceneName = SceneName;
        this.X = X;
        this.Y = Y;
    }


    public (string, float, float) AsTuple() => (SceneName, X, Y);
    public (string, float, float)[] AsTupleArray() => [AsTuple()];
}

internal interface IPinLocationProvider
{
    PinLocation ProvidePinLocation();
}

internal record LocationData
{
    public ItemType ItemType;
    public LocationPool LocationPool;
    public int Count;
    public AbstractLocation? Location;
    public bool FullFlexible;
    public IString Logic = new BoxedString("");
    public PinLocation? PinLocationOverride;
    public string? DisplaySource;

    private static void UpdateNames(string name, AbstractLocation loc)
    {
        loc.name = name;
        if (loc is DualLocation dloc)
        {
            UpdateNames(name, dloc.falseLocation);
            UpdateNames(name, dloc.trueLocation);
        }
    }

    private static PinLocation? DerivePinLocation(AbstractLocation location)
    {
        if (location is IPinLocationProvider provider) return provider.ProvidePinLocation();
        if (location is CoordinateLocation loc) return new(loc.sceneName!, loc.x, loc.y);
        if (location is DualLocation dual) return DerivePinLocation(dual.falseLocation) ?? DerivePinLocation(dual.trueLocation);
        return null;
    }

    internal void AddInteropData() => Location!.AddInteropData(ItemType.PoolName(), PinLocationOverride ?? DerivePinLocation(Location!), DisplaySource);

    internal void Update(string name)
    {
        UpdateNames(name, Location!);
        Location!.RemoveTags<InteropTag>();
    }

    internal LocationDef LocationDef() => new()
    {
        AdditionalProgressionPenalty = FullFlexible,
        FlexibleCount = FullFlexible,
        Name = Location?.name ?? "",
        SceneName = Location?.sceneName ?? ""
    };

    private LocationState GetLocationState(GenerationSettings gs, RandomizationSettings rs)
    {
        if (!rs.IsLocationPoolEnabled(LocationPool)) return LocationState.Skip;

        return ItemType switch
        {
            ItemType.Coin or ItemType.Gem => (rs.EnableCoinsAndGems && !gs.PoolSettings.Keys) ? LocationState.Preplaced : LocationState.Randomized,
            ItemType.DiceTotem or ItemType.PushPin => gs.PoolSettings.Relics ? LocationState.Randomized : LocationState.Preplaced,
            _ => throw new ArgumentException($"Bad type: {ItemType}"),
        };
    }

    internal void AddToRequestBuilder(RandomizationSettings rs, RequestBuilder rb)
    {
        switch (GetLocationState(rb.gs, rs))
        {
            case LocationState.Skip: break;
            case LocationState.Preplaced:
                for (int i = 0; i < Count; i++) rb.AddToPreplaced(new(ItemType.ItemName(), Location!.name));
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

        var placement = Location!.Wrap();
        for (int i = 0; i < Count; i++)
        {
            var item = Finder.GetItem(ItemType.ItemName())!;

            if (LocationPool == LocationPool.MapShop)
            {
                var cost = item.AddTag<CostTag>();
                cost.Cost += Cost.NewGeoCost((i + 1) * 480);
                cost.Cost += new MapCost((i + 1) * 5);
            }
            placement.Add(item);
        }

        ItemChangerMod.CreateSettingsProfile(false);
        ItemChangerMod.AddPlacements([placement]);
    }
}

internal class Locations
{
    private static SortedDictionary<string, LocationData>? data;
    public static IReadOnlyDictionary<string, LocationData> GetLocations() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, LocationData>>("BugPrince.Resources.Data.locations.json"));
}
