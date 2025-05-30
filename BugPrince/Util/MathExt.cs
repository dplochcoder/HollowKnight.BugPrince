using System.Collections.Generic;

namespace BugPrince.Util;

internal static class MathExt
{
    internal static float NextFloat(this System.Random self, float min, float max) => min + (float)self.NextDouble() * (max - min);

    internal static float NextFloat(this System.Random self, float max) => self.NextFloat(0, max);

    internal static float NextFloat(this System.Random self) => self.NextFloat(0, 1);

    internal static T Random<T>(this T[] self) => self[UnityEngine.Random.Range(0, self.Length)];

    internal static T Random<T>(this List<T> self) => self[UnityEngine.Random.Range(0, self.Count)];
}
