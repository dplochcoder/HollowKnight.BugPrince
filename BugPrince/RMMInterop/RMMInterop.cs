using Modding;
using RandoMapCore.Data;
using RandomizerMod.RandomizerData;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BugPrince.RMMInterop;

internal static class RMMInterop
{
    internal static void MaybeUpdateRandoMapMod()
    {
        if (ModHooks.GetMod("RandoMapMod") is null) return;
        UpdateRandoMapModImpl();
    }

    private static void UpdateRandoMapModImpl() => RMMInteropImpl.UpdateRmmDataModule();
}

// Rando map mod assumes that randomized transition placements only change on entering the game.
internal static class RMMInteropImpl
{
    private static readonly Type moduleType = Type.GetType("RandoMapMod.RmmDataModule, RandoMapMod");
    private static readonly FieldInfo randomizedTransitionsField = moduleType.GetField("_randomizedTransitions", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly FieldInfo randomizedTransitionPlacementsField = moduleType.GetField("_randomizedTransitionPlacements", BindingFlags.NonPublic | BindingFlags.Static);

    private static RmcTransitionDef ConvertTransitionDef(TransitionDef def) => new()
    {
        SceneName = def.SceneName,
        DoorName = def.DoorName,
        VanillaTarget = def.VanillaTarget,
    };

    internal static void UpdateRmmDataModule()
    {
        if (randomizedTransitionsField.GetValue(null) is not Dictionary<string, RmcTransitionDef> randomizedTransitions) return;
        if (randomizedTransitionPlacementsField.GetValue(null) is not Dictionary<RmcTransitionDef, RmcTransitionDef> randomizedTransitionPlacements) return;

        randomizedTransitions.Clear();
        randomizedTransitionPlacements.Clear();
        foreach (var placement in RandomizerMod.RandomizerMod.RS.Context.transitionPlacements)
        {
            var sourceDef = ConvertTransitionDef(placement.Source.TransitionDef);
            var targetDef = ConvertTransitionDef(placement.Target.TransitionDef);

            randomizedTransitions[placement.Source.Name] = sourceDef;
            randomizedTransitions[placement.Target.Name] = targetDef;
            randomizedTransitionPlacements[sourceDef] = targetDef;
        }
    }
}
