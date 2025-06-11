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

    [Preload(SceneNames.Mines_05, "Breakable Wall")]
    public GameObject? CrystalBreakableWall { get; private set; }

    [Preload(SceneNames.Mines_11, "mine_globe_02 (2)")]
    public GameObject? CrystalGlobe { get; private set; }

    [Preload(SceneNames.Deepnest_40, "Dream Gate (2)")]
    public GameObject? DreamWall { get; private set; }

    [Preload(SceneNames.Crossroads_13, "_Enemies/Worm")]
    public GameObject? Goam { get; private set; }

    [Preload(SceneNames.Cliffs_06, "Nightmare Lantern/lantern_dream")]
    public GameObject? NightmareLantern { get; private set; }

    [Preload(SceneNames.White_Palace_18, "_Managers/PlayMaker Unity 2D")]
    public GameObject? PlayMaker { get; private set; }

    [Preload(SceneNames.Ruins2_04, "lake_water_v02/ruin_water_top_v02_002 (12)")]
    public GameObject? RuinsWater { get; private set; }

    [Preload(SceneNames.Ruins2_01_b, "Quake Floor Glass")]
    public GameObject? QuakeFloorGlass { get; private set; }

    public List<GameObject> QuakeFloorGlassDebris => [.. QuakeFloorGlass!.FindChild("Debris")!.Children().Where(o => o.name.StartsWith("Bottle Glass"))];

    public AudioClip QuakeFloorGlassShatterAudio => QuakeFloorGlass!.LocateMyFSM("quake_floor").GetState("Glass").GetFirstActionOfType<AudioPlayerOneShot>().audioClips[0];

    public AudioClip SecretSoundClip => (SecretSoundRegion!.LocateMyFSM("unmasker").GetState("Sound").GetFirstActionOfType<AudioPlayerOneShotSingle>().audioClip.Value as AudioClip)!;

    [Preload(SceneNames.Deepnest_44, "Secret Sound Region")]
    public GameObject? SecretSoundRegion { get; private set; }

    [Preload(SceneNames.Deepnest_East_08, "Hollow_Shade Marker")]
    public GameObject? ShadeMarker { get; private set; }

    [Preload(SceneNames.Crossroads_ShamanTemple, "_Props/Shaman Meeting")]
    public GameObject? ShamanMeeting { get; private set; }

    [Preload(SceneNames.Tutorial_01, "_Scenery/plat_float_07")]
    public GameObject? SmallPlatform { get; private set; }

    [Preload(SceneNames.Ruins2_04, "Surface Water Region")]
    public GameObject? SurfaceWaterRegion { get; private set; }
    
    public PhysicsMaterial2D TerrainMaterial => SmallPlatform!.GetComponent<Collider2D>().sharedMaterial;

    public AudioClip TinkEffectClip => Goam!.GetComponent<TinkEffect>().blockEffect.GetComponent<AudioSource>().clip;

    [Preload(SceneNames.Mines_33, "Toll Gate")]
    public GameObject? TollGate { get; private set; }

    [Preload(SceneNames.Mines_33, "Toll Gate Machine")]
    public GameObject? TollGateMachine { get; private set; }

    [Preload(SceneNames.Crossroads_ShamanTemple, "_Props/Torch Breakable 4")]
    public GameObject? TorchProp { get; private set; }
}
