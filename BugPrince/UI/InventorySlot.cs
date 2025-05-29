using BugPrince.Util;
using ItemChanger.Extensions;
using PurenailCore.GOUtil;
using UnityEngine;

namespace BugPrince.UI;

internal class InventorySlot : MonoBehaviour
{
    internal static InventorySlot Create(GameObject parent, Vector2 pos, int value, Sprite sprite, float spriteScale, bool hiddenPin = false)
    {
        var localParent = parent.AddNewChild("InvSlot");
        localParent.transform.localPosition = pos;

        var shaker = localParent.AddNewChildShaker(UIConstants.INV_SLOT_SHAKE_SPAN);

        var textObj = shaker.gameObject.AddNewChild("Text");
        textObj.transform.localPosition = new(-UIConstants.INV_SLOT_X_SPACE / 2, 0);
        textObj.transform.localScale = new(UIConstants.INV_SLOT_TEXT_SCALE, UIConstants.INV_SLOT_TEXT_SCALE, UIConstants.INV_SLOT_TEXT_SCALE);
        var text = textObj.AddComponent<TextMesh>();
        text.fontSize = UIConstants.INV_SLOT_TEXT_SIZE;
        text.alignment = TextAlignment.Center;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = $"{value}";
        textObj.GetOrAddComponent<MeshRenderer>().SetUILayer(UISortingOrder.InventoryText);

        var spriteObj = shaker.gameObject.AddNewChild("Sprite");
        spriteObj.transform.localPosition = new(UIConstants.INV_SLOT_X_SPACE / 2, 0);
        spriteObj.transform.localScale = new(spriteScale, spriteScale, spriteScale);
        var spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.SetUILayer(UISortingOrder.InventoryIcons);

        if (hiddenPin)
        {
            var hiddenTextObj = shaker.gameObject.AddNewChild("Text");
            hiddenTextObj.transform.localPosition = new(UIConstants.INV_SLOT_TEXT_USED_X_POS, 0);
            hiddenTextObj.transform.localScale = new(UIConstants.INV_SLOT_TEXT_USED_SCALE, UIConstants.INV_SLOT_TEXT_USED_SCALE, UIConstants.INV_SLOT_TEXT_USED_SCALE);
            var hiddenText = hiddenTextObj.AddComponent<TextMesh>();
            hiddenText.fontSize = UIConstants.INV_SLOT_TEXT_USED_SIZE;
            hiddenText.alignment = TextAlignment.Center;
            hiddenText.anchor = TextAnchor.MiddleCenter;
            hiddenText.color = new(1, 0.2f, 0.2f);
            hiddenText.text = "(used)";
            hiddenTextObj.GetOrAddComponent<MeshRenderer>().SetUILayer(UISortingOrder.InventoryText);
        }

        var slot = localParent.AddComponent<InventorySlot>();
        slot.displayAmount = value;
        slot.targetAmount = value;
        slot.shaker = shaker;
        slot.text = text;
        return slot;
    }

    private Shaker? shaker;
    private TextMesh? text;
    private int displayAmount;
    private int targetAmount;
    private float ticker;

    internal void Shake() => shaker?.Shake();

    internal void FadeIn(float duration) => gameObject.Recursively(go => go.FadeColor(Color.white.WithAlpha(0), Color.white, duration));

    internal void FadeOut(float duration) => gameObject.Recursively(go => go.FadeColor(Color.white.WithAlpha(0), duration));

    internal void Give(int amount) => targetAmount += amount;
    internal void Take(int amount) => targetAmount -= amount;

    private static readonly Color lossColor = new(0.8f, 0, 0);
    private static readonly Color gainColor = new(0.8f, 0.8f, 0.8f);

    private void Update()
    {
        if (displayAmount == targetAmount) return;

        ticker += Time.deltaTime;
        if (ticker >= UIConstants.INV_SLOT_TICK_INTERVAL)
        {
            ticker -= UIConstants.INV_SLOT_TICK_INTERVAL;

            bool positive = displayAmount < targetAmount;
            if (positive) ++displayAmount;
            else --displayAmount;
            if (displayAmount == targetAmount) ticker = 0;

            var change = gameObject.AddNewChild("Change");
            change.transform.position = text!.transform.position;
            change.transform.localScale = new(UIConstants.INV_SLOT_TEXT_CHANGE_SCALE, UIConstants.INV_SLOT_TEXT_CHANGE_SCALE, UIConstants.INV_SLOT_TEXT_CHANGE_SCALE);
            var changeText = change.AddComponent<TextMesh>();
            changeText.color = positive ? lossColor : gainColor;
            changeText.fontSize = UIConstants.INV_SLOT_TEXT_SIZE;
            changeText.alignment = TextAlignment.Center;
            changeText.anchor = TextAnchor.MiddleCenter;
            changeText.text = positive ? "+1" : "-1";
            change.GetOrAddComponent<MeshRenderer>().SetUILayer(UISortingOrder.InventoryLossText);
            change.FadeColor(changeText.color.WithAlpha(0), UIConstants.INV_SLOT_TICK_DURATION);
            change.AddComponent<Mover>().SetVelocity(new(0, UIConstants.INV_SLOT_TICK_SPEED));

            text.text = $"{displayAmount}";
        }
    }
}
