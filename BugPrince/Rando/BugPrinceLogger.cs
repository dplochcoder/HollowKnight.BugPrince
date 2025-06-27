using RandomizerMod.Logging;
using RandomizerMod.RandomizerData;

namespace BugPrince.Rando;

internal class BugPrinceLogger : RandoLogger
{
    public override void Log(LogArguments args)
    {
        if (!BugPrinceMod.RS.IsEnabled) return;

        var ls = RandoInterop.LS;
        if (ls == null) return;
        LogManager.Write(tw => JsonUtil.Serialize(tw, ls), "BugPrinceSpoiler.json");
    }
}
