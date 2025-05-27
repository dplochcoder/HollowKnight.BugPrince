using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class CoinItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-Coin";
    internal const string TERM_NAME = "BUG_PRINCE_COINS";

    internal static readonly EmbeddedSprite sprite = new("Items.coin");

    public CoinItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Coin"),
            shopDesc = new BoxedString("This doesn't look like it's from around here, some kind of... plated Geo? Must be worth something, right?"),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().Coins++;

    public override AbstractItem Clone() => new CoinItem();
}
