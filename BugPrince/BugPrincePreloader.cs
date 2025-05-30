using BugPrince.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using PurenailCore.ModUtil;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugPrince;

internal class BugPrincePreloader : Preloader
{
    internal static readonly BugPrincePreloader Instance = new();

    [Preload(SceneNames.Ruins1_05, "ruind_dressing_light_03 (4)")]
    public GameObject? CityLamp { get; private set; }

    [Preload(SceneNames.Crossroads_13, "_Enemies/Worm")]
    public GameObject? Goam { get; private set; }

    [Preload(SceneNames.Ruins2_01_b, "Quake Floor Glass")]
    public GameObject? QuakeFloorGlass { get; private set; }

    public List<GameObject> QuakeFloorGlassDebris => QuakeFloorGlass!.FindChild("Debris")!.Children().Where(o => o.name.StartsWith("Bottle Glass")).ToList();

    public AudioClip QuakeFloorGlassShatterAudio => QuakeFloorGlass!.LocateMyFSM("quake_floor").GetState("Glass").GetFirstActionOfType<AudioPlayerOneShot>().audioClips[0];

    public AudioClip SecretSoundClip => (SecretSoundRegion!.LocateMyFSM("unmasker").GetState("Sound").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value as AudioClip)!;

    [Preload(SceneNames.Deepnest_44, "Secret Sound Region")]
    public GameObject? SecretSoundRegion { get; private set; }

    [Preload(SceneNames.Crossroads_ShamanTemple, "_Props/Shaman Meeting")]
    public GameObject? ShamanMeeting { get; private set; }

    public AudioClip TinkEffectClip => Goam!.GetComponent<TinkEffect>().blockEffect.GetComponent<AudioSource>().clip;

    [Preload(SceneNames.Crossroads_ShamanTemple, "_Props/Torch Breakable (4)")]
    public GameObject? TorchProp { get; private set; }
}
