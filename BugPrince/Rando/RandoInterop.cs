using BugPrince.Data;
using BugPrince.IC;
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
    public HashSet<string> RandomizedTransitions = [];
    public List<string> CostGroupProgression = [];

    IReadOnlyDictionary<string, CostGroup> ICostGroupProgressionProvider.CostGroups() => CostGroups;

    IReadOnlyDictionary<string, string> ICostGroupProgressionProvider.CostGroupsByScene() => CostGroupsByScene;

    IReadOnlyCollection<string> ICostGroupProgressionProvider.RandomizedTransitions() => RandomizedTransitions;

    IReadOnlyList<string> ICostGroupProgressionProvider.CostGroupProgression() => CostGroupProgression;
}

internal static class RandoInterop
{
    internal static LocalSettings? LS;

    private static RandomizationSettings RS => BugPrinceMod.GS.RandoSettings;

    internal static bool IsEnabled => RS.Enabled;

    internal static bool AreCostsEnabled => RS.Enabled && RS.CostsEnabled;

    internal static void Setup()
    {
        ConnectionMenu.Setup();
        RequestModifier.Setup();

        DefineCustomItems();

        RandoController.OnExportCompleted += OnExportCompleted;
        RandomizerMod.Logging.SettingsLog.AfterLogSettings += LogSettings;
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

        var module = ItemChangerMod.Modules.GetOrAdd<BugPrinceModule>();
        module.CostGroups = LS!.CostGroups;
        module.CostGroupsByScene = LS.CostGroupsByScene;
        module.RandomizedTransitions = LS.RandomizedTransitions;
        module.CostGroupProgression = LS.CostGroupProgression;

        LS = null;
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
