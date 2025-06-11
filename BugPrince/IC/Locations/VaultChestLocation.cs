using BugPrince.Scripts.Proxy;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Locations;
using ItemChanger.Placements;
using UnityEngine;

namespace BugPrince.IC.Locations;

internal class VaultChestLocation : CoordinateLocation
{
    public string TollGateProxyPath = "";

    public override AbstractPlacement Wrap()
    {
        var placement = base.Wrap();
        if (placement is MutablePlacement mutable) mutable.containerType = Container.Chest;
        return placement;
    }

    public override void PlaceContainer(GameObject obj, string containerType)
    {
        base.PlaceContainer(obj, containerType);

        if (containerType != Container.Chest) return;

        var fsm = obj.LocateMyFSM("Chest Control");
        fsm.GetState("Range?").AddFirstAction(new Lambda(() =>
        {
            var tollGate = GameObject.Find(TollGateProxyPath);
            if (tollGate?.GetComponent<TollGateProxy>()?.IsOpened ?? false) fsm.SendEvent("FINISHED");
        }));
    }
}
