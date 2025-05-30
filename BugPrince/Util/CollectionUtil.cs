using System.Collections.Generic;

namespace BugPrince.Util;

internal static class CollectionUtil
{
    internal static void AddRange<T>(this HashSet<T> self, IEnumerable<T> range)
    {
        foreach (var item in range) self.Add(item);
    }
}
