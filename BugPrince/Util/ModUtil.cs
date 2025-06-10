using Modding;

namespace BugPrince.Util;

internal static class ModUtil
{
    internal static bool TryGetSettings<T>(this Mod self, out T settings)
    {
        if (self is IGlobalSettings<T> m)
        {
            settings = m.OnSaveGlobal();
            return true;
        }
        settings = default;
        return false;
    }
}
