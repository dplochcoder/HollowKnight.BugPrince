using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class PushPinItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-PushPin";

    internal static readonly EmbeddedSprite sprite = new("Items.push_pin");

    public PushPinItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Push Pin"),
            shopDesc = new BoxedString("I could really use a dozen more of these to organize all the clutter."),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().PushPins++;

    public override AbstractItem Clone() => new PushPinItem();
}
