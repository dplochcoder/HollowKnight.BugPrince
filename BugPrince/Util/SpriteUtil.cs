using BugPrince.Data;
using BugPrince.IC;
using UnityEngine;

namespace BugPrince.Util;

internal static class SpriteUtil
{
    internal static Sprite GetSprite(string name)
    {
        EmbeddedSprite sprite = new(name);
        return sprite.Value;
    }

    internal static Sprite GetSprite(this CostType type) => GetSprite(type == CostType.Coins ? "Items.coin" : "Items.gem");
}
