using BugPrince.Data;
using ItemChanger;
using ItemChanger.Tags;

namespace BugPrince.IC;

internal static class Interop
{
    internal static void AddInteropPoolGroup(this AbstractItem self, string pool)
    {
        var interop = self.GetOrAddTag<InteropTag>();
        interop.Message = "RandoSupplementalMetadata";
        interop.Properties["ModSource"] = nameof(BugPrinceMod);
        interop.Properties["PoolGroup"] = pool;
    }

    internal static void AddInteropPinData(this AbstractLocation self, string pool, PinLocation? pinLocation = null)
    {
        var interop = self.GetOrAddTag<InteropTag>();
        interop.Message = "RandoSupplementalMetadata";
        interop.Properties["ModSource"] = nameof(BugPrinceMod);
        interop.Properties["PinSpriteKey"] = pool;

        if (pinLocation != null)
        {
            if (pinLocation.IsWorld) interop.Properties["WorldMapLocation"] = pinLocation.AsTuple();
            else interop.Properties["MapLocations"] = pinLocation.AsTupleArray();
        }
    }
}
