using ItemChanger;
using ItemChanger.Locations;
using ItemChanger.Placements;

namespace BugPrince.IC.Locations;

internal class VaultChestLocation : CoordinateLocation
{
    public override AbstractPlacement Wrap()
    {
        var ret = base.Wrap();
        if (ret is MutablePlacement mutable) mutable.containerType = Container.Chest;
        return ret;
    }
}
