using BugPrince.Scripts.InternalLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using SFCore.Utils;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class TollGateProxy : MonoBehaviour
{
    [ShimField] public string TollGateId = "";
    [ShimField] public int TollCost;

    [ShimField] public Transform? Machine;
    [ShimField] public Transform? Gate;
    [ShimField] public bool OpenRight;

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
        activated = machineFsm.GetFsmBoolVariable("Activated");
        machineFsm.GetFsmState("Activated").GetFirstActionOfType<SendEventByName>().sendEvent = OpenedEvent();
        machineFsm.RemoveFsmGlobalTransition("TOLL GATE OPENED");
        machineFsm.AddFsmGlobalTransitions(OpenedEvent(), "Toll Gate Opened");
        machineFsm.GetFsmState("Get Price").Actions = [new Lambda(() => { machineFsm.GetFsmIntVariable("Toll Cost").Value = TollCost; })];
        machineFsm.GetFsmState("Open Gates").GetFirstActionOfType<SendEventByName>().sendEvent = OpenEvent();

        var gate = Instantiate(BugPrincePreloader.Instance.TollGate!);
        gate.transform.position = Gate!.position;
        gate.transform.localScale = new(OpenRight ? -1 : 1, 1.5f, 1);
        gate.SetActive(true);

        var gateFsm = gate.LocateMyFSM("Toll Gate");
        var idle = gateFsm.GetFsmState("Idle");
        idle.RemoveTransitionsOn("TOLL GATE OPEN");
        idle.AddFsmTransition(OpenEvent(), "Open");
        idle.RemoveTransitionsOn("TOLL GATE OPENED");
        idle.AddFsmTransition(OpenedEvent(), "Destroy Self");

        Machine!.gameObject.SetActive(false);
        Gate!.gameObject.SetActive(false);
    }
}
