using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.Settings;

namespace BugPrince.Rando;

internal static class LogicPatcher
{
    internal static void Setup() => RCData.RuntimeLogicOverride.Subscribe(-2000f, ModifyLogic);

    private static void ModifyLogic(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        lmb.VariableResolver = new BugPrinceVariableResolver(lmb.VariableResolver);
    }
}
