using System;
using System.Collections.Generic;
using ConnectionSettingsRando;

namespace BugPrince.CSRInterop;

internal static class CSRInterop
{
    internal static void Setup() =>
        CSR.Register(
            "BugPrince",
            rng =>
            {
                var (settings, stats) = Randomize(rng);
                Rando.ConnectionMenu.Instance?.ApplySettings(settings);
                return stats;
            }
        );

    private static (RandomizationSettings, RandomizationStats) Randomize(Random rng)
    {
        SettingsRandomizer randomizer = new();
        var (settings, stats) = randomizer.Randomize(
            BugPrinceMod.GS.RandoSettings,
            rng,
            "BugPrince"
        );

        // Randomize ranges manually to avoid multiplicative rng failure.
        IReadOnlyList<string> path = ["BugPrince"];
        if (
            SettingsRandomizer.Skip(
                settings.StartingDiceTotems.GetType(),
                nameof(settings.StartingDiceTotems),
                path
            )
            || SettingsRandomizer.Skip(
                settings.TotalDiceTotems.GetType(),
                nameof(settings.TotalDiceTotems),
                path
            )
        )
        {
            SettingsRandomizer.TrackSkip(nameof(settings.StartingDiceTotems), path, stats);
            SettingsRandomizer.TrackSkip(nameof(settings.TotalDiceTotems), path, stats);
        }
        else
        {
            SettingsRandomizer.TrackRando(nameof(settings.StartingDiceTotems), path, stats);
            SettingsRandomizer.TrackRando(nameof(settings.TotalDiceTotems), path, stats);

            settings.TotalDiceTotems = rng.Next(RandomizationSettings.MAX_TOTAL_DICE_TOTEMS + 1);
            settings.StartingDiceTotems = rng.Next(
                Math.Min(RandomizationSettings.MAX_STARTING_DICE_TOTEMS, settings.TotalDiceTotems)
                    + 1
            );
        }

        if (
            SettingsRandomizer.Skip(
                settings.StartingPushPins.GetType(),
                nameof(settings.StartingPushPins),
                path
            )
            || SettingsRandomizer.Skip(
                settings.TotalPushPins.GetType(),
                nameof(settings.TotalPushPins),
                path
            )
        )
        {
            SettingsRandomizer.TrackSkip(nameof(settings.StartingPushPins), path, stats);
            SettingsRandomizer.TrackSkip(nameof(settings.TotalPushPins), path, stats);
        }
        else
        {
            SettingsRandomizer.TrackRando(nameof(settings.StartingPushPins), path, stats);
            SettingsRandomizer.TrackRando(nameof(settings.TotalPushPins), path, stats);

            settings.TotalPushPins = rng.Next(RandomizationSettings.MAX_TOTAL_PUSH_PINS + 1);
            settings.StartingPushPins = rng.Next(
                Math.Min(RandomizationSettings.MAX_STARTING_PUSH_PINS, settings.TotalPushPins) + 1
            );
        }

        if (
            SettingsRandomizer.Skip(
                settings.MinimumMaps.GetType(),
                nameof(settings.MinimumMaps),
                path
            )
            || SettingsRandomizer.Skip(
                settings.MaximumMaps.GetType(),
                nameof(settings.MaximumMaps),
                path
            )
            || SettingsRandomizer.Skip(
                settings.MapTolerance.GetType(),
                nameof(settings.MapTolerance),
                path
            )
        )
        {
            SettingsRandomizer.TrackSkip(nameof(settings.MinimumMaps), path, stats);
            SettingsRandomizer.TrackSkip(nameof(settings.MaximumMaps), path, stats);
            SettingsRandomizer.TrackSkip(nameof(settings.MapTolerance), path, stats);
        }
        else
        {
            SettingsRandomizer.TrackRando(nameof(settings.MinimumMaps), path, stats);
            SettingsRandomizer.TrackRando(nameof(settings.MaximumMaps), path, stats);
            SettingsRandomizer.TrackRando(nameof(settings.MapTolerance), path, stats);

            settings.MapTolerance = rng.Next(RandomizationSettings.MAX_MAP_TOLERANCE + 1);
            int max = 13 - settings.MapTolerance;
            int a = rng.Next(max + 1);
            int b = rng.Next(max + 1);
            settings.MinimumMaps = Math.Min(a, b);
            settings.MaximumMaps = Math.Max(a, b);
        }

        return (settings, stats);
    }
}
