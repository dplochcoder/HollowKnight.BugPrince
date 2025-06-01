using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Locations;
using PurenailCore.GOUtil;
using RandomizerMod.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Locations;

internal class OvergrownMoundPuzzleLocation : ContainerLocation
{
    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, ModifyScene);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, ModifyScene);

    private static readonly List<string> PLATFORM_NAMES = [
        "fung_plat_float_01",
        "fung_plat_float_05",
        "fung_plat_float_07 (1)",
        "fung_plat_float_04",
        "fung_plat_float_08",
        "fung_plat_float_09",
        "fung_plat_float_05 (1)",
        "fung_plat_float_08 (2)",
        "fung_plat_float_08 (1)",
        "fung_plat_float_07"
    ];

    private void ModifyScene(Scene scene)
    {
        List<BoxCollider2D> boxes = [.. PLATFORM_NAMES.Select(n => scene.FindGameObject(n)!.GetComponent<BoxCollider2D>())];
        List<TorchInterface> torches = [.. boxes.Select(SpawnTorch)];

        GameObject tracker = new("PuzzleTracker");
        tracker.AddComponent<PlatformPuzzleTracker>().Init(this, boxes, torches);
    }

    private TorchInterface SpawnTorch(BoxCollider2D platform)
    {
        var obj = Object.Instantiate(BugPrincePreloader.Instance.TorchProp!);

        var bounds = platform.bounds;
        obj.transform.position = new((bounds.min.x + bounds.max.x) / 2, bounds.max.y + 2, 0.2f);

        System.Random rng = new(platform.name.GetStableHashCode());
        obj.transform.localRotation = Quaternion.Euler(0, 0, rng.Next(11) - 5);

        Object.Destroy(obj.GetComponent<Breakable>());
        obj.SetActive(true);

        var fsm = obj.FindChild("Active")!.FindChild("Pop")!.LocateMyFSM("Pop Control");
        var waitState = fsm.GetState("Wait");
        waitState.RemoveTransitionsTo("Pop");
        waitState.AddTransition("MANUAL POP", "Pop");

        GameObject holder = new("TorchInterface");
        var torch = holder.AddComponent<TorchInterface>();
        torch.Init(fsm);
        return torch;
    }
}

internal class TorchInterface : MonoBehaviour
{
    private const float SCALE_MULTIPLIER = 2.5f;
    private const float SHRINK_PERIOD = 0.6f;

    private PlayMakerFSM? fsm;
    private GameObject? torchGlow;
    private Vector3 baseScale;

    internal void Init(PlayMakerFSM fsm)
    {
        this.fsm = fsm;
        torchGlow = fsm.gameObject.transform.parent.gameObject.FindChild("torch_glow_02")!;
        torchGlow.GetComponent<SpriteRenderer>().SetAlpha(1f);
        baseScale = torchGlow.transform.localScale;

        var particleSystem = fsm.gameObject.GetComponent<ParticleSystem>();
        var main = particleSystem.main;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    private float sincePop = float.PositiveInfinity;

    internal void Pop()
    {
        fsm?.SendEvent("MANUAL POP");
        sincePop = 0;
    }


    private void Update()
    {
        sincePop += Time.deltaTime;
        
        if (torchGlow != null)
        {
            float pct = sincePop > SHRINK_PERIOD ? 1 : sincePop / SHRINK_PERIOD;
            float cos = Mathf.Cos(pct * Mathf.PI / 2);
            torchGlow.transform.localScale = baseScale * (1 + (SCALE_MULTIPLIER - 1) * cos);
        }
    }
}

internal class PlatformPuzzleTracker : MonoBehaviour
{
    private OvergrownMoundPuzzleLocation? Location;
    private List<BoxCollider2D> boxes = [];
    private List<TorchInterface> torches = [];

    internal void Init(OvergrownMoundPuzzleLocation location, List<BoxCollider2D> boxes, List<TorchInterface> torches)
    {
        this.Location = location;
        this.boxes = [.. boxes];
        this.torches = [.. torches];
    }

    private void Awake()
    {
        On.HeroController.OnCollisionEnter2D += TrackCollisionEnter;

        StartCoroutine(Run());
    }

    private void OnDestroy() => On.HeroController.OnCollisionEnter2D -= TrackCollisionEnter;

    private bool IsTerrain(GameObject obj) => obj.layer == (int)PhysLayers.TERRAIN;

    private int lastTouchedIndex = -1;

    private void TrackCollisionEnter(On.HeroController.orig_OnCollisionEnter2D orig, HeroController self, Collision2D collision)
    {
        if (collision.collider is BoxCollider2D box)
        {
            int idx = boxes.IndexOf(box);
            if (idx >= 0) lastTouchedIndex = idx;
        }

        orig(self, collision);
    }

    private static readonly Vector2 SPAWN_POS = new(86.5f, 14.5f);

    private IEnumerator Run()
    {
        // Wait 1 frame for init.
        yield return null;

        while (true)
        {
            var wait = Random.Range(3f, 6f);
            while (lastTouchedIndex != 0)
            {
                yield return null;
                wait -= Time.deltaTime;
                if (wait <= 0)
                {
                    wait += Random.Range(3f, 6f);
                    torches[0].Pop();
                }
            }

            torches[1].Pop();
            wait = Random.Range(3f, 8f);
            int expected = 1;
            while (true)
            {
                if (lastTouchedIndex == expected - 1)
                {
                    yield return null;
                    wait -= Time.deltaTime;
                    if (wait <= 0)
                    {
                        wait += Random.Range(3f, 6f);
                        torches[expected].Pop();
                    }
                }
                else if (lastTouchedIndex == expected)
                {
                    if (++expected < torches.Count)
                    {
                        torches[expected].Pop();
                        wait = Random.Range(3f, 6f);
                        continue;
                    }

                    // Done!
                    Location?.SummonItems(SPAWN_POS);

                    foreach (var torch in torches)
                    {
                        torch.Pop();
                        yield return new WaitForSeconds(0.15f);
                    }
                    yield break;
                }
                else
                {
                    // Failure.
                    foreach (var torch in torches) torch.Pop();

                    yield return new WaitForSeconds(1);
                    lastTouchedIndex = -1;
                    torches[0].Pop();
                    break;
                }
            }
        }
    }
}
