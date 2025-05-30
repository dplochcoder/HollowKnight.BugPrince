using ItemChanger;
using ItemChanger.UIDefs;
using RandomizerCore.LogicItems;
using RandomizerMod.RandomizerData;

namespace BugPrince.IC.Items;

public class PushPinItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-PushPin";

    internal static readonly EmbeddedSprite Sprite = new("Items.push_pin");
    internal static readonly EmbeddedSprite LargeSprite = new("Items.push_pin_large");

    internal static EmptyItem LogicItem() => new(ITEM_NAME);

    internal static ItemDef ItemDef() => new()
    {
        Name = ITEM_NAME,
        Pool = PoolNames.Relic,
        MajorItem = false,
        PriceCap = 1250,
    };

    public PushPinItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Push Pin"),
            shopDesc = new BoxedString("I could really use a dozen more of these to organize all the clutter."),
            sprite = Sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().PushPins++;

    public override AbstractItem Clone() => new PushPinItem();
}
