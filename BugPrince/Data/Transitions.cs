using System.Collections.Generic;
using RandomizerCore.Logic;
using RandomizerMod.RandomizerData;

namespace BugPrince.Data;

internal record TransitionData
{
    public LocationPool LocationPool;
    public TransitionDef? Def;
    public string Logic = "";
    public List<RawLogicDef> LogicEdits = [];
}

internal static class Transitions
{
    private static SortedDictionary<string, TransitionData>? data;

    public static IReadOnlyDictionary<string, TransitionData> GetTransitions() =>
        (
            data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<
                SortedDictionary<string, TransitionData>
            >("BugPrince.Resources.Data.transitions.json")
        );
}
