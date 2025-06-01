using MenuChanger.Attributes;
using System;

namespace BugPrince;

public record GlobalSettings
{
    public RandomizationSettings RandoSettings = new();
}

internal class TransitionSettingAttribute : Attribute { }

internal class RelicSettingAttribute : Attribute { }

internal class CostSettingAttribute : Attribute { }

internal class LocationSettingAttribute : Attribute { }

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
    [DynamicBound(nameof(TotaliceTotems), true)]
    public int StartingDiceTotems = 0;
    [RelicSetting]
    [MenuRange(0, 10)]
    [DynamicBound(nameof(StartingDiceTotems), false)]
    public int TotaliceTotems = 7;
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

    [LocationSetting]
    public bool CustomLocations = true;
    [LocationSetting]
    public bool TheVault = true;
    [LocationSetting]
    public bool GemstoneCavern = true;
}
