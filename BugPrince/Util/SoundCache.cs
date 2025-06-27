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
    internal static AudioClip ChangeSelection => GetAudioClip("change_selection");

    [SoundCachePreload]
    internal static AudioClip Confirm => GetAudioClip("confirm");

    [SoundCachePreload]
    internal static AudioClip Damage => GetAudioClip("damage");

    [SoundCachePreload]
    internal static AudioClip LockedOut => GetAudioClip("locked_out");

    [SoundCachePreload]
    internal static AudioClip RollTotem => GetAudioClip("roll_totem");

    [SoundCachePreload]
    internal static AudioClip SpendResources => GetAudioClip("spend_resources");

    internal static AudioClip FailedMenu => BugPrincePreloader.Instance.TinkEffectClip;
}
