using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class GemItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-Gem";
    internal const string TERM_NAME = "BUG_PRINCE_GEMS";

    internal static readonly EmbeddedSprite sprite = new("Items.gem");

    public GemItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Gem"),
            shopDesc = new BoxedString("Sparkling, bright, beautifully cut... I should charge at least five times more for this."),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().Gems++;

    public override AbstractItem Clone() => new GemItem();
}
