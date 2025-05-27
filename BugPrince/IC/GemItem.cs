using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class GemItem : AbstractItem
{
    internal static readonly EmbeddedSprite sprite = new("Items.gem");

    internal const string TERM_NAME = "BUG_PRINCE_COINS";

    public GemItem()
    {
        name = "BugPrince-Gem";
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Gem"),
            shopDesc = new BoxedString("Sparkling, bright, beautifully cut... I should charge at least five times more for this."),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info)
    {
        var module = BugPrinceModule.Get();
        module.Gems++;
        module.TotalGems++;
    }

    public override AbstractItem Clone() => new GemItem();
}
