using ItemChanger;
using ItemChanger.UIDefs;
using RandomizerCore.LogicItems;

namespace BugPrince.IC;

public class DiceTotemItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-DiceTotem";

    internal static readonly EmbeddedSprite Sprite = new("Items.dice_totem");
    internal static readonly EmbeddedSprite LargeSprite = new("Items.dice_totem_large");

    internal static EmptyItem LogicItem() => new(ITEM_NAME);

    public DiceTotemItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Dice Totem"),
            shopDesc = new BoxedString("A monument to chance, to persistence! Just one more, surely."),
            sprite = Sprite,
        };
    }

    public override void GiveImmediate(GiveInfo info) => BugPrinceModule.Get().DiceTotems++;

    public override AbstractItem Clone() => new DiceTotemItem();
}
