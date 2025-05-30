using ItemChanger;
using System.Collections.Generic;

namespace BugPrince.Data;

internal enum ItemType
{
    Coin,
    DiceTotem,
    Gem,
    PushPin
}

internal class Locations
{
    private static SortedDictionary<string, Locations>? data;
    public static IReadOnlyDictionary<string, Locations> GetLocations() => (data ??= PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeEmbedded<SortedDictionary<string, Locations>>("BugPrince.Resources.Data.locations.json"));

    public ItemType Type;
    public int Count;
    public AbstractLocation? Location;
    public bool FullFlexible;
    public string Logic = "";
}
