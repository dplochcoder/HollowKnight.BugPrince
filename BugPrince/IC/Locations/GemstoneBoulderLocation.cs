using ItemChanger;
using ItemChanger.Locations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.IC.Locations;

internal class GemstoneBoulderLocation : ExistingContainerLocation
{
    public int NailUpgradesThreshold;
    public EmbeddedSprite? BoulderSprite;

    protected override void OnLoad() { }

    protected override void OnUnload() { }

    public override AbstractPlacement Wrap() => new GemstoneBoulderPlacement(this);

    public override ContainerLocation AsContainerLocation() => throw new InvalidOperationException("GemstoneBoulder cannot be replaced");
}

internal class GemstoneBoulderPlacement : AbstractPlacement
{
    private static SortedDictionary<int, GemstoneBoulderPlacement> placements = [];
    internal static IEnumerable<GemstoneBoulderPlacement> ActivePlacements() => placements.Values;

    public GemstoneBoulderLocation Location;

    [JsonConstructor]
    private GemstoneBoulderPlacement() : base("") { }

    public GemstoneBoulderPlacement(GemstoneBoulderLocation loc) : base(loc.name) => Location = loc;

    protected override void OnLoad()
    {
        Location.Load();
        placements.Add(Location.NailUpgradesThreshold, this);
    }

    protected override void OnUnload()
    {
        placements.Remove(Location.NailUpgradesThreshold);
        Location.Unload();
    }

    public override IEnumerable<Tag> GetPlacementAndLocationTags() => base.GetPlacementAndLocationTags().Concat(Location.tags ?? []);
}
