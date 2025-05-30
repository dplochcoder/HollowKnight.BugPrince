using BugPrince.Util;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using PurenailCore.SystemUtil;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Locations;

internal class AncestralMoundPuzzleLocation : ContainerLocation
{
    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, ModifyScene);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, ModifyScene);

    private static int Seed() => RandomizerMod.RandomizerMod.RS.GenerationSettings.Seed + 47;

    private void ModifyScene(Scene scene)
    {
        System.Random rng = new(Seed());
        var numTorches = rng.Next(6, 8);

        List<int> order = [];
        for (int i = 0; i < numTorches; i++) order.Add(i);
        List<int> suffix = new(order);
        suffix.Shuffle(rng);
        for (int i = 0; i < 10 - numTorches; i++) order.Add(suffix[i]);
        order.Shuffle(rng);

        UnityEngine.Object.Destroy(scene.FindGameObject("_Props/Torch Breakable 4")!);
        var torchTemplate = scene.FindGameObject("_Props/Torch Breakable 3")!;
        torchTemplate.transform.Translate(new(0, -0.2f));
        torchTemplate.transform.localRotation = Quaternion.Euler(0, 0, rng.NextFloat(-13, 3));

        var popParticles = torchTemplate.FindChild("Pop")!;
        var popClips = torchTemplate.LocateMyFSM("Pop Control").GetState("Pop").GetFirstActionOfType<AudioPlayerOneShot>().audioClips;

        var min = torchTemplate.transform.position;
        var max = min with { x = 74.5f };

        List<GameObject> signals = [torchTemplate];
        for (int i = 1; i < numTorches; i++)
        {
            var next = UnityEngine.Object.Instantiate(torchTemplate);
            next.transform.position = Vector3.Lerp(min, max, i / (numTorches - 1f));
            next.transform.localRotation = Quaternion.Euler(0, 0, rng.NextFloat(-13, 3));
            signals.Add(next);
        }

        var leftSwitch = scene.FindGameObject("_Scenery/torch15")!;
        var rightSwitch = scene.FindGameObject("_Scenery/torch14")!;
        List<GameObject> switches = [leftSwitch];
        for (int i = 1; i < numTorches - 1; i++)
        {
            var next = UnityEngine.Object.Instantiate(leftSwitch);
            next.transform.position = Vector3.Lerp(leftSwitch.transform.position, rightSwitch.transform.position, i / (numTorches - 1f));
        }
        switches.Add(rightSwitch);

        void SpawnContainer(Vector2 pos)
        {
            GetContainer(out var obj, out var containerType);
            Container.GetContainer(containerType)!.ApplyTargetContext(obj, pos.x, pos.y, 0);
            obj.SetActive(true);
        }

        CoordinatePops([.. order.Select(idx => signals[idx])]);
        MakeSwitches(order, [.. order.Select(idx => switches[idx])], popParticles, popClips, SpawnContainer);
    }

    private const float INTERVAL = 0.35f;

    private static void CoordinatePops(List<GameObject> signals)
    {
        signals.ForEach(o => o.SetActive(false));

        Wrapped<float> nextWait = new(0);
        for (int i = 0; i < signals.Count; i++)
        {
            var fsm = signals[i].FindChild("Pop")!.LocateMyFSM("Pop Control");
            var waitState = fsm.GetState("Wait");
            var wait = waitState.GetFirstActionOfType<WaitRandom>();

            var firstWait = 1f + i * INTERVAL;
            bool first = i == 0;
            Wrapped<bool> firstIter = new(false);
            waitState.AddFirstAction(new Lambda(() =>
            {
                var waitTime = firstIter.Value ? firstWait : nextWait.Value;
                firstIter.Value = false;
                wait.timeMin.Value = waitTime;
                wait.timeMax.Value = waitTime;

                if (first)
                {
                    var min = INTERVAL * signals.Count + 3;
                    nextWait.Value = UnityEngine.Random.Range(min, min + 2);
                }
            }));

            fsm.SetState("Init");
        }

        signals.ForEach(o => o.SetActive(true));
    }

    private const int NUM_PARTICLES = 12;

    private void MakeSwitches(IReadOnlyList<int> order, List<GameObject> switches, GameObject particlesTemplate, AudioClip[] popClips, Action<Vector2> spawnContainer)
    {
        List<int> history = [];
        Wrapped<bool> complete = new(false);
        for (int i = 0; i < switches.Count; i++)
        {
            GameObject obj = new("Switch");
            obj.transform.position = new(switches[i].transform.position.x, switches[i].transform.position.y - 2.8f, 0);
            obj.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
            var box = obj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new(1, 1);

            GameObject particlesParent = new("ParticlesParent");
            particlesParent.transform.position = obj.transform.position;
            List<GameObject> particles = [];
            for (int j = 0; j < NUM_PARTICLES; j++)
            {
                var copy = UnityEngine.Object.Instantiate(particlesTemplate);
                copy.transform.SetParent(particlesParent.transform);
                copy.transform.position = particlesParent.transform.position;
                copy.transform.localRotation = Quaternion.Euler(0, 0, j * 360f / NUM_PARTICLES);
                particles.Add(copy);
            }

            obj.AddComponent<TorchPuzzleSwitch>().Init(
                i, particlesParent, [.. particles.Select(o => o.GetComponent<ParticleSystem>())], history, order, complete, popClips, this);
        }
    }
}

internal class TorchPuzzleSwitch : MonoBehaviour, IHitResponder
{
    private int idx;
    private GameObject particlesParent;
    private List<ParticleSystem> particles = [];
    private List<int> history;
    private IReadOnlyList<int> order;
    private Wrapped<bool> complete;
    private AudioClip[] audioClips;
    private ContainerLocation location;

    private float invuln;

    internal void Init(int idx, GameObject particlesParent, List<ParticleSystem> particles, List<int> history, IReadOnlyList<int> order, Wrapped<bool> complete, AudioClip[] audioClips, ContainerLocation location)
    {
        this.idx = idx;
        this.particlesParent = particlesParent;
        this.particles = particles;
        this.history = history;
        this.order = order;
        this.complete = complete;
        this.audioClips = audioClips;
        this.location = location;
    }

    public void Hit(HitInstance damageInstance)
    {
        if (damageInstance.AttackType != AttackTypes.Nail) return;
        if (invuln > 0) return;

        invuln = 0.15f;
        particlesParent.transform.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360f));
        particles.ForEach(p => p.Play());
        gameObject.GetOrAddComponent<AudioSource>().PlayOneShot(audioClips.Random());

        if (complete.Value) return;

        history.Add(idx);
        if (history.Count > order.Count) history.RemoveAt(0);
        if (history.SequenceEqual(order))
        {
            complete.Value = true;
            location.SummonItems(gameObject.transform.position);
        }
    }

    private void Update()
    {
        if (invuln > 0) invuln -= Time.deltaTime;
        if (invuln < 0) invuln = 0;
    }
}
