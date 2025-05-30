using bugPrince.Imports;
using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using ItemChanger;
using Newtonsoft.Json;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.IO;

namespace BugPrince.Rando;

internal record LocalSettings : ICostGroupProgressionProvider
{
    public Dictionary<string, CostGroup> CostGroups = [];
    public Dictionary<string, string> CostGroupsByScene = [];
    public HashSet<Transition> RandomizedTransitions = [];
    public List<string> CostGroupProgression = [];

    IReadOnlyDictionary<string, CostGroup> ICostGroupProgressionProvider.CostGroups() => CostGroups;

    IReadOnlyDictionary<string, string> ICostGroupProgressionProvider.CostGroupsByScene() => CostGroupsByScene;

    public bool IsRandomizedTransition(Transition transition) => RandomizedTransitions.Contains(transition);

    IReadOnlyList<string> ICostGroupProgressionProvider.CostGroupProgression() => CostGroupProgression;
}

internal static class RandoInterop
{
    internal static LocalSettings? LS;

    internal static RandomizationSettings RS => BugPrinceMod.GS.RandoSettings;

    internal static bool IsEnabled => RS.Enabled;

    internal static bool AreCostsEnabled => RS.Enabled && RS.CostsEnabled;

    internal static void Setup()
    {
        ConnectionMenu.Setup();
        LogicPatcher.Setup();
        RequestModifier.Setup();

        DefineCustomItems();

        RandoController.OnExportCompleted += OnExportCompleted;

        RandomizerMod.Logging.SettingsLog.AfterLogSettings += LogSettings;
        RandomizerMod.Logging.LogManager.AddLogger(new BugPrinceLogger());
        CondensedSpoilerLogger.AddCategory("BugPrince Currency", _ => IsEnabled, [CoinItem.ITEM_NAME, GemItem.ITEM_NAME]);
    }

    private static void DefineCustomItems()
    {
        Finder.DefineCustomItem(new CoinItem());
        Finder.DefineCustomItem(new DiceTotemItem());
        Finder.DefineCustomItem(new GemItem());
        Finder.DefineCustomItem(new PushPinItem());
    }

    private static void OnExportCompleted(RandoController rc)
    {
        if (!IsEnabled) return;

        var module = ItemChangerMod.Modules.Add<BugPrinceModule>();
        module.CostGroups = LS!.CostGroups;
        module.CostGroupsByScene = LS.CostGroupsByScene;
        module.RandomizedTransitions = LS.RandomizedTransitions;
        module.CostGroupProgression = LS.CostGroupProgression;
        module.Seed = rc.gs.Seed;

        ItemChangerMod.Modules.Add<BreakablesModule>();
    }

    private static void LogSettings(RandomizerMod.Logging.LogArguments args, TextWriter tw)
    {
        if (!IsEnabled) return;

        tw.WriteLine("Logging BugPrince Settings:");
        using JsonTextWriter jtw = new(tw) { CloseOutput = false };
        RandomizerMod.RandomizerData.JsonUtil._js.Serialize(jtw, BugPrinceMod.GS.RandoSettings);
        tw.WriteLine();
    }
}
