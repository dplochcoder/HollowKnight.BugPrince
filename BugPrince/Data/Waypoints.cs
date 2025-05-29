using System.Collections.Generic;

namespace BugPrince.Data;

internal class Waypoints
{
    private static SortedDictionary<string, string>? data;
    public static IReadOnlyDictionary<string, string> GetWaypoints() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, string>>("BugPrince.Resources.Data.waypoints.json"));
}
