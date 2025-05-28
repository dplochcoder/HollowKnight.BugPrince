using GlobalEnums;
using UnityEngine;

namespace BugPrince.UI;

internal static class UISortingOrder
{
    internal const int Background = 1;
    internal const int SceneText = 2;
    internal const int ScenePicture = 3;
    internal const int SceneFrame = 4;
    internal const int CostIcons = 4;
    internal const int SelectionCorners = 5;
    internal const int InventoryLossText = 6;
    internal const int InventoryIcons = 7;
    internal const int InventoryText = 7;

    internal static void SetUILayer(this Renderer self, int order)
    {
        self.gameObject.layer = (int)PhysLayers.UI;
        self.sortingLayerName = "HUD";
        self.sortingOrder = order + 11;  // Always in front of blanker.
    }
}
