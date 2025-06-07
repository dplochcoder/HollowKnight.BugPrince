using BugPrince.Util;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using System;
using System.Collections.Generic;

namespace BugPrince.IC.Locations;

internal class MillibelleLocation : ExistingContainerLocation
{
    private static readonly FsmID FSM_ID = new("Banker Spa NPC", "tink_effect");

    protected override void OnLoad() => Events.AddFsmEdit(UnsafeSceneName, FSM_ID, ModifyMillibelle);

    protected override void OnUnload() => Events.RemoveFsmEdit(UnsafeSceneName, FSM_ID, ModifyMillibelle);

    public override ContainerLocation AsContainerLocation() => throw new InvalidOperationException("MillibelleLocation cannot be replaced");

    private const int NUM_HITS = 3;

    private void ModifyMillibelle(PlayMakerFSM fsm)
    {
        List<(string, FlingDirection)> dirs = [("Right", FlingDirection.Right), ("Left", FlingDirection.Left), ("Up", FlingDirection.Any), ("Down", FlingDirection.Down)];
        Wrapped<int> numHits = new(NUM_HITS);
        foreach (var (name, dir) in dirs)
        {
            var dirCopy = dir;
            fsm.GetState($"Blocked {name}").AddLastAction(new Lambda(() =>
            {
                if (--numHits.Value > 0) return;

                foreach (var item in Placement.Items)
                {
                    if (!item.GiveOrFling(Placement, fsm.gameObject.transform, dirCopy)) continue;

                    numHits.Value = NUM_HITS;
                    break;
                }
            }));
        }
    }
}
