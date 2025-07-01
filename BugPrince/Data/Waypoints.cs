using System.Collections.Generic;

namespace BugPrince.Data;

internal record Waypoint
{
    public LocationPool LocationPool;
    public string Logic = "";
}

internal class Waypoints
{
    private static SortedDictionary<string, Waypoint>? data;
    public static IReadOnlyDictionary<string, Waypoint> GetWaypoints() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, Waypoint>>("BugPrince.Resources.Data.waypoints.json"));
}
