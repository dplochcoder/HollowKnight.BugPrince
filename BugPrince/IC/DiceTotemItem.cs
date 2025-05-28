using ItemChanger;
using ItemChanger.UIDefs;
using RandomizerCore.LogicItems;
using RandomizerMod.RandomizerData;

namespace BugPrince.IC;

public class DiceTotemItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-DiceTotem";

    internal static readonly EmbeddedSprite Sprite = new("Items.dice_totem");
    internal static readonly EmbeddedSprite LargeSprite = new("Items.dice_totem_large");

    internal static EmptyItem LogicItem() => new(ITEM_NAME);

    internal static ItemDef ItemDef() => new()
    {
        Name = ITEM_NAME,
        Pool = PoolNames.Relic,
        MajorItem = false,
        PriceCap = 800,
    };

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
