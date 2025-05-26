using ItemChanger;
using PurenailCore.ModUtil;
using UnityEngine;

namespace BugPrince;

internal class BugPrincePreloader : Preloader
{
    internal static readonly BugPrincePreloader Instance = new();

    [Preload(SceneNames.Crossroads_13, "_Enemies/Worm")]
    public GameObject? Goam { get; private set; }

    public AudioClip TinkEffectClip => Goam!.GetComponent<TinkEffect>().blockEffect.GetComponent<AudioSource>().clip;
}
