using bugPrince.Imports;
using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using ItemChanger;
using Newtonsoft.Json;
using PurenailCore.SystemUtil;
using RandomizerMod.RC;
using System.Collections.Generic;
using System.IO;

namespace BugPrince.Rando;

internal record LocalSettings : ICostGroupProgressionProvider
{
    public int gen = CostGroupProgressionProviderGeneration.NextGen();
    public Dictionary<string, CostGroup> CostGroups = [];
    public Dictionary<string, string> CostGroupsByScene = [];
    public HashSet<Transition> RandomizedTransitions = [];
    public List<string> CostGroupProgression = [];

    int ICostGroupProgressionProvider.Generation() => gen;

    IReadOnlyDictionary<string, CostGroup> ICostGroupProgressionProvider.GetCostGroups() => CostGroups;

    IReadOnlyDictionary<string, string> ICostGroupProgressionProvider.GetCostGroupsByScene() => CostGroupsByScene;

    public bool IsRandomizedTransition(Transition transition) => RandomizedTransitions.Contains(transition);

    IReadOnlyList<string> ICostGroupProgressionProvider.GetCostGroupProgression() => CostGroupProgression;
}

internal static class RandoInterop
{
    internal static LocalSettings? LS;

    internal static void Setup()
    {
        ConnectionMenu.Setup();
        LogicPatcher.Setup();
        RequestModifier.Setup();

        DefineCustomItems();
        DefineCustomLocations();

        RandoController.OnExportCompleted += OnExportCompleted;

        RandomizerMod.Logging.SettingsLog.AfterLogSettings += LogSettings;
        RandomizerMod.Logging.LogManager.AddLogger(new BugPrinceLogger());
        CondensedSpoilerLogger.AddCategory("BugPrince Currency", _ => BugPrinceMod.RS.IsEnabled, [CoinItem.ITEM_NAME, GemItem.ITEM_NAME]);
    }

    private static void DefineCustomItems()
    {
        Finder.DefineCustomItem(new CoinItem());
        Finder.DefineCustomItem(new DiceTotemItem());
        Finder.DefineCustomItem(new GemItem());
        Finder.DefineCustomItem(new PushPinItem());
    }

    private static void DefineCustomLocations() => Locations.GetLocations().Values.ForEach(l =>
    {
        l.AddInteropData();
        Finder.DefineCustomLocation(l.Location!);
    });

    private static bool IsItemSyncImpl() => ItemSyncMod.ItemSyncMod.ISSettings.IsItemSync;

    private static void OnExportCompleted(RandoController rc)
    {
        if (BugPrinceMod.RS.EnableTransitionChoices)
        {
            var module = ItemChangerMod.Modules.Add<TransitionSelectionModule>();
            module.CostGroups = LS!.CostGroups;
            module.CostGroupsByScene = LS.CostGroupsByScene;
            module.RandomizedTransitions = LS.RandomizedTransitions;
            module.MutableState.CostGroupProgression = LS.CostGroupProgression;
            module.Seed = rc.gs.Seed;
            module.DiceTotems = BugPrinceMod.RS.StartingDiceTotems;
            module.TotalPushPins = BugPrinceMod.RS.StartingPushPins;
        }
        if (BugPrinceMod.RS.AdvancedLocations) ItemChangerMod.Modules.Add<BreakablesModule>();
        if (BugPrinceMod.RS.MapShop)
        {
            ItemChangerMod.Modules.Add<MapShopModule>();
            if (!rc.gs.PoolSettings.Maps) MapShopModule.PlaceVanillaMaps();
        }
        if (BugPrinceMod.RS.GemstoneCavern) ItemChangerMod.Modules.Add<GemstoneCavernModule>();
        if (BugPrinceMod.RS.TheVault) ItemChangerMod.Modules.Add<VaultModule>();

        Locations.GetLocations().Values.ForEach(l => l.AddVanillaToItemChanger(rc.gs, BugPrinceMod.RS));
    }

    private static void LogSettings(RandomizerMod.Logging.LogArguments args, TextWriter tw)
    {
        if (!BugPrinceMod.RS.IsEnabled) return;

        tw.WriteLine("Logging BugPrince Settings:");
        using JsonTextWriter jtw = new(tw) { CloseOutput = false };
        RandomizerMod.RandomizerData.JsonUtil._js.Serialize(jtw, BugPrinceMod.GS.RandoSettings);
        tw.WriteLine();
    }
}
