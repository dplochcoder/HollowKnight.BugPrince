using ItemChanger.Internal;

namespace BugPrince.IC;

public class EmbeddedSprite : ItemChanger.EmbeddedSprite
{
    private static readonly SpriteManager manager = new(typeof(EmbeddedSprite).Assembly, "BugPrince.Resources.Sprites.", new EmbeddedSpriteInfo());

    public EmbeddedSprite(string key) => this.key = key;

    public override SpriteManager SpriteManager => manager;
}

internal class EmbeddedSpriteInfo : SpriteManager.Info
{
    internal EmbeddedSpriteInfo() => overridePPUs = new() { ["Game.cracked_window"] = 27.6f };

    public override float GetPixelsPerUnit(string name)
    {
        if (name.StartsWith("Game.") && (overridePPUs == null || !overridePPUs.ContainsKey(name))) return 64f;
        return base.GetPixelsPerUnit(name);
    }
}
