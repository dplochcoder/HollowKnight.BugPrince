using BugPrince.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using PurenailCore.SystemUtil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Locations;

internal class CrystallizedMoundChallengeLocation : ContainerLocation
{
    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, ModifyScene);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, ModifyScene);

    private void ModifyScene(Scene scene)
    {
        scene.FindGameObject("mines_fog (4)")?.SetActive(false);

        GameObject ctrl = new("LanternControl");
        ctrl.AddComponent<LanternControl>().Location = this;
    }
}

internal class LanternControl : MonoBehaviour
{
    private static readonly Vector3 LANTERN_POSITION = new(6.8f, 11.1f, 5.6f);
    private static readonly List<Vector2> SHINY_POSITIONS = [
        new(104.8f, 10.7f),
        new(93.3f, 4.8f),
        new(79.8f, 3.4f),
        new(86.3f, 17.2f)
    ];

    internal ContainerLocation? Location;

    private void Awake()
    {
        var lanternClone = Instantiate(BugPrincePreloader.Instance.NightmareLantern!);

        lanternClone.transform.position = LANTERN_POSITION;
        foreach (var child in lanternClone.Children()) child.SetActive(false);
        lanternClone.SetActive(true);

        var bigLantern = lanternClone.FindChild("big_lantern")!;
        bigLantern.SetActive(true);
        var brazier = bigLantern.FindChild("grimm_brazier")!;

        var fsm = brazier.LocateMyFSM("grimm_brazier")!;
        fsm.GetState("Init").RemoveTransitionsTo("Activated");
        var hitState = fsm.GetState("Hit");
        hitState.RemoveTransitionsTo("Spark");
        hitState.AddLastAction(new Lambda(OnHit));

        lightSound = (fsm.GetState("Spark").GetActionsOfType<AudioPlayerOneShotSingle>()[1].audioClip.Value as AudioClip)!;
        sharpFlash = lanternClone.FindChild("Sharp Flash")!;
        flamePillar = lanternClone.FindChild("Flame Pillar")!;

        var dreamEffects = lanternClone.FindChild("dream_area_effect")!;
        dreamParticles = dreamEffects.Children().ToList();
        dreamParticles.ForEach(o => o.SetActive(true));
        dreamEffects.SetActive(true);
        Deactivate(dreamParticles, true);

        var firstLightParent = lanternClone.FindChild("Lantern First Light")!;
        var firstLightObj = firstLightParent.FindChild("particle_flame (1)")!;
        foreach (var child in firstLightParent.Children()) child.SetActive(false);
        firstLightObj.SetActive(true);
        firstLightParent.SetActive(true);
        firstLight = firstLightObj.GetComponent<ParticleSystem>();
        firstLight.Stop();

        var lanternLitObjects = lanternClone.FindChild("Lantern Lit Objects")!;
        mainAudioLoop = lanternLitObjects.FindChild("Main Audio Loop")!;
        mainAudioLoop.SetActive(false);
        var grimmFlame = lanternLitObjects.FindChild("Grimm_lantern_flame")!;
        grimmFlameChildren = grimmFlame.Children().ToList();
        grimmFlameChildren.ForEach(o => o.SetActive(true));
        Destroy(lanternLitObjects.LocateMyFSM("Control"));
        lanternLitObjects.SetActive(true);
        Deactivate(grimmFlameChildren, true);

        var finalFlamesParent = brazier.FindChild("Final Flames")!;
        finalFlames = finalFlamesParent.FindChild("particle_flame (1)")!.GetComponent<ParticleSystem>();
        finalFlames.gameObject.SetActive(true);
        finalFlamesParent.SetActive(true);
        Deactivate([finalFlames.gameObject], true);

        dreamWalls.Add(SpawnDreamWall(new(19.5f, 49f), true));
        dreamWalls.Add(SpawnDreamWall(new(43.5f, 49f), false));

        platforms = [.. FindObjectsOfType<FlipPlatform>().Select(p => p.gameObject)];
        enemies = [.. FindObjectsOfType<HealthManager>().Where(hm => hm.name.Contains("Crystal Flyer") || hm.name.Contains("Crawler") || hm.name.Contains("Roller"))];
        shinies = [.. SHINY_POSITIONS.Select(p => {
            var obj = GameObjectUtil.MakeShinyDecorator();
            obj.transform.position = p;
            return obj;
        })];
    }

