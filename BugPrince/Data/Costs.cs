using BreakableWallRandomizer.Settings;
using Modding;
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

internal enum CostConstraint
{
    None,
    MapAreaRando,
    TitledAreaRando,
    MapOrTitledAreaRando,
    BossEssence,
    DiveFloors,
    EggShop,
    GhostEssence,
    JunkPitChests,
    JunkShop,
    LemmShop,
    Levers,
    NailUpgrades,
    RockWalls,
    TRJR,
}

internal static class CostConstraintExtensions
{
    private static bool TryGetSettings<T>(Mod mod, out T settings)
    {
        if (mod is IGlobalSettings<T> m)
        {
            settings = m.OnSaveGlobal();
            return true;
        }
        settings = default;
        return false;
    }

    private static bool DiveFloorsImpl(Mod mod) => TryGetSettings<BWR_Settings>(mod, out var settings) && settings.Enabled && settings.DiveFloors;
    private static bool DiveFloorsApplies() => ModHooks.GetMod("Breakable Wall Randomizer") is Mod mod && DiveFloorsImpl(mod);

    private static bool GhostEssenceImpl(Mod mod) => TryGetSettings<RandoPlus.GlobalSettings>(mod, out var settings) && settings.GhostEssence;
    private static bool GhostEssenceApplies() => ModHooks.GetMod("RandoPlus") is Mod mod && GhostEssenceImpl(mod);

    private static bool JunkShopImpl(Mod mod) => TryGetSettings<MoreLocations.GlobalSettings>(mod, out var settings) && settings.RS.Enabled && settings.RS.JunkShopSettings.Enabled;
    private static bool JunkShopApplies() => ModHooks.GetMod("MoreLocations") is Mod mod && JunkShopImpl(mod);

    private static bool LemmShopImpl(Mod mod) => TryGetSettings<MoreLocations.GlobalSettings>(mod, out var settings) && settings.RS.Enabled && settings.RS.LemmShopSettings.Enabled;
    private static bool LemmShopApplies() => ModHooks.GetMod("MoreLocations") is Mod mod && LemmShopImpl(mod);

    private static bool LeversImpl(Mod mod) => TryGetSettings<RandomizableLevers.GlobalSettings>(mod, out var settings) && settings.RandoSettings.RandomizeLevers;
    private static bool LeversApplies() => ModHooks.GetMod("RandomizableLevers") is Mod mod && LeversImpl(mod);

    private static bool NailUpgradesImpl(Mod mod) => TryGetSettings<RandoPlus.GlobalSettings>(mod, out var settings) && settings.NailUpgrades;
    private static bool NailUpgradesApplies() => ModHooks.GetMod("RandoPlus") is Mod mod && NailUpgradesImpl(mod);

    private static bool RockWallsImpl(Mod mod) => TryGetSettings<BWR_Settings>(mod, out var settings) && settings.Enabled && settings.RockWalls;
    private static bool RockWallsApplies() => ModHooks.GetMod("Breakable Wall Randomizer") is Mod mod && RockWallsImpl(mod);

    private static bool TRJRImpl(Mod mod) => TryGetSettings<TheRealJournalRando.GlobalSettings>(mod, out var settings) && settings.RandoSettings.Enabled;
    private static bool TRJRApplies() => ModHooks.GetMod("TheRealJournalRando") is Mod mod && TRJRImpl(mod);

    internal static bool Applies(this CostConstraint self, GenerationSettings gs)
    {
        return self switch
        {
            CostConstraint.None => true,
            CostConstraint.MapAreaRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.MapAreaRandomizer,
            CostConstraint.TitledAreaRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.FullAreaRandomizer,
            CostConstraint.MapOrTitledAreaRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.MapAreaRandomizer || gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.FullAreaRandomizer,
            CostConstraint.BossEssence => gs.PoolSettings.BossEssence,
            CostConstraint.DiveFloors => DiveFloorsApplies(),
            CostConstraint.EggShop => gs.PoolSettings.RancidEggs,
            CostConstraint.GhostEssence => GhostEssenceApplies(),
            CostConstraint.JunkPitChests => gs.PoolSettings.JunkPitChests,
            CostConstraint.JunkShop => JunkShopApplies(),
            CostConstraint.LemmShop => LemmShopApplies(),
            CostConstraint.Levers => LeversApplies(),
            CostConstraint.NailUpgrades => NailUpgradesApplies(),
            CostConstraint.RockWalls => RockWallsApplies(),
            CostConstraint.TRJR => TRJRApplies(),
            _ => throw new ArgumentException($"Unknown cost type: {self}")
        };
    }
}

public record CostGroup
{
    public const float DEFAULT_PRIORITY = 0f;

    public readonly string Name;
    public HashSet<string> SceneNames = [];
    public CostType Type;
    public int Cost;
    public float Priority = DEFAULT_PRIORITY;

    private static SortedDictionary<string, ICostGroupProducer>? data;
    public static IReadOnlyDictionary<string, ICostGroupProducer> LoadProducers() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, ICostGroupProducer>>("BugPrince.Resources.Data.cost_groups.json"));

    public static void AddProducer(string name, ICostGroupProducer producer)
    {
        if (LoadProducers().ContainsKey(name)) throw new ArgumentException($"Duplicate ICostGroupProducer: {name}");
        data!.Add(name, producer);
    }

    public CostGroup(string name) => Name = name;
}

public interface ICostGroupProducer
{
    public bool ProduceCostGroup(GenerationSettings gs, HashSet<string> randomizedScenes, out CostGroup costGroup);
}

internal class CostGroupProducer : ICostGroupProducer
{
    public string Name;
    public SortedSet<string> SceneNames = [];
    public CostType Type;
    public int Cost;

    public virtual bool ProduceCostGroup(GenerationSettings gs, HashSet<string> randomizedScenes, out CostGroup costGroup)
    {
        costGroup = default;
        if (!SceneNames.Any(randomizedScenes.Contains)) return false;
        
        costGroup = new(Name)
        {
            SceneNames = [.. SceneNames],
            Type = Type,
            Cost = Cost
        };
        return true;
    }
}

internal class ConstrainedCostGroupProducer : CostGroupProducer
{
    public CostConstraint Constraint;

    public override bool ProduceCostGroup(GenerationSettings gs, HashSet<string> randomizedScenes, out CostGroup costGroup)
    {
        costGroup = default;
        if (!Constraint.Applies(gs)) return false;

        return base.ProduceCostGroup(gs, randomizedScenes, out costGroup);
    }
}

internal record CostTier
{
    public CostConstraint Constraint;
    public int Cost;
}

internal class TieredCostGroupProducer : ICostGroupProducer
{
    public string Name;
    public SortedSet<string> SceneNames = [];
    public CostType Type;
    public List<CostTier> Tiers = [];

    public bool ProduceCostGroup(GenerationSettings gs, HashSet<string> randomizedScenes, out CostGroup costGroup)
    {
        costGroup = default;
        if (!SceneNames.Any(randomizedScenes.Contains)) return false;

        foreach (var tier in Tiers)
        {
            if (!tier.Constraint.Applies(gs)) continue;

            costGroup = new(Name)
            {
                SceneNames = [.. SceneNames],
                Type = Type,
                Cost = tier.Cost,
            };
            return true;
        }
        return false;
    }
}
