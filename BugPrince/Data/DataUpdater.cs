using JsonUtil = PurenailCore.SystemUtil.JsonUtil<BugPrince.BugPrinceMod>;

namespace BugPrince.Data;

public static class DataUpdater
{
    public static void Run()
    {
        string root = JsonUtil.InferGitRoot();
        string costGroupsPath = $"{root}/BugPrince/Resources/Data/cost_groups.json";

        // Update names.
        var producers = CostGroup.LoadProducers();
        foreach (var e in producers)
        {
            if (e.Value is CostGroupProducer p) p.Name = e.Key;
            else if (e.Value is TieredCostGroupProducer p) p.Name = e.Key;
        }

        JsonUtil.RewriteJsonFile(producers, costGroupsPath);
    }
}