using ItemChanger;
using ItemChanger.UIDefs;

namespace BugPrince.IC;

public class DiceTotemItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-DiceTotem";

    internal static readonly EmbeddedSprite sprite = new("Items.dice_totem");

    public DiceTotemItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Dice Totem"),
            shopDesc = new BoxedString("A monument to chance, to persistence! Just one more, surely."),
            sprite = sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().DiceTotems++;

    public override AbstractItem Clone() => new DiceTotemItem();
}
