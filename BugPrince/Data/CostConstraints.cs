using BreakableWallRandomizer.Settings;
using BugPrince.Util;
using Modding;
using Newtonsoft.Json;
using RandomizerMod.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Data;

internal enum CostConstraintAtom
{
    None,
    MapAreaRando,
    FullAreaRando,
    RoomRando,
    BasicLocations,
    AdvancedLocations,
    MapShop,
    GemstoneCavern,
    TheVault,
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
    TRJR
}

internal record CostConstraintCondition
{
    public CostConstraintAtom atom;
    public bool negated;

    private static bool DiveFloorsImpl(Mod mod) => mod.TryGetSettings<BWR_Settings>(out var settings) && settings.Enabled && settings.DiveFloors;
    private static bool DiveFloorsApplies() => ModHooks.GetMod("Breakable Wall Randomizer") is Mod mod && DiveFloorsImpl(mod);

    private static bool GhostEssenceImpl(Mod mod) => mod.TryGetSettings<RandoPlus.GlobalSettings>(out var settings) && settings.GhostEssence;
    private static bool GhostEssenceApplies() => ModHooks.GetMod("RandoPlus") is Mod mod && GhostEssenceImpl(mod);

    private static bool JunkShopImpl(Mod mod) => mod.TryGetSettings<MoreLocations.GlobalSettings>(out var settings) && settings.RS.Enabled && settings.RS.JunkShopSettings.Enabled;
    private static bool JunkShopApplies() => ModHooks.GetMod("MoreLocations") is Mod mod && JunkShopImpl(mod);

    private static bool LemmShopImpl(Mod mod) => mod.TryGetSettings<MoreLocations.GlobalSettings>(out var settings) && settings.RS.Enabled && settings.RS.LemmShopSettings.Enabled;
    private static bool LemmShopApplies() => ModHooks.GetMod("MoreLocations") is Mod mod && LemmShopImpl(mod);

    private static bool LeversImpl(Mod mod) => mod.TryGetSettings<RandomizableLevers.GlobalSettings>(out var settings) && settings.RandoSettings.RandomizeLevers;
    private static bool LeversApplies() => ModHooks.GetMod("RandomizableLevers") is Mod mod && LeversImpl(mod);

    private static bool NailUpgradesImpl(Mod mod) => mod.TryGetSettings<RandoPlus.GlobalSettings>(out var settings) && settings.NailUpgrades;
    private static bool NailUpgradesApplies() => ModHooks.GetMod("RandoPlus") is Mod mod && NailUpgradesImpl(mod);

    private static bool RockWallsImpl(Mod mod) => mod.TryGetSettings<BWR_Settings>(out var settings) && settings.Enabled && settings.RockWalls;
    private static bool RockWallsApplies() => ModHooks.GetMod("Breakable Wall Randomizer") is Mod mod && RockWallsImpl(mod);

    private static bool TRJRImpl(Mod mod) => mod.TryGetSettings<TheRealJournalRando.GlobalSettings>(out var settings) && settings.RandoSettings.Enabled;
    private static bool TRJRApplies() => ModHooks.GetMod("TheRealJournalRando") is Mod mod && TRJRImpl(mod);

    internal static bool Applies(CostConstraintAtom atom, GenerationSettings gs)
    {
        return atom switch
        {
            CostConstraintAtom.None => true,
            CostConstraintAtom.MapAreaRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.MapAreaRandomizer,
            CostConstraintAtom.FullAreaRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.FullAreaRandomizer,
            CostConstraintAtom.RoomRando => gs.TransitionSettings.Mode == TransitionSettings.TransitionMode.RoomRandomizer,
            CostConstraintAtom.BasicLocations => BugPrinceMod.RS.BasicLocations,
            CostConstraintAtom.AdvancedLocations => BugPrinceMod.RS.AdvancedLocations,
            CostConstraintAtom.MapShop => BugPrinceMod.RS.MapShop,
            CostConstraintAtom.GemstoneCavern => BugPrinceMod.RS.GemstoneCavern,
            CostConstraintAtom.TheVault => BugPrinceMod.RS.TheVault,
            CostConstraintAtom.BossEssence => gs.PoolSettings.BossEssence,
            CostConstraintAtom.DiveFloors => DiveFloorsApplies(),
            CostConstraintAtom.EggShop => gs.PoolSettings.RancidEggs,
            CostConstraintAtom.GhostEssence => GhostEssenceApplies(),
            CostConstraintAtom.JunkPitChests => gs.PoolSettings.JunkPitChests,
            CostConstraintAtom.JunkShop => JunkShopApplies(),
            CostConstraintAtom.LemmShop => LemmShopApplies(),
            CostConstraintAtom.Levers => LeversApplies(),
            CostConstraintAtom.NailUpgrades => NailUpgradesApplies(),
            CostConstraintAtom.RockWalls => RockWallsApplies(),
            CostConstraintAtom.TRJR => TRJRApplies(),
            _ => throw new ArgumentException($"Unknown cost type: {atom}")
        };
    }

    internal bool Applies(GenerationSettings gs) => Applies(atom, gs) ^ negated;
}

[JsonConverter(typeof(CostConstraintConverter))]
internal class CostConstraints
{
    private List<List<CostConstraintCondition>> dnf = [];

    private CostConstraints() { }

    internal bool Applies(GenerationSettings gs) => dnf.Any(c => c.All(t => t.Applies(gs)));

    internal static bool ParseFromString(string str, out CostConstraints costConstraints)
    {
        costConstraints = new();
        List<List<CostConstraintCondition>> dnf = [];
        foreach (var clauseStr in str.Split('|'))
        {
            if (!ParseClauseFromString(clauseStr.Trim(), out var clause)) return false;
            dnf.Add(clause);
        }

        if (dnf.Count == 0) return false;

        costConstraints.dnf = dnf;
        return true;
    }

    private static bool ParseClauseFromString(string str, out List<CostConstraintCondition> clause)
    {
        clause = [];
        foreach (var token in str.Split('+'))
        {
            if (!ParseTokenFromString(token.Trim(), out var cond)) return false;
            clause.Add(cond);
        }

        return clause.Count > 0;
    }

    private static bool ParseTokenFromString(string str, out CostConstraintCondition cond)
    {
        cond = new();
        if (str.StartsWith("!"))
        {
            cond.negated = true;
            str = str.Substring(1);
        }
        str = str.ToUpper();

        foreach (var atom in Enum.GetValues(typeof(CostConstraintAtom)).Cast<CostConstraintAtom>())
        {
            if (atom.ToString().ToUpper() == str)
            {
                cond.atom = atom;
                return true;
            }
        }
        return false;
    }

    public override string ToString()
    {
        List<string> clauses = [];
        foreach (var clause in dnf)
        {
            List<string> atoms = [];
            foreach (var cond in clause) atoms.Add(cond.negated ? $"!{cond.atom}" : cond.atom.ToString());
            clauses.Add(string.Join(" + ", atoms));
        }
        return string.Join(" | ", clauses);
    }
}

class CostConstraintConverter : JsonConverter<CostConstraints>
{
    public override CostConstraints? ReadJson(JsonReader reader, Type objectType, CostConstraints? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.String) throw new NotSupportedException($"CostConstraints must be serialized as string");
        var str = (string)reader.Value!;
        if (!CostConstraints.ParseFromString(str, out var costConstraints)) throw new ArgumentException($"Bad CostConstraints: {str}");
        return costConstraints;
    }

    public override void WriteJson(JsonWriter writer, CostConstraints? value, JsonSerializer serializer) => writer.WriteValue(value!.ToString());
}
