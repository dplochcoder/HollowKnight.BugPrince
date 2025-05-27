using JsonUtil = PurenailCore.SystemUtil.JsonUtil<BugPrince.BugPrinceMod>;

namespace BugPrince.Data;

public static class DataUpdater
{
    public static void Run()
    {
        string root = JsonUtil.InferGitRoot();
        string costGroupsPath = $"{root}/BugPrince/Resources/Data/cost_groups.json";

        JsonUtil.RewriteJsonFile(CostGroup.GetProducers(), costGroupsPath);
    }
}