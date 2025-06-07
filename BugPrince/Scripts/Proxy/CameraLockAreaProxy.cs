using BugPrince.Scripts.InternalLib;
using BugPrince.Util;
using GlobalEnums;
using ItemChanger.Extensions;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BugPrince.Scripts.Proxy;

[Shim]
internal class CameraLockAreaProxy : MonoBehaviour
{
    [ShimField] public bool preventLookUp;
    [ShimField] public bool preventLookDown;
    [ShimField] public bool maxPriority;

    private void Start()
    {
        var tilemap = GameObject.Find("TilemapGrid")!.FindChild("Tilemap")!.GetComponent<Tilemap>();
        var lockBox = GetComponent<BoxCollider2D>().bounds;
        var xMid = (lockBox.min.x + lockBox.max.x) / 2;
        var yMid = (lockBox.min.y + lockBox.max.y) / 2;
        var xHalf = GameConstants.CAMERA_HALF_WIDTH;
        var yHalf = GameConstants.CAMERA_HALF_HEIGHT;
        var cameraXMin = Mathf.Max(Mathf.Min(lockBox.min.x + xHalf, xMid), xHalf);
        var cameraYMin = Mathf.Max(Mathf.Min(lockBox.min.y + yHalf, yMid), yHalf);
        var cameraXMax = Mathf.Min(Mathf.Max(lockBox.max.x - xHalf, xMid), tilemap.size.x - xHalf);
        var cameraYMax = Mathf.Min(Mathf.Max(lockBox.max.y - yHalf, yMid), tilemap.size.y - yHalf);

        int i = 0;
        var parentName = transform.parent.gameObject.name;
        var siblings = transform.parent.gameObject.Children().ToList();
        foreach (var go in siblings)
        {
            if (go == gameObject) continue;

            GameObject triggerObj = new($"{parentName}.Trigger{++i}");
            triggerObj.SetActive(false);
            triggerObj.layer = (int)PhysLayers.HERO_DETECTOR;

            var area = triggerObj.AddComponent<CameraLockArea>();
            area.preventLookUp = preventLookUp;
            area.preventLookDown = preventLookDown;
            area.maxPriority = maxPriority;
            area.cameraXMin = cameraXMin;
            area.cameraYMin = cameraYMin;
            area.cameraXMax = cameraXMax;
            area.cameraYMax = cameraYMax;

            var b2d = go.GetComponent<BoxCollider2D>();
            var newB2d = triggerObj.AddComponent<BoxCollider2D>();
            triggerObj.transform.position = go.transform.position;
            newB2d.isTrigger = true;
            newB2d.offset = b2d.offset;
            newB2d.size = b2d.size;

            Destroy(go);

            triggerObj.SetActive(true);
        }

        Destroy(gameObject);
    }
}