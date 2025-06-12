using BugPrince.Data;
using ItemChanger;
using ItemChanger.Tags;

namespace BugPrince.IC;

internal static class Interop
{
    internal static void AddInteropData(this AbstractItem self, string pool)
    {
        var interop = self.GetOrAddTag<InteropTag>();
        interop.Message = "RandoSupplementalMetadata";
        interop.Properties["ModSource"] = nameof(BugPrinceMod);
        interop.Properties["PoolGroup"] = pool;
    }

    internal static void AddInteropData(this AbstractLocation self, string pool, PinLocation? pinLocation, string? displaySource)
    {
        var rsmTag = self.AddTag<InteropTag>();
        rsmTag.Message = "RandoSupplementalMetadata";
        rsmTag.Properties["ModSource"] = nameof(BugPrinceMod);
        rsmTag.Properties["PinSpriteKey"] = pool;
        if (pinLocation != null)
        {
            if (pinLocation.IsWorld) rsmTag.Properties["WorldMapLocation"] = pinLocation.AsTuple();
            else rsmTag.Properties["MapLocations"] = pinLocation.AsTupleArray();
        }

        if (displaySource != null)
        {
            var riTag = self.AddTag<InteropTag>();
            riTag.Message = "RecentItems";
            riTag.Properties["DisplaySource"] = displaySource;
        }
    }
}
