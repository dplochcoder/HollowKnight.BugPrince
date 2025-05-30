using ItemChanger;
using ItemChanger.Locations;
using JsonUtil = PurenailCore.SystemUtil.JsonUtil<BugPrince.BugPrinceMod>;

namespace BugPrince.Data;

public static class DataUpdater
{
    public static void Run()
    {
        string root = JsonUtil.InferGitRoot();

        UpdateJson(CostGroup.GetProducers(), root, "cost_groups");

        var locations = Locations.GetLocations();
        foreach (var e in locations) UpdateNames(e.Key, e.Value.Location!);
        UpdateJson(Locations.GetLocations(), root, "locations");

        UpdateJson(Waypoints.GetWaypoints(), root, "waypoints");
    }

    private static void UpdateJson<T>(T obj, string root, string name)
    {
        var path = $"{root}/BugPrince/Resources/Data/{name}.json";
        JsonUtil.RewriteJsonFile(obj, path);
    }

    private static void UpdateNames(string name, AbstractLocation loc)
    {
        loc.name = name;
        if (loc is DualLocation dloc)
        {
            UpdateNames(name, dloc.falseLocation);
            UpdateNames(name, dloc.trueLocation);
        }
    }
}