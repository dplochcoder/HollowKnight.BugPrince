using BugPrince.Data;
using BugPrince.Rando;
using BugPrince.UI;
using ItemChanger;
using ItemChanger.Internal.Menu;
using Modding;
using RandomizerMod.Logging;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince;

public class BugPrinceMod : Mod, IGlobalSettings<GlobalSettings>, ICustomMenuMod
{
    public static BugPrinceMod? Instance;
    public static GlobalSettings GS = new();
    public static RandomizationSettings RS => GS.RandoSettings;

    private static readonly string version = PurenailCore.ModUtil.VersionUtil.ComputeVersion<BugPrinceMod>();

    public override string GetVersion() => version;

    public BugPrinceMod() : base("BugPrince")
    {
        Instance = this;
    }

    public void OnLoadGlobal(GlobalSettings s) => GS = s;

    public GlobalSettings OnSaveGlobal() => GS;

    public override List<(string, string)> GetPreloadNames() => BugPrincePreloader.Instance.GetPreloadNames();

    private static void SetupDebug() => DebugInterop.DebugInterop.Setup();

    private static void SetupRandoSettingsManager() => SettingsProxy.Setup();

    public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects)
    {
        BugPrincePreloader.Instance.Initialize(preloadedObjects);

        RandoInterop.Setup();
        if (ModHooks.GetMod("DebugMod") is Mod) SetupDebug();
        if (ModHooks.GetMod("RandoSettingsManager") is Mod) SetupRandoSettingsManager();
    }

    public bool ToggleButtonInsideMenu => false;

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        ModMenuScreenBuilder builder = new("Bug Prince", modListMenu);
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Precomputation",
            Description = "If yes, BugPrince will precompute room choices when entering a scene to save time.",
            Values = ["No", "Yes"],
            Saver = i => GS.EnablePrecomputation = i == 1,
            Loader = () => GS.EnablePrecomputation ? 1 : 0
        });
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Pathfinder Updates",
            Description = "If yes, BugPrince will update rando map mod with transition choices whenever you open the map.",
            Values = ["No", "Yes"],
            Saver = i => GS.EnablePathfinderUpdates = i == 1,
            Loader = () => GS.EnablePathfinderUpdates ? 1 : 0
        });
        return builder.CreateMenuScreen();
    }

    private const string DEBUG_LOG_FILENAME = "BugPrinceDebug.txt";

    internal static void StartDebugLog() => LogManager.Write("--- BugPrinceDebugLog ---\n", DEBUG_LOG_FILENAME);
    internal static void DebugLog(string message) => LogManager.Append($"{message}\n", DEBUG_LOG_FILENAME);

    // Public API
    public static void AddCostGroupProducer(Mod source, string name, ICostGroupProducer producer) => CostGroup.AddProducer($"{source.Name}-{name}", producer);
    public static void AddSceneSprite(string sceneName, ISprite sprite) => SceneChoiceInfo.AddSceneSprite(sceneName, sprite);
}
