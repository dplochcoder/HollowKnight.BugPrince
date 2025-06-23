using Modding;

namespace BugPrince.RMMInterop;

internal static class RMMInterop
{
    internal static void MaybeUpdateRandoMapMod()
    {
        if (ModHooks.GetMod("RandoMapCoreMod") is not null) UpdateRandoMapCoreModImpl();
        if (ModHooks.GetMod("RandoMapMod") is not null) UpdateRandoMapModImpl();
    }

    private static void UpdateRandoMapCoreModImpl() => RandoMapCore.RandoMapCoreMod.RebuildModules();

    private static void UpdateRandoMapModImpl() => RandoMapMod.RandoMapMod.RebuildModules();
}
