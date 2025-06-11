using SFCore.Utils;
using System.Linq;
using UnityEngine;

namespace BugPrince.Scripts.Proxy;

internal static class AreaTitleControllerProxy
{
    internal static void ShowAreaTitle(string areaName, int areaId, string pdVisitedBool)
    {
        var obj = Object.Instantiate(BugPrincePreloader.Instance.AreaTitleController!);
        var fsm = obj.LocateMyFSM("Area Title Controller");

        var vars = fsm.FsmVariables;
        vars.GetFsmString("Area Event").Value = areaName;
        vars.GetFsmBool("Display Right").Value = true;
        vars.GetFsmBool("Sub Area").Value = true;
        vars.GetFsmFloat("Unvisited Pause").Value = 2;
        vars.GetFsmFloat("Visited Pause").Value = 1;
        vars.GetFsmGameObject("Area Title").Value = GameObject.Find("Area Title");

        // Define private Area object
        var areaType = typeof(AreaTitleController).GetNestedType("Area", System.Reflection.BindingFlags.NonPublic);
        var con = areaType.GetConstructor([typeof(string), typeof(int), typeof(bool), typeof(string)]);
        var areaObj = con.Invoke([areaName, areaId, false, pdVisitedBool]);

        // Add new areas
        var atc = obj.GetComponent<AreaTitleController>();
        var atcList = atc.GetAttr<AreaTitleController, object>("areaList");
        var addMethod = atcList.GetType().GetMethods().Where(mi => mi.Name == "Add" && mi.GetParameters().Length == 1).FirstOrDefault();
        addMethod.Invoke(atcList, [areaObj]);

        obj.SetActive(true);
    }
}
