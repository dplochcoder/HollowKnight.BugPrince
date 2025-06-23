using Modding;

namespace BugPrince.RMMInterop;

internal static class RMMInterop
{
    internal static void MaybeUpdateRandoMapMod()
    {
        if (ModHooks.GetMod("RandoMapCoreMod") is Mod) UpdateRandoMapCoreModImpl();
    }

    private static void UpdateRandoMapCoreModImpl() => RandoMapCore.RandoMapCoreMod.Rebuild();
}
