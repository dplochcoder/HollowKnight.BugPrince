using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MenuChanger.Attributes;
using Newtonsoft.Json;

namespace BugPrince;

public record GlobalSettings
{
    public bool EnablePrecomputation = true;
    public bool EnablePathfinderUpdates = true;
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
    GemstoneCavern,
}

internal class LocationSettingAttribute : Attribute
{
    public LocationPool LocationPool { get; init; }

    internal LocationSettingAttribute(LocationPool locationPool) => LocationPool = locationPool;
}

internal class MapShopSettingAttribute : Attribute { }

file class CSRIgnoreAttribute : Attribute { }

public record RandomizationSettings
{
    public bool EnableTransitionChoices = false;

    [TransitionSetting]
    [MenuRange(2, 5)]
    public int NumRoomChoices = 3;

    [TransitionSetting]
    [MenuRange(0, 20)]
    public int RefreshCycle = 5;

    internal const int MAX_STARTING_DICE_TOTEMS = 3;

    [RelicSetting]
    [MenuRange(0, MAX_STARTING_DICE_TOTEMS)]
    [DynamicBound(nameof(TotalDiceTotems), true)]
    [CSRIgnore]
    public int StartingDiceTotems = 0;

    internal const int MAX_TOTAL_DICE_TOTEMS = 10;

    [RelicSetting]
    [MenuRange(0, MAX_TOTAL_DICE_TOTEMS)]
    [DynamicBound(nameof(StartingDiceTotems), false)]
    [CSRIgnore]
    public int TotalDiceTotems = 7;

    internal const int MAX_STARTING_PUSH_PINS = 2;

    [RelicSetting]
    [MenuRange(0, MAX_STARTING_PUSH_PINS)]
    [DynamicBound(nameof(TotalPushPins), true)]
    [CSRIgnore]
    public int StartingPushPins = 0;

    internal const int MAX_TOTAL_PUSH_PINS = 7;

    [RelicSetting]
    [MenuRange(0, MAX_TOTAL_PUSH_PINS)]
    [DynamicBound(nameof(StartingPushPins), false)]
    [CSRIgnore]
    public int TotalPushPins = 5;

    public bool EnableCoinsAndGems;

    [CostSetting]
    [MenuRange(0, 2)]
    public int CoinTolerance = 1;

    [CostSetting]
    [MenuRange(0, 5)]
    public int CoinDuplicates = 1;

    [CostSetting]
    [MenuRange(0, 2)]
    public int GemTolerance = 2;

    [CostSetting]
    [MenuRange(0, 5)]
    public int GemDuplicates = 2;

    [LocationSetting(LocationPool.MapShop)]
    public bool MapShop = false;

    [MapShopSetting]
    [DynamicBound(nameof(MaximumMaps), true)]
    [CSRIgnore]
    public int MinimumMaps = 1;

    [MapShopSetting]
    [DynamicBound(nameof(MinimumMaps), false)]
    [DynamicBound(nameof(MaximumMapsLimit), true)]
    [CSRIgnore]
    public int MaximumMaps = 10;

    internal const int MAX_MAP_TOLERANCE = 4;

    [MapShopSetting]
    [MenuRange(0, MAX_MAP_TOLERANCE)]
    [CSRIgnore]
    public int MapTolerance = 2;

    private int MaximumMapsLimit() => 13 - MapTolerance;

    [LocationSetting(LocationPool.BasicLocations)]
    public bool BasicLocations = false;

    [LocationSetting(LocationPool.AdvancedLocations)]
    public bool AdvancedLocations = false;

    [LocationSetting(LocationPool.ShamanPuzzles)]
    public bool ShamanPuzzles = false;

    [LocationSetting(LocationPool.TheVault)]
    public bool TheVault = false;

    [LocationSetting(LocationPool.GemstoneCavern)]
    public bool GemstoneCavern = false;

    internal bool IsLocationPoolEnabled(LocationPool locationPool) =>
        poolFields.TryGetValue(locationPool, out var field) && field.GetValue(this) is true;

    [JsonIgnore]
    internal bool IsEnabled =>
        EnableTransitionChoices || poolFields.Values.Any(f => f.GetValue(this) is true);

    [JsonIgnore]
    internal bool AreCostsEnabled => EnableTransitionChoices && EnableCoinsAndGems;

    private static readonly Dictionary<LocationPool, FieldInfo> poolFields = [];

    static RandomizationSettings()
    {
        foreach (var field in typeof(RandomizationSettings).GetFields())
            if (
                field.GetCustomAttribute<LocationSettingAttribute>()
                is LocationSettingAttribute attr
            )
                poolFields.Add(attr.LocationPool, field);
    }
}
