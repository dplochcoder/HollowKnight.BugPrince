using ItemChanger;
using ItemChanger.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC;

internal class VaultModule : ItemChanger.Modules.Module
{
    public override void Initialize()
    {
        ItemChangerMod.Modules.GetOrAdd<BugPrinceSceneLoaderModule>();

        Events.AddSceneChangeEdit(SceneNames.Ruins2_04, SpawnDoor);
    }

    public override void Unload()
    {
        Events.RemoveSceneChangeEdit(SceneNames.Ruins2_04, SpawnDoor);
    }

    private void SpawnDoor(Scene scene)
    {
        // Manipulate graphics
        var bgWall = scene.FindGameObject("ruins_bg_wall1 (2)")!;
        bgWall.transform.SetPositionY(bgWall.transform.position.y + 2);
        var frame = scene.FindGameObject("ruins_bg_door_03 (1)")!;
        var closed = scene.FindGameObject("ruins_bg_door_02 (1)")!;
        var frameCopy = Object.Instantiate(frame, closed.transform.position, Quaternion.identity);
        frameCopy.transform.SetPositionY(24.54f);
        closed.SetActive(false);
        var interiorCopy = Object.Instantiate(scene.FindGameObject("room_interior1 (3)")!);
        interiorCopy.transform.position = new(71, 22.8f, 2.04f);

        // Grab door transition
        var transition = Object.Instantiate(scene.FindGameObject("door_Ruin_House_02")!);
        transition.name = "door_BugPrince_Vault";
        transition.transform.position = new(70.8f, 19.8f, 0.2f);
        var fsm = transition.LocateMyFSM("Door Control");
        fsm.FsmVariables.GetFsmString("Entry Gate").Value = "left1";
        fsm.FsmVariables.GetFsmString("New Scene").Value = "BugPrince_Vault";
    }
}
