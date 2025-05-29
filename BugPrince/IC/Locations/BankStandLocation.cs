using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using UnityEngine;

namespace BugPrince.IC.Locations;

internal class BankStandLocation : DualLocation
{
    internal const string NAME = "Millibelle's_Bank_Stand-Fog_Canyon";
    internal const string SCENE_NAME = SceneNames.Fungus3_35;

    internal BankStandLocation()
    {
        name = NAME;
        sceneName = SCENE_NAME;
        Test = new PDIntBool(nameof(PlayerData.bankerTheft), 2, ComparisonOperator.Eq);
        falseLocation = new UprightBankStandLocation();
        trueLocation = new CoordinateLocation()
        {
            name = NAME,
            sceneName = SCENE_NAME,
            x = 18.0f,
            y = 6.5f
        };
    }
}

internal class UprightBankStandLocation : ExistingContainerLocation
{
    private static readonly FsmID FSM_ID = new("Bank Stand", "Stand Control");

    internal UprightBankStandLocation()
    {
        name = BankStandLocation.NAME;
        sceneName = BankStandLocation.SCENE_NAME;
        nonreplaceable = true;
    }

    protected override void OnLoad() => Events.AddFsmEdit(BankStandLocation.SCENE_NAME, FSM_ID, ModifyStandControl);

    protected override void OnUnload() => Events.RemoveFsmEdit(BankStandLocation.SCENE_NAME, FSM_ID, ModifyStandControl);

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