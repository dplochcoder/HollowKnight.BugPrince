using Galaxy.Api;
using MenuChanger.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BugPrince;

public record GlobalSettings
{
    public RandomizationSettings RandoSettings = new();
}

internal class TransitionSettingAttribute : Attribute { }

internal class RelicSettingAttribute : Attribute { }

internal class CostSettingAttribute : Attribute { }

internal enum LocationPool
{
    BasicLocations,
    AdvancedLocations,
    MapShop,
    ShamanPuzzles,
    TheVault,
    GemstoneCavern
}

internal class LocationSettingAttribute : Attribute
{
    public LocationPool LocationPool { get; init; }
    internal LocationSettingAttribute(LocationPool locationPool) => LocationPool = locationPool;
}

internal class MapShopSettingAttribute : Attribute { }

public record RandomizationSettings
{
    public bool EnableTransitionChoices = false;

    [TransitionSetting]
    [MenuRange(2, 5)]
    public int NumRoomChoices = 3;
    [TransitionSetting]
    [MenuRange(0, 20)]
    public int RefreshCycle = 5;

    [RelicSetting]
    [MenuRange(0, 3)]
    [DynamicBound(nameof(TotalDiceTotems), true)]
    public int StartingDiceTotems = 0;
    [RelicSetting]
    [MenuRange(0, 10)]
    [DynamicBound(nameof(StartingDiceTotems), false)]
    public int TotalDiceTotems = 7;
    [RelicSetting]
    [MenuRange(0, 2)]
    [DynamicBound(nameof(TotalPushPins), true)]
    public int StartingPushPins = 0;
    [RelicSetting]
    [MenuRange(0, 7)]
    [DynamicBound(nameof(StartingPushPins), false)]
    public int TotalPushPins = 5;

    public bool EnableCoinsAndGems;
    [CostSetting]
    [MenuRange(0, 5)]
    public int CoinTolerance = 1;
    [CostSetting]
    [MenuRange(0, 10)]
    public int CoinDuplicates = 1;
    [CostSetting]
    [MenuRange(0, 5)]
    public int GemTolerance = 2;
    [CostSetting]
    [MenuRange(0, 10)]
    public int GemDuplicates = 2;

    [LocationSetting(LocationPool.MapShop)]
    public bool MapShop = true;
    [MapShopSetting]
    [DynamicBound(nameof(MaximumMaps), true)]
    public int MinimumMaps = 1;
    [MapShopSetting]
    [DynamicBound(nameof(MinimumMaps), false)]
    [DynamicBound(nameof(MaximumMapsLimit), true)]
    public int MaximumMaps = 10;
    [MapShopSetting]
    [MenuRange(0, 10)]
    public int MapTolerance = 2;
    private int MaximumMapsLimit() => 13 - MapTolerance;

    [LocationSetting(LocationPool.BasicLocations)]
    public bool BasicLocations = true;
    [LocationSetting(LocationPool.AdvancedLocations)]
    public bool AdvancedLocations = true;
    [LocationSetting(LocationPool.ShamanPuzzles)]
    public bool ShamanPuzzles = true;
    [LocationSetting(LocationPool.TheVault)]
    public bool TheVault = true;
    [LocationSetting(LocationPool.GemstoneCavern)]
    public bool GemstoneCavern = true;

    static RandomizationSettings()
    {
        foreach (var field in typeof(RandomizationSettings).GetFields())
            if (field.GetCustomAttribute<LocationSettingAttribute>() is LocationSettingAttribute attr) poolFields.Add(attr.LocationPool, field);
    }

    private static readonly Dictionary<LocationPool, FieldInfo> poolFields = [];

    internal bool IsEnabled(LocationPool locationPool) => poolFields.TryGetValue(locationPool, out var field) && field.GetValue(this) is true;

    internal bool IsAnyLocationPoolEnabled() => poolFields.Values.Any(f => f.GetValue(this) is true);
}
