using ItemChanger;
using ItemChanger.Placements;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.IC.Locations;

internal class GemstoneBoulderLocation : AbstractLocation
{
    public int NailUpgradesThreshold;
    public EmbeddedSprite? BoulderSprite;

    protected override void OnLoad() { }

    protected override void OnUnload() { }

    public override AbstractPlacement Wrap()
    {
        GemstoneBoulderPlacement p = new(name);
        p.Location = this;
        return p;
    }
}

internal class GemstoneBoulderPlacement : AbstractPlacement, IPrimaryLocationPlacement
{
    private static SortedDictionary<int, GemstoneBoulderPlacement> activePlacements = [];
    internal static IEnumerable<GemstoneBoulderPlacement> ActivePlacements() => activePlacements.Values;

    public GemstoneBoulderLocation? Location;

    [JsonIgnore]
    AbstractLocation IPrimaryLocationPlacement.Location => Location!;

    public GemstoneBoulderPlacement(string name) : base(name) { }

    protected override void OnLoad()
    {
        Location!.Placement = this;
        Location.Load();
        activePlacements.Add(Location.NailUpgradesThreshold, this);
    }

    protected override void OnUnload()
    {
        activePlacements.Remove(Location!.NailUpgradesThreshold);
        Location.Unload();
    }

    public override IEnumerable<Tag> GetPlacementAndLocationTags() => base.GetPlacementAndLocationTags().Concat(Location?.tags ?? []);
}
