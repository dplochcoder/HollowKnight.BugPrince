﻿using BugPrince.Data;
using BugPrince.Util;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.Locations;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Locations;

internal class CityLampLocation : ExistingContainerLocation, IPinLocationProvider
{
    public float x;
    public float y;
    public float z;
    public FlingDirection direction;

    protected override void OnLoad() => Events.AddSceneChangeEdit(UnsafeSceneName, InstallLamp);

    protected override void OnUnload() => Events.RemoveSceneChangeEdit(UnsafeSceneName, InstallLamp);

    public override ContainerLocation AsContainerLocation() => throw new InvalidOperationException("CityLampLocation cannot be replaced");


    private static readonly FieldInfo debrisParts = typeof(Breakable).GetField("debrisParts", BindingFlags.Instance | BindingFlags.NonPublic);

    private void InstallLamp(Scene scene)
    {
        var obj = UnityEngine.Object.Instantiate(BugPrincePreloader.Instance.CityLamp!);
        obj.transform.position = new(x, y, z);

        var active = obj.FindChild("Active")!;
        var lampBug = active.FindChild("Lamp_Bug")!;
        lampBug.SetActive(false);

        if (!Placement.AllObtained())
        {
            var shiny = GameObjectUtil.MakeShinyDecorator(Placement.Items);
            shiny.transform.SetParent(active.transform);
            shiny.transform.localPosition = lampBug.transform.localPosition;
            shiny.SetActive(true);
        }

        var breakable = obj.GetComponent<Breakable>();
        var debris = (debrisParts.GetValue(breakable) as List<GameObject>)!;
        debris.RemoveAll(o => o.name.StartsWith("lamp_bug"));
        ItemChangerMod.Modules.Get<BreakablesModule>()?.DoOnBreak(breakable, () => SpawnItems(lampBug.transform));

        obj.SetActive(true);
    }

    private void SpawnItems(Transform src)
    {
        foreach (var item in Placement.Items) item.GiveOrFling(Placement, src, direction);
    }

    public PinLocation ProvidePinLocation() => new(sceneName!, x, y);
}
