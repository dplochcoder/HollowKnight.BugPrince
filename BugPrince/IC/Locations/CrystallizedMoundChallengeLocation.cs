using BugPrince.Util;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
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
        GameObject ctrl = new("LanternControl");
        ctrl.AddComponent<LanternControl>().Location = this;
    }
}

internal class LanternControl : MonoBehaviour
{
    private static readonly Vector3 LANTERN_POSITION = new(17.5f, 12f, 1.5f);

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
        dreamParticles = dreamEffects.Children();
        dreamParticles.ForEach(o => o.SetActive(true));
        Deactivate(dreamParticles);
        dreamEffects.SetActive(true);

        var firstLightParent = lanternClone.FindChild("Lantern First Light")!;
        var firstLightObj = firstLightParent.FindChild("particle_flame (1)")!;
        foreach (var child in firstLightParent.Children()) child.SetActive(false);
        firstLightObj.SetActive(true);
        firstLightParent.SetActive(true);
        firstLight = firstLightObj.GetComponent<ParticleSystem>();
        firstLight.Stop();

        var lanternLitObjects = lanternClone.FindChild("Lantern Lit Objects")!;
        var audioLoop = lanternLitObjects.FindChild("Main Audio Loop")!;
        audioLoop.SetActive(false);
        var grimmFlame = lanternLitObjects.FindChild("Grimm_lantern_flame")!;
        grimmFlameChildren = grimmFlame.Children();
        grimmFlameChildren.ForEach(o => o.SetActive(true));
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
    }

    private AudioClip? lightSound;
    private GameObject? sharpFlash;
    private GameObject? flamePillar;
    private ParticleSystem? firstLight;
    private ParticleSystem? finalFlames;
    private List<GameObject> grimmFlameChildren = [];
    private List<GameObject> dreamParticles = [];
    private List<PlayMakerFSM> dreamWalls = [];
    private GameObject? mainAudioLoop;
    private List<GameObject> platforms = [];

    private PlayMakerFSM SpawnDreamWall(Vector2 pos, bool rightWall)
    {
        var wall = Instantiate(BugPrincePreloader.Instance.DreamWall!);
        wall.transform.position = pos;
        wall.transform.localRotation = Quaternion.Euler(0, 0, rightWall ? 180 : 0);
        return wall.LocateMyFSM("Control");
    }

    private void Light()
    {
        sharpFlash?.SetActive(true);
        flamePillar?.SetActive(true);
        finalFlames?.Play();
        Activate(dreamParticles);
        if (!complete) dreamWalls.ForEach(w => w.SendEvent("DREAM GATE CLOSE"));
        gameObject.GetOrAddComponent<AudioSource>().PlayOneShot(lightSound);
        platforms.ForEach(p => p.SetActive(false));

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
        gameObject.GetOrAddComponent<AudioSource>().PlayOneShot(lightSound);
        Deactivate(grimmFlameChildren);
        Deactivate(dreamParticles);
        dreamWalls.ForEach(w => w.SendEvent("DREAM GATE OPEN"));
        platforms.ForEach(p => p.SetActive(true));
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

    private const float VICTORY_WAIT = 2;
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