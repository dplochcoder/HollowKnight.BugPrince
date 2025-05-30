using BugPrince.Util;
using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using SFCore.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class SoulSanctumEscapeTag : Tag
{
    private const string SCENE_NAME = SceneNames.Ruins1_25;

    public bool WindowBroken;

    public override void Load(object parent)
    {
        base.Load(parent);
        Events.AddSceneChangeEdit(SCENE_NAME, CreateSoulSanctumEscape);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        Events.RemoveSceneChangeEdit(SCENE_NAME, CreateSoulSanctumEscape);
    }

    private void CreateSoulSanctumEscape(Scene scene)
    {
        GameObjectUtil.MakeHazardBox(new(14, 1), new(15, -3));
        GameObjectUtil.MakeHazardBox(new(15, -2), new(41, -3));
        GameObjectUtil.MakeHazardBox(new(41, -3), new(42, 122));

        var terrain = scene.Find("Window Collider (3)");
        var window = scene.Find("ruins_mid_window_01 (5)");

        if (WindowBroken) BreakWindow(terrain, window, true);
        else EditWindow(terrain, window);
    }

    private static EmbeddedSprite crackedWindow = new("Game.cracked_window");
    private static readonly Vector2 BREAK_A = new(14.75f, 3.5f);
    private static readonly Vector2 BREAK_B = new(14.75f, 8f);

    private void EditWindow(GameObject terrain, GameObject window)
    {
        window.FindChild("ruin_layered_0055_w")!.GetComponent<SpriteRenderer>().sprite = crackedWindow.Value;

        GameObject detector = new("BreakDetector");
        detector.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
        detector.transform.position = (BREAK_A + BREAK_B) / 2;

        var box = detector.AddComponent<BoxCollider2D>();
        box.size = new(0.6f, Mathf.Abs(BREAK_A.y - BREAK_B.y));
        box.isTrigger = true;

        detector.AddComponent<WindowBreakDetector>().OnBreak = () => BreakWindow(terrain, window, false);
    }

    private static EmbeddedSprite clippedWindowDressing = new("Game.clipped_window_dressing");

    private void BreakWindow(GameObject terrain, GameObject window, bool immediate)
    {
        terrain.SetActive(false);
        foreach (Transform child in window.transform)
        {
            if (child.name == "ruin_layered_0054_w") child.gameObject.GetComponent<SpriteRenderer>().sprite = clippedWindowDressing.Value;
            else child.gameObject.SetActive(false);
        }

        if (immediate) return;

        window.GetOrAddComponent<AudioSource>().PlayOneShot(BugPrincePreloader.Instance.QuakeFloorGlassShatterAudio);

        int breakables = Random.Range(8, 13);
        for (int i = 0; i < breakables; i++)
        {
            var speed = Random.Range(15f, 25f);
            var angle = Random.Range(20f, 50f);
            var velocity = Quaternion.Euler(0, 0, angle) * new Vector3(1, 0, 0);
            GameObjectUtil.FlingGlassDebris(Vector3.Lerp(BREAK_A, BREAK_B, Random.Range(0f, 1f)), velocity);
        }
    }
}

internal class WindowBreakDetector : MonoBehaviour, IHitResponder
{
    internal System.Action? OnBreak;

    public void Hit(HitInstance damageInstance)
    {
        if (damageInstance.Source?.LocateMyFSM("Fireball Control") != null)
        {
            OnBreak?.Invoke();
            Destroy(this);
        }
    }
}
