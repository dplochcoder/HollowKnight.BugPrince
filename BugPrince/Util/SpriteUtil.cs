using BugPrince.Data;
using BugPrince.IC.Items;
using UnityEngine;

namespace BugPrince.Util;

internal static class SpriteUtil
{
    internal static Sprite GetSprite(this CostType type) => type switch { CostType.Coins => CoinItem.LargeSprite.Value, CostType.Gems => GemItem.LargeSprite.Value };
}
