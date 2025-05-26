using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class CoinItem : AbstractItem
{
    internal static readonly EmbeddedSprite sprite = new("Items.coin");

    public CoinItem()
    {
        name = "BugPrince-Coin";
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Coin"),
            shopDesc = new BoxedString("This doesn't look like it's from around here, some kind of... plated Geo? Must be worth something, right?"),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info)
    {
        var module = BugPrinceModule.Get();
        module.Coins++;
        module.TotalCoins++;
    }

    public override AbstractItem Clone() => new CoinItem();
}
