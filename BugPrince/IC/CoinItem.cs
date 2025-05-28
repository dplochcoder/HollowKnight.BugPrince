using ItemChanger;
using ItemChanger.UIDefs;
using RandomizerCore.StringItems;

namespace BugPrince.IC;

public class CoinItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-Coin";
    internal const string TERM_NAME = "BUG_PRINCE_COINS";

    internal static readonly EmbeddedSprite Sprite = new("Items.coin");
    internal static readonly EmbeddedSprite LargeSprite = new("Items.coin_large");

    internal static StringItemTemplate LogicItem() => new(ITEM_NAME, $"{TERM_NAME}++");

    public CoinItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Coin"),
            shopDesc = new BoxedString("This doesn't look like it's from around here, some kind of... plated Geo? Must be worth something, right?"),
            sprite = Sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().Coins++;

    public override AbstractItem Clone() => new CoinItem();
}
