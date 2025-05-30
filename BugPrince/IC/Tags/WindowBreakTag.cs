using BugPrince.Util;
using GlobalEnums;
using ItemChanger;
using ItemChanger.Extensions;
using Modding.Converters;
using Newtonsoft.Json;
using SFCore.Utils;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.UI.GridLayoutGroup;

namespace BugPrince.IC.Tags;

internal record HazardBox
{
    [JsonConverter(typeof(Vector2Converter))] public Vector2 p1;
    [JsonConverter(typeof(Vector2Converter))] public Vector2 p2;
    public bool Spikes;

    private static FieldInfo blockEffect = typeof(TinkEffect).GetField("blockEffect", BindingFlags.Instance | BindingFlags.NonPublic);


    internal GameObject MakeHazardBox()
    {
        GameObject hazard = new("Hazard");
        hazard.transform.position = (p1 + p2) / 2;
        var box = hazard.AddComponent<BoxCollider2D>();
        box.size = new(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y));
        box.isTrigger = true;
        var damage = hazard.AddComponent<DamageHero>();
        damage.damageDealt = 1;
        damage.hazardType = 1 + (int)HazardType.SPIKES;

        if (!Spikes) hazard.AddComponent<NonBouncer>();
        else
        {
            var tink = hazard.AddComponent<TinkEffect>();
            blockEffect.SetValue(tink, blockEffect.GetValue(BugPrincePreloader.Instance.Goam!.GetComponent<TinkEffect>()));
            tink.SetAttr("boxCollider", box);
            tink.useNailPosition = true;
        }

        return hazard;
    }
}

internal class WindowBreakTag : Tag
{
    public string SceneName = "";
    public string TerrainPath = "";
    public string WindowPath = "";
    public string DressingObjName = "";
    public string ClippedDressingSpriteName = "";
    public float PaneX;
    public float PaneY1;
    public float PaneY2;
    public bool Left;
    public List<HazardBox> HazardBoxes = [];

    public bool Broken;

    public override void Load(object parent)
    {
        base.Load(parent);
        Events.AddSceneChangeEdit(SceneName, ModifyScene);
    }

    public override void Unload(object parent)
    {
        base.Unload(parent);
        Events.RemoveSceneChangeEdit(SceneName, ModifyScene);
    }

    private void ModifyScene(Scene scene)
    {
        foreach (var box in HazardBoxes) GameObjectUtil.MakeHazardBox(box.p1, box.p2);

        var terrain = scene.Find(TerrainPath)!;
        var window = scene.Find(WindowPath)!;

        if (Broken) BreakWindow(terrain, window, true);
        else EditWindow(terrain, window);
    }

    private static EmbeddedSprite crackedWindow = new("Game.cracked_window");

    private void EditWindow(GameObject terrain, GameObject window)
    {
        window.FindChild("ruin_layered_0055_w")!.GetComponent<SpriteRenderer>().sprite = crackedWindow.Value;

        GameObject detector = new("BreakDetector");
        detector.layer = (int)PhysLayers.INTERACTIVE_OBJECT;
        detector.transform.position = new(PaneX, (PaneY1 + PaneY2) / 2);

        var box = detector.AddComponent<BoxCollider2D>();
        box.size = new(0.6f, Mathf.Abs(PaneY1 - PaneY2));
        box.isTrigger = true;

        detector.AddComponent<WindowBreaker>().OnBreak = () => BreakWindow(terrain, window, false);
    }

    private void BreakWindow(GameObject terrain, GameObject window, bool immediate)
    {
        terrain.SetActive(false);
        foreach (Transform child in window.transform)
        {
            if (child.name == DressingObjName)
            {
                EmbeddedSprite clippedSprite = new(ClippedDressingSpriteName);
                child.gameObject.GetComponent<SpriteRenderer>().sprite = clippedSprite.Value;
            }
            else child.gameObject.SetActive(false);
        }

        if (immediate) return;
        Broken = true;

        window.GetOrAddComponent<AudioSource>().PlayOneShot(BugPrincePreloader.Instance.QuakeFloorGlassShatterAudio);

        int breakables = Random.Range(8, 13);
        for (int i = 0; i < breakables; i++)
        {
            var speed = Random.Range(15f, 25f);
            var angle = Random.Range(20f, 50f);
            if (Left) angle = 180 - angle;

            var velocity = Quaternion.Euler(0, 0, angle) * new Vector3(speed, 0, 0);
            GameObjectUtil.FlingGlassDebris(Vector3.Lerp(new(PaneX, PaneY1), new(PaneX, PaneY2), Random.Range(0f, 1f)), velocity);
        }
    }
}

internal class WindowBreaker : MonoBehaviour, IHitResponder
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
