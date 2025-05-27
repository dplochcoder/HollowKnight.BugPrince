using GlobalEnums;
using UnityEngine;

namespace BugPrince.UI;

internal static class UISortingOrder
{
    internal const int SceneText = 1;
    internal const int ScenePicture = 2;
    internal const int SceneFrame = 3;
    internal const int CostIcons = 3;
    internal const int SelectionCorners = 4;
    internal const int InventoryIcons = 5;
    internal const int InventoryText = 5;

    internal static void SetUILayer(this Renderer self, int order)
    {
        self.gameObject.layer = (int)PhysLayers.UI;
        self.sortingLayerName = "HUD";
        self.sortingOrder = order + 11;  // Always in front of blanker.
    }
}
