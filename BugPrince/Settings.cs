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
    [MenuLabel("Number of rooms to draw")]
    public int NumChoices = 3;
    [TransitionSetting]
    [MenuRange(0, 20)]
    [MenuLabel("Room refresh time")]
    public int RefreshCycle = 5;

    [RelicSetting]
    [MenuRange(0, 3)]
    [DynamicBound(nameof(NumDiceTotems), true)]
    [MenuLabel("Starting number of Dice Totems")]
    public int StartingDiceTotems = 0;
    [RelicSetting]
    [MenuRange(0, 10)]
    [DynamicBound(nameof(StartingDiceTotems), false)]
    [MenuLabel("Total number of Dice Totems")]
    public int NumDiceTotems = 7;
    [RelicSetting]
    [MenuRange(0, 2)]
    [DynamicBound(nameof(NumPushPins), true)]
    [MenuLabel("Starting number of Push Pins")]
    public int StartingPushPins = 0;
    [RelicSetting]
    [MenuRange(0, 7)]
    [DynamicBound(nameof(StartingPushPins), false)]
    [MenuLabel("Total number of Push Pins")]
    public int NumPushPins = 5;

    [MenuLabel("Enable Coins and Gems")]
    public bool CostsEnabled;
    [CostSetting]
    [MenuRange(0, 5)]
    [MenuLabel("Coin Tolerance")]
    public int CoinTolerance = 1;
    [CostSetting]
    [MenuRange(0, 10)]
    [MenuLabel("Coin Duplicates")]
    public int CoinDuplicates = 1;
    [CostSetting]
    [MenuRange(0, 5)]
    [MenuLabel("Gem Tolerance")]
    public int GemTolerance = 2;
    [CostSetting]
    [MenuRange(0, 10)]
    [MenuLabel("Gem Duplicates")]
    public int GemDuplicates = 2;

    [LocationSetting]
    [MenuLabel("Custom Locations")]
    public bool NewLocations = true;
    [LocationSetting]
    [MenuLabel("The Vault")]
    public bool TheVault = true;
    [LocationSetting]
    [MenuLabel("Gemstone Cavern")]
    public bool GemstoneCavern = true;
}
