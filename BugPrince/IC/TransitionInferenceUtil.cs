using BugPrince.Util;
using ItemChanger;
using RandomizerMod.RC;
using System.Linq;
using UnityEngine;

namespace BugPrince.IC;

internal enum GateDirection
{
    Left,
    Right,
    Top,
    Bot,
    Door
}

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

        target = modified.ToStruct();
        return true;
    }

    internal static Transition ToStruct(this ITransition self) => new(self.SceneName, self.GateName);

    internal static Transition ToStruct(this RandoModTransition self) => new(self.TransitionDef.SceneName, self.TransitionDef.DoorName);

    internal static bool ToTransition(this string self, out Transition transition)
    {
        transition = default;
        if (!self.EndsWith("]")) return false;

        var split = self.Split('[');
        if (split.Length != 2) return false;
        if (split[0].Length == 0) return false;
        if (split[1].Length <= 1) return false;

        transition = new(split[0], split[1].Substring(0, split[1].Length - 1));
        return true;
    }

    internal static GateDirection GetDirection(this Transition self)
    {
        if (self.GateName.StartsWith("left")) return GateDirection.Left;
        else if (self.GateName.StartsWith("right")) return GateDirection.Right;
        else if (self.GateName.StartsWith("top")) return GateDirection.Top;
        else if (self.GateName.StartsWith("bot")) return GateDirection.Bot;
        else return GateDirection.Door;
    }

    internal static GateDirection Opposite(this GateDirection self) => self switch
    {
        GateDirection.Left => GateDirection.Right,
        GateDirection.Right => GateDirection.Left,
        GateDirection.Bot => GateDirection.Top,
        GateDirection.Top => GateDirection.Bot,
        GateDirection.Door => GateDirection.Door,
        _ => throw self.InvalidEnum()
    };
}
