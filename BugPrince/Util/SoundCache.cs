using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace BugPrince.Util;

internal class SoundCachePreloadAttribute : Attribute { }

internal static class SoundCache
{
    private static readonly Dictionary<string, AudioClip> clips = [];

    private static AudioClip GetAudioClip(string name)
    {
        if (clips.TryGetValue(name, out var clip)) return clip;

        using Stream s = typeof(SoundCache).Assembly.GetManifestResourceStream($"BugPrince.Resources.Sounds.{name}.wav");
        clip = SFCore.Utils.WavUtils.ToAudioClip(s);
        clips[name] = clip;
        return clip;
    }

    static SoundCache()
    {
        foreach (var property in typeof(SoundCache).GetProperties(BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (property.GetCustomAttribute<SoundCachePreloadAttribute>() != null) property.GetValue(null);
        }
    }

    [SoundCachePreload]
    internal static AudioClip change_selection => GetAudioClip("change_selection");

    [SoundCachePreload]
    internal static AudioClip confirm => GetAudioClip("confirm");

    [SoundCachePreload]
    internal static AudioClip damage => GetAudioClip("damage");

    [SoundCachePreload]
    internal static AudioClip locked_out => GetAudioClip("locked_out");

    [SoundCachePreload]
    internal static AudioClip spend_resources => GetAudioClip("spend_resources");

    internal static AudioClip failed_menu => BugPrincePreloader.Instance.TinkEffectClip;
}
