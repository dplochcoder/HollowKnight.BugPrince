using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Locations;
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

    private static List<string> PLATFORM_NAMES = [
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
        List<PlayMakerFSM> torches = [.. boxes.Select(SpawnTorch)];

        GameObject tracker = new("PuzzleTracker");
        tracker.AddComponent<PlatformPuzzleTracker>().Init(this, boxes, torches);
    }

    private PlayMakerFSM SpawnTorch(BoxCollider2D platform)
    {
        var obj = Object.Instantiate(BugPrincePreloader.Instance.TorchProp!);

        var bounds = platform.bounds;
        int seed = platform.name.GetStableHashCode();
        obj.transform.position = new((bounds.min.x + bounds.max.x) / 2, bounds.max.y + 2);

        System.Random rng = new(platform.name.GetStableHashCode());
        obj.transform.localRotation = Quaternion.Euler(0, 0, rng.Next(11) - 5);

        Object.Destroy(obj.GetComponent<Breakable>());
        obj.SetActive(true);

        var fsm = obj.FindChild("Pop")!.LocateMyFSM("Pop Control");
        var waitState = fsm.GetState("Wait");
        waitState.RemoveTransitionsTo("Pop");
        waitState.AddTransition("MANUAL POP", "Pop");

        return fsm;
    }
}

internal class PlatformPuzzleTracker : MonoBehaviour
{
    private OvergrownMoundPuzzleLocation? Location;
    private List<BoxCollider2D> boxes = [];
    private List<PlayMakerFSM> torches = [];

    internal void Init(OvergrownMoundPuzzleLocation location, List<BoxCollider2D> boxes, List<PlayMakerFSM> torches)
    {
        this.Location = location;
        this.boxes = [.. boxes];
        this.torches = [.. torches];
    }

    private bool solved = false;

    private int next = 0;
    private float nextPop = Random.Range(3f, 5f);

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

    private static readonly Vector2 SPAWN_POS = new(76.5f, 21f);

    private IEnumerator Run()
    {
        // Wait 1 frame for init.
        yield return null;

        while (true)
        {
            yield return new WaitUntil(() => lastTouchedIndex == 0);

            torches[1].SendEvent("MANUAL POP");
            int expected = 1;
            while (true)
            {
                if (lastTouchedIndex == expected - 1) yield return null;
                else if (lastTouchedIndex == expected)
                {
                    if (++expected < torches.Count)
                    {
                        torches[expected].SendEvent("MANUAL POP");
                        continue;
                    }

                    // Done!
                    Location?.SummonItems(SPAWN_POS);

                    foreach (var torch in torches)
                    {
                        torch.SendEvent("MANUAL POP");
                        yield return new WaitForSeconds(0.15f);
                    }
                    yield break;
                }
                else
                {
                    // Failure.
                    foreach (var torch in torches) torch.SendEvent("MANUAL POP");

                    yield return new WaitForSeconds(1);
                    lastTouchedIndex = -1;
                    torches[0].SendEvent("MANUAL POP");
                }
            }
        }
    }
}
