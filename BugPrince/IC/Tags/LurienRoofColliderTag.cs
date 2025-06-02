using ItemChanger;
using ItemChanger.Extensions;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugPrince.IC.Tags;

internal class LurienRoofColliderTag : SceneModifierTag
{
    protected override string GetSceneName() => SceneNames.Ruins2_Watcher_Room;

    protected override void ModifyScene(Scene scene)
    {
        var polygon = scene.FindGameObject("Roof Collider")!.GetComponent<PolygonCollider2D>();
        polygon.points = polygon.points.Select(p =>
        {
            if (p.x > 18 && p.y > 10) return p with { x = 13 };
            else return p;
        }).ToArray();

        polygon = scene.FindGameObject("Roof Collider (1)")!.GetComponent<PolygonCollider2D>();
        polygon.points = polygon.points.Where(p => p.x <= 25).ToArray();
    }
}
