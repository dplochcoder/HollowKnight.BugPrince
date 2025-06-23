using BugPrince.Data;
using ItemChanger;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Rando;

// Interface for powering bug prince progression logic.
// Instances must never observably changed once accessed under this interface.
internal interface ICostGroupProgressionProvider
{
    int Generation();
    IReadOnlyDictionary<string, CostGroup> GetCostGroups();
    IReadOnlyDictionary<string, string> GetCostGroupsByScene();
    bool IsRandomizedTransition(Transition transition);
    IReadOnlyList<string> GetCostGroupProgression();
}

internal static class CostGroupProgressionProviderGeneration
{
    private static int gen = 0;

    internal static int NextGen() => gen++;
}

internal class OverlaidCostGroupProgressionProvider : ICostGroupProgressionProvider
{
    private readonly int gen = CostGroupProgressionProviderGeneration.NextGen();
    private readonly ICostGroupProgressionProvider impl;
    private readonly List<string> progressionOverride;

    internal OverlaidCostGroupProgressionProvider(ICostGroupProgressionProvider impl, List<string> progressionOverride)
    {
        this.impl = impl;
        this.progressionOverride = [.. progressionOverride];
    }

    public int Generation() => gen;

    public IReadOnlyDictionary<string, CostGroup> GetCostGroups() => impl.GetCostGroups();

    public IReadOnlyDictionary<string, string> GetCostGroupsByScene() => impl.GetCostGroupsByScene();

    public bool IsRandomizedTransition(Transition transition) => impl.IsRandomizedTransition(transition);

    public IReadOnlyList<string> GetCostGroupProgression() => progressionOverride;
}

internal static class ICostGroupProgressionProviderExtensions
{
    internal static int GetItemCount(this ICostGroupProgressionProvider self, CostType costType) => self.GetCostGroups().Values.Where(g => g.Type == costType).Select(g => g.Cost).Sum();

    internal static bool GetCostGroupByScene(this ICostGroupProgressionProvider self, string scene, out string groupName, out CostGroup costGroup)
    {
        if (!self.GetCostGroupsByScene().TryGetValue(scene, out groupName))
        {
            costGroup = default;
            return false;
        }
        return self.GetCostGroups().TryGetValue(groupName, out costGroup);
    }

    internal static bool GetProgressiveCostByScene(this ICostGroupProgressionProvider self, string sceneName, out CostType costType, out int cost)
    {
        costType = default;
        cost = 0;
        if (!self.GetCostGroupByScene(sceneName, out var groupName, out var group)) return false;

        costType = group.Type;

        // Add progressive costs
        foreach (var previousName in self.GetCostGroupProgression())
        {
            if (!self.GetCostGroups().TryGetValue(previousName, out var previousGroup)) throw new ArgumentException("Bad cost group progression data");

            if (previousGroup.Type == group.Type) cost += previousGroup.Cost;
            if (previousName == groupName) return true;
        }

        throw new ArgumentException("Bad cost group progression data");
    }
}
