using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using UnityEngine;

namespace BugPrince.IC.Locations;

internal class BankStandLocation : ExistingContainerLocation
{
    private const string SCENE_NAME = SceneNames.Fungus3_35;
    private static readonly FsmID FSM_ID = new("Bank Stand", "Stand Control");

    protected override void OnLoad() => Events.AddFsmEdit(SCENE_NAME, FSM_ID, ModifyStandControl);

    protected override void OnUnload() => Events.RemoveFsmEdit(SCENE_NAME, FSM_ID, ModifyStandControl);

    private void ModifyStandControl(PlayMakerFSM fsm)
    {
        fsm.GetState("Land").AddLastAction(new Lambda(() =>
        {
            GameObject flingSrc = new("FlingSrc");
            flingSrc.transform.position = new(18, 6.5f, 0);

            GiveInfo info = new()
            {
                Container = Container.Chest,
                FlingType = FlingType.Everywhere,
                Transform = flingSrc.transform,
                MessageType = MessageType.Corner
            };

            foreach (var item in Placement.Items) item.GiveOrFling(Placement, flingSrc.transform);
        }));
    }
}