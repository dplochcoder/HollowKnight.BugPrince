using BugPrince.Scripts.InternalLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class TollGateProxy : MonoBehaviour
{
    [ShimField]
    public string TollGateId = "";

    [ShimField]
    public int TollCost;

    [ShimField]
    public Transform? Machine;

    [ShimField]
    public Transform? Gate;

    [ShimField]
    public bool OpenRight;

    private string OpenEvent() => $"TOLL GATE {TollGateId} OPEN";

    private string OpenedEvent() => $"TOLL GATE {TollGateId} OPENED";

    private FsmBool? activated;
    internal bool IsOpened => activated?.Value ?? false;

    private void Awake()
    {
        var machine = Instantiate(BugPrincePreloader.Instance.TollGateMachine!);
        machine.transform.position = Machine!.position;
        var pbi = machine.GetComponent<PersistentBoolItem>();
        pbi.persistentBoolData.sceneName = gameObject.scene.name;
        pbi.persistentBoolData.id = TollGateId;

        machine.SetActive(true);
        Destroy(machine.LocateMyFSM("Disable if No Lantern"));

        var machineFsm = machine.LocateMyFSM("Toll Machine");
        activated = machineFsm.FsmVariables.GetFsmBool("Activated");
        machineFsm.GetState("Activated").GetFirstActionOfType<SendEventByName>().sendEvent =
            OpenedEvent();
        SFCore.Utils.FsmUtil.RemoveGlobalTransition(machineFsm, "TOLL GATE OPENED");
        SFCore.Utils.FsmUtil.AddGlobalTransition(machineFsm, OpenedEvent(), "Toll Gate Opened");
        machineFsm.GetState("Get Price").Actions =
        [
            new Lambda(() =>
            {
                machineFsm.FsmVariables.GetFsmInt("Toll Cost").Value = TollCost;
            }),
        ];
        machineFsm.GetState("Open Gates").GetFirstActionOfType<SendEventByName>().sendEvent =
            OpenEvent();

        var gate = Instantiate(BugPrincePreloader.Instance.TollGate!);
        gate.transform.position = Gate!.position;
        gate.transform.localScale = new(OpenRight ? -1 : 1, 1.5f, 1);
        gate.SetActive(true);

        var gateFsm = gate.LocateMyFSM("Toll Gate");
        var idleState = gateFsm.GetState("Idle");
        idleState.RemoveTransitionsOn("TOLL GATE OPEN");
        idleState.AddTransition(OpenEvent(), "Open");
        idleState.RemoveTransitionsOn("TOLL GATE OPENED");
        idleState.AddTransition(OpenedEvent(), "Destroy Self");

        Machine!.gameObject.SetActive(false);
        Gate!.gameObject.SetActive(false);
    }
}
