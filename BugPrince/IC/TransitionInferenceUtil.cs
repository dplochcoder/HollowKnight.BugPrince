using ItemChanger;
using RandomizerMod.RC;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BugPrince.IC;

internal static class TransitionInferenceUtil
{
    // Mostly copied from ItemChanger.
    internal static bool GetSrcTarget(GameManager gameManager, GameManager.SceneLoadInfo info, out Transition source, out Transition target)
    {
        source = default;
        target = default;
        if (info.GetType() != typeof(GameManager.SceneLoadInfo)) return false;

        string sceneName = gameManager.sceneName;
        string? gateName = null;
        Transition origTarget = new(info.SceneName, info.EntryGateName);
        target = new(info.SceneName, info.EntryGateName);

        TransitionPoint tp = Object.FindObjectsOfType<TransitionPoint>().FirstOrDefault(p => p.entryPoint == info.EntryGateName && p.targetScene == info.SceneName);
        if (tp != null)
        {
            gateName = tp.name.Split(null)[0];
            if (sceneName == SceneNames.Fungus2_14 && gateName[0] == 'b') gateName = "bot3";
            else if (sceneName == SceneNames.Fungus2_15 && gateName[0] == 't') gateName = "top3";
        }
        else
        {
            switch (sceneName)
            {
                case SceneNames.Fungus3_44 when info.EntryGateName == "left1":
                case SceneNames.Crossroads_02 when info.EntryGateName == "left1":
                case SceneNames.Crossroads_06 when info.EntryGateName == "left1":
                case SceneNames.Deepnest_10 when info.EntryGateName == "left1":
                case SceneNames.Ruins1_04 when info.SceneName == SceneNames.Room_nailsmith:
                case SceneNames.Fungus3_48 when info.SceneName == SceneNames.Room_Queen:
                    gateName = "door1";
                    break;
                case SceneNames.Town when info.SceneName == SceneNames.Room_shop:
                    gateName = "door_sly";
                    break;
                case SceneNames.Town when info.SceneName == SceneNames.Room_Town_Stag_Station:
                    gateName = "door_station";
                    break;
                case SceneNames.Town when info.SceneName == SceneNames.Room_Bretta:
                    gateName = "door_bretta";
                    break;
                case SceneNames.Town when info.SceneName == SceneNames.Grimm_Main_Tent:
                    gateName = "room_grimm";
                    break;
                case SceneNames.Crossroads_04 when info.SceneName == SceneNames.Room_Charm_Shop:
                    gateName = "door_charmshop";
                    break;
                case SceneNames.Crossroads_04 when info.SceneName == SceneNames.Room_Mender_House:
                    gateName = "door_Mender_House";
                    break;
                default:
                    break;
            }
        }

        if (sceneName == null || gateName == null) return false;
        
        source = new(sceneName, gateName);
        if (!ItemChanger.Internal.Ref.Settings.TransitionOverrides.TryGetValue(source, out var modified)) return false;
            
        // automatically handle the split Mantis Village transition (while still using a consistent transition value for events)
        // the original behavior is not possible when the source is a horizontal transition, in which case the gate is not changed.
        if (info.SceneName == SceneNames.Fungus2_14 && info.EntryGateName == "bot3" && HeroController.SilentInstance?.cState?.facingRight == false
            && !source.GateName.StartsWith("left") && !source.GateName.StartsWith("right"))
        {
            info.EntryGateName = "bot1";
        }

        target = new(modified.SceneName, modified.GateName);
        return true;
    }

    internal static Transition ToStruct(this ITransition self) => new(self.SceneName, self.GateName);

    internal static Transition ToStruct(this RandoModTransition self) => new(self.TransitionDef.SceneName, self.TransitionDef.DoorName);
}
