using BugPrince.Util;
using ItemChanger;
using Modding;
using Newtonsoft.Json;

namespace BugPrince.Rando;

internal class NailUpgradesLogicString : IString
{
    public string WithRandomizedNailUpgrades = "";
    public string WithoutRandomizedNailUpgrades = "";

    private static bool RandomizeNailUpgrades(Mod mod) => mod.TryGetSettings<RandoPlus.GlobalSettings>(out var settings) && settings.NailUpgrades;

    [JsonIgnore]
    public string Value => (ModHooks.GetMod("RandoPlus") is Mod mod && RandomizeNailUpgrades(mod)) ? WithRandomizedNailUpgrades : WithoutRandomizedNailUpgrades;

    public IString Clone() => (IString)MemberwiseClone();
}