    private AudioClip? lightSound;
    private GameObject? sharpFlash;
    private GameObject? flamePillar;
    private ParticleSystem? firstLight;
    private ParticleSystem? finalFlames;
    private List<GameObject> grimmFlameChildren = [];
    private List<GameObject> dreamParticles = [];
    private readonly List<PlayMakerFSM> dreamWalls = [];
    private GameObject? mainAudioLoop;
    private List<GameObject> platforms = [];
    private List<HealthManager> enemies = [];
    private List<GameObject> shinies = [];

    private PlayMakerFSM SpawnDreamWall(Vector2 pos, bool rightWall)
    {
        var wall = Instantiate(BugPrincePreloader.Instance.DreamWall!);
        wall.transform.position = pos;
        wall.transform.localRotation = Quaternion.Euler(0, 0, rightWall ? 180 : 0);
        wall.SetActive(true);
        return wall.LocateMyFSM("Control");
    }

    private void Light()
    {
        sharpFlash?.SetActive(true);
        flamePillar?.SetActive(true);
        finalFlames?.Play();
        Activate(dreamParticles);
        if (!complete) dreamWalls.ForEach(w => w.SendEvent("DREAM GATE CLOSE"));
        gameObject.PlaySoundEffect(lightSound!);
        platforms.ForEach(p => p.SetActive(false));
        enemies.Where(hm => hm.hp > 0).ForEach(hm => hm.gameObject.SetActive(false));
        shinies.ForEach(p => p.SetActive(true));

        IEnumerator Animate()
        {
            yield return new WaitForSeconds(1);
            finalFlames?.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            Activate(grimmFlameChildren);
            mainAudioLoop?.SetActive(true);
            firstLight?.Play();
        };
        StartCoroutine(Animate());
    }

    private void Extinguish()
    {
        finalFlames?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        firstLight?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        gameObject.PlaySoundEffect(lightSound!);
        Deactivate(grimmFlameChildren);
        Deactivate(dreamParticles);
        dreamWalls.ForEach(w => w.SendEvent("DREAM GATE OPEN"));
        platforms.ForEach(p => p.SetActive(true));
        enemies.Where(hm => hm.hp > 0).ForEach(hm => hm.gameObject.SetActive(true));
        shinies.ForEach(p => p.SetActive(false));
    }

    private float hitProgress = 0;
    private float sinceLastHit = float.PositiveInfinity;
    private bool lit = false;

    private void OnHit()
    {
        hitProgress += 1;
        sinceLastHit = 0;
        if (hitProgress >= 2.9f)
        {
            lit = !lit;
            if (lit) Light(); else Extinguish();
            hitProgress = 0;
        }
    }

    private const float X1 = 47;
    private const float X2 = 68;
    private const float Y1 = 47;
    private const float Y2 = 58;

    private const float VICTORY_WAIT = 1.5f;
    private float victoryTimer = 0;
    private bool complete = false;

    private void Update()
    {
        sinceLastHit += Time.deltaTime;
        if (sinceLastHit > 1) hitProgress -= Time.deltaTime;
        if (hitProgress < 0) hitProgress = 0;

        if (complete) return;

        var kPos = HeroController.instance.transform.position;
        if (lit && kPos.x >= X1 && kPos.x <= X2 && kPos.y >= Y1 && kPos.y <= Y2)
        {
            victoryTimer += Time.deltaTime;
            if (victoryTimer >= VICTORY_WAIT)
            {
                // Win!
                Location?.SummonItems(new(53.5f, 55f));
                dreamWalls.ForEach(w => w.SendEvent("DREAM GATE OPEN"));
                complete = true;
            }
        }
        else victoryTimer = 0;
    }

    private static void Activate(List<GameObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj.GetComponent<ParticleSystem>() is ParticleSystem sys) sys.Play();
            else obj.SetActive(true);
        }
    }

    private static void Deactivate(List<GameObject> objects, bool hard = false)
    {
        foreach (var obj in objects)
        {
            if (obj.GetComponent<ParticleSystem>() is ParticleSystem sys) sys.Stop(true, hard ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            else obj.SetActive(false);
        }
    }
}