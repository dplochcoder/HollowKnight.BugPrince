using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Data;

public enum CostType
{
    Coins,
    Gems
}

public record CostGroup
{
    public const float DEFAULT_PRIORITY = 0f;

    public HashSet<string> SceneNames = [];
    public CostType Type;
    public int Cost;
    // Set higher or lower to change average positioning within cost group progression. Must be in (0, 1).
    public float SkewedAverage = 0.5f;
    public float Priority = DEFAULT_PRIORITY;  // For connections.

    private static SortedDictionary<string, ICostGroupProducer>? data;
    public static IReadOnlyDictionary<string, ICostGroupProducer> GetProducers() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, ICostGroupProducer>>("BugPrince.Resources.Data.cost_groups.json"));

    public static void AddProducer(string name, ICostGroupProducer producer)
    {
        if (GetProducers().ContainsKey(name)) throw new ArgumentException($"Duplicate ICostGroupProducer: {name}");
        data!.Add(name, producer);
    }
}

public interface ICostGroupProducer
{
    public IReadOnlyCollection<string> RelevantSceneNames();

    public bool ProduceCostGroup(GenerationSettings gs, Func<string, bool> sceneFilter, out CostGroup costGroup);
}

internal class CostGroupProducer : ICostGroupProducer
{
    public SortedSet<string> SceneNames = [];
    public CostType Type;
    public int Cost;
    public float SkewedAverage = 0.5f;

    public virtual bool ProduceCostGroup(GenerationSettings gs, Func<string, bool> sceneFilter, out CostGroup costGroup)
    {
        costGroup = default;
        if (!SceneNames.Any(sceneFilter)) return false;

        costGroup = new()
        {
            SceneNames = [.. SceneNames],
            Type = Type,
            Cost = Cost,
            SkewedAverage = SkewedAverage
        };
        return true;
    }

    IReadOnlyCollection<string> ICostGroupProducer.RelevantSceneNames() => SceneNames;
}

internal class ConstrainedCostGroupProducer : CostGroupProducer
{
    public CostConstraints? Constraint;

    public override bool ProduceCostGroup(GenerationSettings gs, Func<string, bool> sceneFilter, out CostGroup costGroup)
    {
        costGroup = default;
        return (Constraint?.Applies(gs) ?? true) && base.ProduceCostGroup(gs, sceneFilter, out costGroup);
    }
}

internal record CostTier
{
    public CostConstraints? Constraint;
    public int Cost;
}

internal class TieredCostGroupProducer : ICostGroupProducer
{
    public SortedSet<string> SceneNames = [];
    public CostType Type;
    public List<CostTier> Tiers = [];
    public float SkewedAverage = 0.5f;

    public bool ProduceCostGroup(GenerationSettings gs, Func<string, bool> sceneFilter, out CostGroup costGroup)
    {
        costGroup = default;
        if (!SceneNames.Any(sceneFilter)) return false;

        foreach (var tier in Tiers)
        {
            if (!(tier.Constraint?.Applies(gs) ?? false)) continue;

            costGroup = new()
            {
                SceneNames = [.. SceneNames],
                Type = Type,
                Cost = tier.Cost,
                SkewedAverage = SkewedAverage
            };
            return true;
        }
        return false;
    }

    IReadOnlyCollection<string> ICostGroupProducer.RelevantSceneNames() => SceneNames;
}
