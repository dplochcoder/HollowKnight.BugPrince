using ItemChanger;
using PurenailCore.ModUtil;
using UnityEngine;

namespace BugPrince;

internal class BugPrincePreloader : Preloader
{
    internal static readonly BugPrincePreloader Instance = new();

    [Preload(SceneNames.Ruins1_05, "ruind_dressing_light_03 (4)")]
    public GameObject? CityLamp { get; private set; }

    [Preload(SceneNames.Crossroads_13, "_Enemies/Worm")]
    public GameObject? Goam { get; private set; }

    public AudioClip TinkEffectClip => Goam!.GetComponent<TinkEffect>().blockEffect.GetComponent<AudioSource>().clip;
}
