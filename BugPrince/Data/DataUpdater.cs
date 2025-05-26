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
            if (e.Value is CostGroupProducer cgp) cgp.Name = e.Key;
            else if (e.Value is TieredCostGroupProducer tcgp) tcgp.Name = e.Key;
        }

        JsonUtil.RewriteJsonFile(producers, costGroupsPath);
    }
}