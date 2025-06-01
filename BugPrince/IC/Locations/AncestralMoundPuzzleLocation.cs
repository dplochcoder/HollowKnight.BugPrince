using BugPrince.Util;
using GlobalEnums;
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

internal class AncestralMoundPuzzleLocation : ContainerLocation
{
    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, ModifyScene);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, ModifyScene);

    private static int Seed() => RandomizerMod.RandomizerMod.RS.GenerationSettings.Seed + 47;

    private void ModifyScene(Scene scene)
    {
        System.Random rng = new(Seed());
        var numTorches = rng.Next(5, 7);

        List<int> order = [];
        for (int i = 0; i < numTorches; i++) order.Add(i);
        List<int> suffix = [.. order];
        suffix.Shuffle(rng);
        for (int i = 0; i < 8 - numTorches; i++) order.Add(suffix[i]);
        order.Shuffle(rng);

        scene.FindGameObject("_Props/Torch Breakable 4")?.SetActive(false);
        scene.FindGameObject("_Props/Shaman Generic 7")?.SetActive(false);
        scene.FindGameObject("_Scenery/haze/haze16")?.SetActive(false);
        scene.FindGameObject("_Scenery/haze/haze18")?.SetActive(false);

        var torchTemplate = scene.FindGameObject("_Props/Torch Breakable 3")!;
        Object.Destroy(torchTemplate.GetComponent<Breakable>());
        torchTemplate.transform.Translate(new(0, -0.2f));
        torchTemplate.transform.localRotation = Quaternion.Euler(0, 0, rng.NextFloat(-13, 3));

        var popParticles = torchTemplate.FindChild("Active")!.FindChild("Pop")!;
        var particleSystem = popParticles.GetComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var popClips = popParticles.LocateMyFSM("Pop Control").GetState("Pop").GetFirstActionOfType<AudioPlayerOneShot>().audioClips;

        var min = torchTemplate.transform.position;
        var max = min with { x = 74.5f };

        List<GameObject> signals = [torchTemplate];
        for (int i = 1; i < numTorches; i++)
        {
            var next = Object.Instantiate(torchTemplate);
            Object.Destroy(next.GetComponent<Breakable>());
            next.transform.position = Vector3.Lerp(min, max, i / (numTorches - 1f));
            next.transform.localRotation = Quaternion.Euler(0, 0, rng.NextFloat(-13, 3));
            signals.Add(next);
        }

        var leftSwitch = scene.FindGameObject("_Scenery/torch15")!;
        var rightSwitch = scene.FindGameObject("_Scenery/torch14")!;
        List<GameObject> switches = [leftSwitch];
        for (int i = 1; i < numTorches - 1; i++)
        {
            var next = Object.Instantiate(leftSwitch);
            next.transform.position = Vector3.Lerp(leftSwitch.transform.position, rightSwitch.transform.position, i / (numTorches - 1f));
            switches.Add(next);
        }
        switches.Add(rightSwitch);

        CoordinatePops(order, signals);
        MakeSwitches(order, switches, popParticles, popClips);
    }

    private static void CoordinatePops(List<int> order, List<GameObject> signals)
    {
        List<PlayMakerFSM> fsms = [];
        for (int i = 0; i < signals.Count; i++)
        {
            var fsm = signals[i].FindChild("Active")!.FindChild("Pop")!.LocateMyFSM("Pop Control");
            var waitState = fsm.GetState("Wait");
            waitState.RemoveTransitionsTo("Pop");
            waitState.AddTransition("MANUAL POP", "Pop");

            var popState = fsm.GetState("Pop");
            popState.RemoveActionsOfType<PlayParticleEmitter>();
            var particles = fsm.gameObject.GetComponent<ParticleSystem>();
            popState.AddLastAction(new Lambda(() =>
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                particles.Play();
            }));

            fsm.SetState("Init");
            fsms.Add(fsm);
        }

        GameObject popper = new("Popper");
        popper.AddComponent<PopCoordinator>().Init(order, fsms);
    }

    private const int NUM_PARTICLES = 12;

    private void MakeSwitches(IReadOnlyList<int> order, List<GameObject> switches, GameObject particlesTemplate, AudioClip[] popClips)
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
                var copy = Object.Instantiate(particlesTemplate);
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

internal class PopCoordinator : MonoBehaviour
{
    private List<int> order = [];
    private List<PlayMakerFSM> fsms = [];

    private void Awake() => StartCoroutine(Run());

    internal void Init(List<int> order, List<PlayMakerFSM> fsms)
    {
        this.fsms = [.. fsms];
        this.order = [.. order];
    }

    private IEnumerator Run()
    {
        yield return null;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(3f, 5));

            bool first = true;
            foreach (var idx in order)
            {
                if (!first) yield return new WaitForSeconds(0.4f);
                first = false;
                fsms[idx].SendEvent("MANUAL POP");
            }
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
        particlesParent.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
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
