using BugPrince.Util;
using GlobalEnums;
using ItemChanger.Extensions;
using Modding.Converters;
using Newtonsoft.Json;
using SFCore.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal record ColliderBox
{
    [JsonConverter(typeof(Vector2Converter))] public Vector2 p1;
    [JsonConverter(typeof(Vector2Converter))] public Vector2 p2;
    public bool Spikes;
    public bool Terrain;

    internal GameObject MakeGameObject()
    {
        GameObject obj = new("Hazard");
        obj.transform.position = (p1 + p2) / 2;

        var box = obj.AddComponent<BoxCollider2D>();
        box.size = new(Mathf.Abs(p2.x - p1.x), Mathf.Abs(p2.y - p1.y));

        if (Terrain) obj.layer = (int)PhysLayers.TERRAIN;
        else
        {
            box.isTrigger = true;
            obj.layer = (int)PhysLayers.ENEMIES;
            var damage = obj.AddComponent<DamageHero>();
            damage.damageDealt = 1;
            damage.hazardType = 2;

            if (!Spikes) obj.AddComponent<NonBouncer>();
            else
            {
                var tink = obj.AddComponent<TinkEffect>();
                tink.blockEffect = BugPrincePreloader.Instance.Goam!.GetComponent<TinkEffect>().blockEffect;
                tink.SetAttr("boxCollider", box);
                tink.useNailPosition = true;
            }
        }

        return obj;
    }
}

internal class WindowBreakTag : SceneModifierTag
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
    public List<ColliderBox> HazardBoxes = [];
    [JsonConverter(typeof(Vector2Converter))] public Vector2 HazardRespawnTriggerPos;
    [JsonConverter(typeof(Vector2Converter))] public Vector2 HazardRespawnTriggerSize;
    [JsonConverter(typeof(Vector2Converter))] public Vector2 HazardRespawnMarkerPos;
    public bool RespawnRight;

    public bool Broken;

    protected override string GetSceneName() => SceneName;

    protected override void ModifyScene(Scene scene)
    {
        HazardBoxes.ForEach(b => b.MakeGameObject());
        AddHazardRespawnTrigger();

        var terrain = scene.FindGameObject(TerrainPath)!;
        var window = scene.FindGameObject(WindowPath)!;

        if (Broken) BreakWindow(terrain, window, true);
        else EditWindow(terrain, window);
    }

    private static readonly EmbeddedSprite crackedWindow = new("Game.cracked_window");

    private void AddHazardRespawnTrigger()
    {
        GameObject markerObj = new("HazardRespawnMarker");
        markerObj.transform.position = HazardRespawnMarkerPos;
        var marker = markerObj.AddComponent<HazardRespawnMarker>();
        marker.respawnFacingRight = RespawnRight;

        GameObject triggerObj = new("Trigger")
        {
            layer = (int)PhysLayers.HERO_DETECTOR
        };
        triggerObj.transform.position = HazardRespawnTriggerPos;
        var box = triggerObj.AddComponent<BoxCollider2D>();
        box.size = HazardRespawnTriggerSize;
        box.isTrigger = true;
        var trigger = triggerObj.AddComponent<HazardRespawnTrigger>();
        trigger.respawnMarker = marker;
    }

    private void EditWindow(GameObject terrain, GameObject window)
    {
        window.FindChild("ruin_layered_0055_w")!.GetComponent<SpriteRenderer>().sprite = crackedWindow.Value;

        GameObject detector = new("BreakDetector")
        {
            layer = (int)PhysLayers.INTERACTIVE_OBJECT
        };
        detector.transform.position = new(PaneX, (PaneY1 + PaneY2) / 2);
        detector.AddComponent<NonBouncer>();

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

        int breakables = Mathf.CeilToInt(Mathf.Abs(PaneY2 - PaneY1) / 0.4f);
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
