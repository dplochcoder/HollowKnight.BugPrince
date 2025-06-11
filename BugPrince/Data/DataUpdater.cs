using System.IO;
using JsonUtil = PurenailCore.SystemUtil.JsonUtil<BugPrince.BugPrinceMod>;

namespace BugPrince.Data;

public static class DataUpdater
{
    public static void Run()
    {
        string root = JsonUtil.InferGitRoot();

        UpdateJson(CostGroup.GetProducers(), root, "cost_groups");

        var locations = Locations.GetLocations();
        foreach (var e in locations) e.Value.Update(e.Key);
        UpdateJson(Locations.GetLocations(), root, "locations");

        UpdateJson(Transitions.GetTransitions(), root, "transitions");

        UpdateJson(Waypoints.GetWaypoints(), root, "waypoints");

        // Code generation.
        UnityScriptShimsGenerator.GenerateUnityShims(root);

        CopyDlls(root);
    }

    private static void UpdateJson<T>(T obj, string root, string name)
    {
        var path = $"{root}/BugPrince/Resources/Data/{name}.json";
        JsonUtil.RewriteJsonFile(obj, path);
    }

    private static void CopyDlls(string root) => CopyDll(root, "UnityScriptShims/bin/Debug/net472/BugPrince.dll", "BugPrince/Unity/Assets/Assemblies/BugPrince.dll");

    private static void CopyDll(string root, string src, string dst)
    {
        var inputDll = Path.Combine(root, src);
        var outputDll = Path.Combine(root, dst);
        if (File.Exists(outputDll)) File.Delete(outputDll);
        File.Copy(inputDll, outputDll);
    }
}