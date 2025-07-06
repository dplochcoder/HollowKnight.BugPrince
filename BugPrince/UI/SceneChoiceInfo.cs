using BugPrince.Data;
using BugPrince.IC;
using ItemChanger;
using System.Collections.Generic;
using UnityEngine;

namespace BugPrince.UI;

internal record SceneChoiceInfo
{
    public Transition OrigSrc { get; }
    public Transition Target { get; }
    public (CostType, int)? Cost { get; }
    public bool Pinned { get; }

    public SceneChoiceInfo(Transition OrigSrc, Transition Target, (CostType, int)? Cost, bool Pinned)
    {
        this.OrigSrc = OrigSrc;
        this.Target = Target;
        this.Cost = Cost;
        this.Pinned = Pinned;
    }

    public bool CanAfford(TransitionSelectionModule module)
    {
        if (!Cost.HasValue) return true;

        var (costType, cost) = Cost.Value;
        return costType switch
        {
            CostType.Coins => module.GetCoins() >= cost,
            CostType.Gems => module.GetGems() >= cost,
            _ => throw new System.ArgumentException($"Unknown cost type: {costType}")
        };
    }

    private static readonly Dictionary<string, ISprite> extraSceneSprites = [];
    internal static void AddSceneSprite(string sceneName, ISprite sprite) => extraSceneSprites[sceneName] = sprite;

    public Sprite GetSceneSprite()
    {
        if (extraSceneSprites.TryGetValue(Target.SceneName, out var iSprite)) return iSprite.Value;

        IC.EmbeddedSprite sprite = new($"Scenes.{Target.SceneName}");
        return sprite.Value;
    }
}
