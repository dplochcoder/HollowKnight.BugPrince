using BugPrince.Util;
using GlobalEnums;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using Modding;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC;

internal class GemstoneCavernModule : ItemChanger.Modules.Module
{
    public bool BrokeWall;

    public override void Initialize()
    {
        ItemChangerMod.Modules.GetOrAdd<BugPrinceSceneLoaderModule>();

        Events.AddSceneChangeEdit(SceneNames.Mines_20, SpawnBreakableWall);
        Events.OnBeginSceneTransition += ApplyTransitionFixes;
        ModHooks.GetPlayerBoolHook += GetPlayerBool;
        ModHooks.SetPlayerBoolHook += SetPlayerBool;
    }

    public override void Unload()
    {
        Events.RemoveSceneChangeEdit(SceneNames.Mines_20, SpawnBreakableWall);
        Events.OnBeginSceneTransition -= ApplyTransitionFixes;
        ModHooks.GetPlayerBoolHook -= GetPlayerBool;
        ModHooks.SetPlayerBoolHook -= SetPlayerBool;
    }

    private bool GetPlayerBool(string name, bool orig) => name == BREAKABLE_WALL_NAME ? BrokeWall : orig;
    private bool SetPlayerBool(string name, bool value) => name == BREAKABLE_WALL_NAME ? (BrokeWall = value) : value;

    private const string GATE_NAME = "right_gemstonecavern";
    private const string BREAKABLE_WALL_NAME = "bugPrince_brokeGemstoneCavernWall";

    private void SpawnBreakableWall(Scene scene)
    {
        scene.FindGameObject("Mines_floor_97 (2)")?.SetActive(false);

        var template = scene.FindGameObject("Mines_floor_97 (1)")!;
        template.transform.SetPositionY(template.transform.position.y + 0.96f);
        var floor = Object.Instantiate(template);
        floor.transform.position = new(73.7f, 147.7f, 5.94f);
        floor.transform.rotation = Quaternion.Euler(0, 0, 359.72f);
        var ceiling = Object.Instantiate(template);
        ceiling.transform.position = new(74.13f, 152.43f, 5.94f);
        ceiling.transform.rotation = Quaternion.Euler(0, 0, 179.18f);

        // Don't bulldoze the player.
        scene.FindGameObject("Crystal Crawler (1)").LocateMyFSM("Crawler").GetState("Walk").GetFirstActionOfType<WalkLeftRight>().startLeft = true;

        GameObject tilemapEditorObj = new("TilemapEditor");
        var tilemapEditor = tilemapEditorObj.AddComponent<TilemapEditor>();
        tilemapEditor.P1 = new(72, 148);
        tilemapEditor.P2 = new(75, 152);

        var wall = Object.Instantiate(BugPrincePreloader.Instance.CrystalBreakableWall!);
        wall.transform.position = new(73.56f, 150.18f, -0.15f);
        wall.transform.localScale = new(1.103f, 1.0125f, 1);
        wall.SetActive(true);
        var fsm = wall.LocateMyFSM("breakable_wall_v2");
        fsm.FsmVariables.GetFsmString("PlayerData Bool").Value = BREAKABLE_WALL_NAME;

        GameObject transitionObj = new(GATE_NAME);
        transitionObj.transform.SetParent(scene.FindGameObject("_Transition Gates")!.transform);
        transitionObj.transform.position = new(74.5f, 150f);
        transitionObj.layer = (int)PhysLayers.HERO_DETECTOR;
        var tCollider = transitionObj.AddComponent<BoxCollider2D>();
        tCollider.isTrigger = true;
        tCollider.size = new(1, 4);
        GameObject hrmObj = new("hazard_respawn");
        hrmObj.transform.position = transitionObj.transform.position + new Vector3(-3, 0, 0);
        var hrm = hrmObj.AddComponent<HazardRespawnMarker>();
        hrm.respawnFacingRight = false;
        var transition = transitionObj.AddComponent<TransitionPoint>();
        transition.targetScene = "BugPrince_GemstoneCavern";
        transition.entryPoint = "left1";
        transition.respawnMarker = hrm;
    }

    private void ApplyTransitionFixes(Transition t)
    {
        if (t.SceneName == SceneNames.Mines_20 && t.GateName == GATE_NAME) BrokeWall = true;
    }
}

internal class TilemapEditor : MonoBehaviour
{
    internal Vector2Int P1;
    internal Vector2Int P2;

    private tk2dTileMap? tilemap;

    internal void Update()
    {
        if (tilemap == null)
        {
            tilemap = GameObject.Find("TileMap")?.GetComponent<tk2dTileMap>();
            if (tilemap == null) return;

            for (int x = P1.x; x < P2.x; x++) for (int y = P1.y; y < P2.y; y++) tilemap.ClearTile(x, y, 0);
            tilemap.ForceBuild();
            tilemap.gameObject.DoOnDestroy(() => tilemap = null);
        }
    }
}
