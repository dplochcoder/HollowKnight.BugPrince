using ItemChanger;
using ItemChanger.Extensions;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class LurienRoofColliderEditTag : SceneModifierTag
{
    protected override string GetSceneName() => SceneNames.Ruins2_Watcher_Room;

    protected override void ModifyScene(Scene scene)
    {
        var polygon = scene.FindGameObject("Roof Collider (1)")!.GetComponent<PolygonCollider2D>();
        polygon.points = polygon.points.Where(p => p.x <= 26).ToArray();
    }
}
