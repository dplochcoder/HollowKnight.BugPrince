using System.Collections.Generic;

namespace BugPrince.Util;

internal static class MathExt
{
    internal static T Random<T>(this T[] self) => self[UnityEngine.Random.Range(0, self.Length)];

    internal static T Random<T>(this List<T> self) => self[UnityEngine.Random.Range(0, self.Count)];
}
