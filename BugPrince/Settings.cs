using MenuChanger.Attributes;

namespace BugPrince;

public record GlobalSettings
{
    public RandomizationSettings RandoSettings = new();
}

public record RandomizationSettings
{
    public bool CrownThePrince = false;

    [MenuRange(2, 5)] public int Choices = 3;
    [MenuRange(0, 20)] public int RefreshCycle = 5;
    [MenuRange(0, 7)] public int AddDiceTotems = 3;
    [MenuRange(0, 7)] public int AddPushPins = 3;
    public bool EnableCoinsAndGems;
    [MenuRange(0, 5)] public int CoinTolerance = 1;
    [MenuRange(0, 10)] public int CoinDuplicates = 1;
    [MenuRange(0, 5)] public int GemTolerance = 2;
    [MenuRange(0, 10)] public int GemDuplicates = 2;
    public bool NewLocations = true;
    public bool TheVault = true;
    public bool GemMine = true;
}
