using BugPrince.Data;
using System.Collections.Generic;

namespace BugPrince.Rando;

// Interface for powering bug prince progression logic.
// Instances must never observably changed once accessed under this interface.
internal interface ICostGroupProgressionProvider
{
    IReadOnlyDictionary<string, CostGroup> CostGroups();
    IReadOnlyDictionary<string, string> CostGroupsByScene();
    IReadOnlyCollection<string> RandomizedTransitions();
    IReadOnlyList<string> CostGroupProgression();
}

internal class OverlaidCostGroupProgressionProvider : ICostGroupProgressionProvider
{
    private readonly ICostGroupProgressionProvider impl;
    private readonly List<string> progressionOverride;

    internal OverlaidCostGroupProgressionProvider(ICostGroupProgressionProvider impl, List<string> progressionOverride)
    {
        this.impl = impl;
        this.progressionOverride = [.. progressionOverride];
    }

    public IReadOnlyDictionary<string, CostGroup> CostGroups() => impl.CostGroups();

    public IReadOnlyDictionary<string, string> CostGroupsByScene() => impl.CostGroupsByScene();

    public IReadOnlyCollection<string> RandomizedTransitions() => impl.RandomizedTransitions();

    public IReadOnlyList<string> CostGroupProgression() => progressionOverride;
}

internal static class ICostGroupProgressionProviderExtensions
{
    internal static bool GetCostGroupByScene(this ICostGroupProgressionProvider self, string scene, out string groupName, out CostGroup costGroup)
    {
        groupName = default;
        costGroup = default;
        if (!self.CostGroupsByScene().TryGetValue(scene, out groupName)) return false;
        return self.CostGroups().TryGetValue(groupName, out costGroup);
    }

    internal static bool GetProgressiveCost(this ICostGroupProgressionProvider self, string groupName, out CostType costType, out int cost)
    {
        costType = default;
        cost = 0;
        if (!self.CostGroups().TryGetValue(groupName, out var group)) return false;

        costType = group.Type;

        // Add progressive costs
        foreach (var previousName in self.CostGroupProgression())
        {
            if (!self.CostGroups().TryGetValue(previousName, out var previousGroup)) throw new System.ArgumentException("Bad cost group progression data");

            if (previousGroup.Type == group.Type) cost += previousGroup.Cost;
            if (previousName == groupName) return true;
        }

        throw new System.ArgumentException("Bad cost group progression data");
    }
}
