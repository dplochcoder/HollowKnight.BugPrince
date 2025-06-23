using BugPrince.Data;
using BugPrince.IC;
using ItemChanger;
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
            CostType.Coins => module.Coins >= cost,
            CostType.Gems => module.Gems >= cost,
            _ => throw new System.ArgumentException($"Unknown cost type: {costType}")
        };
    }

    public Sprite GetSceneSprite()
    {
        IC.EmbeddedSprite sprite = new($"Scenes.{Target.SceneName}");
        return sprite.Value;
    }
}
