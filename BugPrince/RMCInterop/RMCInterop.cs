using Modding;

namespace BugPrince.RMCInterop;

internal static class RMCInterop
{
    internal static void MaybeUpdateRandoMapMod()
    {
        if (ModHooks.GetMod("RandoMapCoreMod") is Mod) UpdateRandoMapCoreModImpl();
    }

    private static void UpdateRandoMapCoreModImpl() => RandoMapCore.RandoMapCoreMod.Rebuild();
}
