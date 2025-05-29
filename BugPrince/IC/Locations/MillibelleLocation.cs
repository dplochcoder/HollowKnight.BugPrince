using BugPrince.Util;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using System.Collections.Generic;

namespace BugPrince.IC.Locations;

internal class MillibelleLocation : ExistingContainerLocation
{
    private const string SCENE_NAME = SceneNames.Ruins_Bathhouse;
    private static readonly FsmID FSM_ID = new("Banker Spa NPC", "tink_effect");

    internal MillibelleLocation()
    {
        name = "Millibelle-Pleasure_House";
        sceneName = SCENE_NAME;
        nonreplaceable = true;
    }

    protected override void OnLoad() => Events.AddFsmEdit(SCENE_NAME, FSM_ID, ModifyMillibelle);

    protected override void OnUnload() => Events.RemoveFsmEdit(SCENE_NAME, FSM_ID, ModifyMillibelle);

    private const int MIN_HITS = 2;
    private const int MAX_HITS = 4;

    private void ModifyMillibelle(PlayMakerFSM fsm)
    {
        List<(string, FlingDirection)> dirs = [("Right", FlingDirection.Right), ("Left", FlingDirection.Left), ("Up", FlingDirection.Any), ("Down", FlingDirection.Down)];
        Wrapped<int> numHits = new(UnityEngine.Random.Range(MIN_HITS, MAX_HITS + 1));
        foreach (var (name, dir) in dirs)
        {
            var dirCopy = dir;
            fsm.GetState($"Blocked {name}").AddLastAction(new Lambda(() =>
            {
                if (--numHits.Value > 0) return;

                foreach (var item in Placement.Items)
                {
                    if (!item.GiveOrFling(Placement, fsm.gameObject.transform, dirCopy)) continue;

                    numHits.Value = UnityEngine.Random.Range(MIN_HITS, MAX_HITS + 1);
                    break;
                }
            }));
        }
    }
}
