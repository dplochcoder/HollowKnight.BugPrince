using System;
using System.Collections.Generic;

namespace BugPrince.Util;

internal static class CollectionUtil
{
    internal static void AddRange<T>(this HashSet<T> self, IEnumerable<T> range)
    {
        foreach (var item in range) self.Add(item);
    }

    internal static ArgumentException InvalidEnum<E>(this E self) where E : Enum => new($"Unsupported enum: {self}");
}
