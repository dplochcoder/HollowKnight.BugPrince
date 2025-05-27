using MenuChanger.Attributes;
using System;

namespace BugPrince;

public record GlobalSettings
{
    public RandomizationSettings RandoSettings = new();
}

internal class MainSettingAttribute : Attribute { }

internal class CostFieldAttribute : Attribute { }

internal class LocationFieldAttribute : Attribute { }

public record RandomizationSettings
{
    public bool Enabled = false;

    [MainSetting]
    [MenuRange(2, 5)]
    [MenuLabel("Number of rooms to draw")]
    public int NumChoices = 3;
    [MainSetting]
    [MenuRange(0, 20)]
    [MenuLabel("Room refresh time")]
    public int RefreshCycle = 5;
    [MainSetting]
    [MenuRange(0, 10)]
    [MenuLabel("Number of Dice Totems")]
    public int NumDiceTotems = 5;
    [MainSetting]
    [MenuRange(0, 7)]
    [MenuLabel("Number of Push Pins")]
    public int NumPushPins = 3;

    [MenuLabel("Enable Coins and Gems")]
    public bool CostsEnabled;
    [CostField]
    [MenuRange(0, 5)]
    [MenuLabel("Tolerance")]
    public int CoinTolerance = 1;
    [CostField]
    [MenuRange(0, 10)]
    [MenuLabel("Duplicates")]
    public int CoinDuplicates = 1;
    [CostField]
    [MenuRange(0, 5)]
    [MenuLabel("Tolerance")]
    public int GemTolerance = 2;
    [CostField]
    [MenuRange(0, 10)]
    [MenuLabel("Duplicates")]
    public int GemDuplicates = 2;

    [LocationField]
    [MenuLabel("Custom Locations")]
    public bool NewLocations = true;
    [LocationField]
    [MenuLabel("The Vault")]
    public bool TheVault = true;
    [LocationField]
    [MenuLabel("Gemstone Cavern")]
    public bool GemstoneCavern = true;
}
