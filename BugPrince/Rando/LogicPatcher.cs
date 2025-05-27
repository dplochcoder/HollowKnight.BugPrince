using BugPrince.IC;
using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace BugPrince.Rando;

internal static class LogicPatcher
{
    internal static void Setup() => RCData.RuntimeLogicOverride.Subscribe(-2000f, ModifyLogic);

    private static void ModifyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!RandoInterop.IsEnabled) return;

        RandoInterop.LS = new();
        lmb.VariableResolver = new BugPrinceVariableResolver(lmb.VariableResolver);
        lmb.AddCostTypeTerms();
        lmb.AddItem(CoinItem.LogicItem());
        lmb.AddItem(DiceTotemItem.LogicItem());
        lmb.AddItem(GemItem.LogicItem());
        lmb.AddItem(PushPinItem.LogicItem());
    }
}
