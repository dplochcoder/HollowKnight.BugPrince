using BugPrince.Data;
using BugPrince.Rando;
using Modding;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince;

public class BugPrinceMod : Mod, IGlobalSettings<GlobalSettings>
{
    public static BugPrinceMod? Instance;
    public static GlobalSettings GS = new();

    private static readonly string version = PurenailCore.ModUtil.VersionUtil.ComputeVersion<BugPrinceMod>();

    public override string GetVersion() => version;

    public BugPrinceMod() : base("BugPrince")
    {
        Instance = this;
    }

    public void OnLoadGlobal(GlobalSettings s) => GS = s;

    public GlobalSettings OnSaveGlobal() => GS;

    public override List<(string, string)> GetPreloadNames() => BugPrincePreloader.Instance.GetPreloadNames();

    private static void HookRandoSettingsManager() => SettingsProxy.Hook();

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        BugPrincePreloader.Instance.Initialize(preloadedObjects);

        if (ModHooks.GetMod("RandoSettingsManager") is Mod) HookRandoSettingsManager();
    }

    // Public API
    public static void AddCostGroupProducer(Mod source, string name, ICostGroupProducer producer) => CostGroup.AddProducer($"{source.Name}-{name}", producer);
}
