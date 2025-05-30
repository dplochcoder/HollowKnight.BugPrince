using BugPrince.Util;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Locations;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Locations;

internal class CityLampLocation : ExistingContainerLocation
{
    public float x;
    public float y;
    public float z;
    public FlingDirection direction;

    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, InstallLamp);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, InstallLamp);

    private static FieldInfo debrisParts = typeof(Breakable).GetField("debrisParts", BindingFlags.Instance | BindingFlags.NonPublic);

    private void InstallLamp(Scene scene)
    {
        var obj = Object.Instantiate(BugPrincePreloader.Instance.CityLamp!);
        obj.transform.position = new(x, y, z);

        var active = obj.FindChild("Active")!;
        active.FindChild("Lamp_Bug")!.SetActive(false);

        if (!Placement.AllObtained())
        {
            var shiny = GameObjectUtil.MakeShinyDecorator(Placement.Items);
            shiny.transform.SetParent(active.transform);
            shiny.transform.localPosition = new(0, 0, -0.4f);
            shiny.SetActive(true);
        }

        var breakable = obj.GetComponent<Breakable>();
        var debris = (debrisParts.GetValue(breakable) as List<GameObject>)!;
        debris.RemoveAll(o => o.name.StartsWith("lamp_bug"));
        ItemChangerMod.Modules.Get<BreakablesModule>()?.DoOnBreak(breakable, () => SpawnItems(obj));

        obj.SetActive(true);
    }

    private void SpawnItems(GameObject src)
    {
        foreach (var item in Placement.Items) item.GiveOrFling(Placement, src.transform, direction);
    }
}
