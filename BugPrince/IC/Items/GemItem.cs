﻿using ItemChanger;
using ItemChanger.UIDefs;
using RandomizerCore.StringItems;
using RandomizerMod.RandomizerData;

namespace BugPrince.IC.Items;

public class GemItem : AbstractItem
{
    internal const string ITEM_NAME = "BugPrince-Gem";
    internal const string TERM_NAME = "BUG_PRINCE_GEMS";

    internal static readonly EmbeddedSprite Sprite = new("Items.gem");
    internal static readonly EmbeddedSprite LargeSprite = new("Items.gem_large");

    internal static StringItemTemplate LogicItem() => new(ITEM_NAME, $"{TERM_NAME}++");

    internal static ItemDef ItemDef() => new()
    {
        Name = ITEM_NAME,
        Pool = PoolNames.Key,
        MajorItem = true,
        PriceCap = 400,
    };

    public GemItem()
    {
        name = ITEM_NAME;
        UIDef = new MsgUIDef()
        {
            name = new BoxedString("Gem"),
            shopDesc = new BoxedString("Sparkling, bright, beautifully cut... I should charge at least five times more for this."),
            sprite = Sprite,
        };
        this.AddInteropData("Keys");
    }

    public override void GiveImmediate(GiveInfo info) => TransitionSelectionModule.Get()!.TotalGems++;

    public override AbstractItem Clone() => new GemItem();
}
