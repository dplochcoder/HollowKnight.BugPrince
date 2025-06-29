using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using BugPrince.ItemSyncInterop;
using BugPrince.Util;
using PurenailCore.GOUtil;
using PurenailCore.SystemUtil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugPrince.UI;

internal record RoomSelectionDecision
{
    public SceneChoiceInfo? chosen;  // Can be null if no choices affordable.
    public SceneChoiceInfo? newPin;  // Set if pin used.
}

internal class RoomSelectionUI : MonoBehaviour
{
    internal delegate void SendSelectionCb(RoomSelectionDecision decision, Action<SwapTransitionsResponse> callback);
    internal delegate void FinalizeSelectionCb(SwapTransitionsResponse response);
    internal delegate bool RerollCb(out List<SceneChoiceInfo> reroll);
    internal delegate void FailedRerollCb();

    private static readonly EmbeddedSprite fullwhite = new("UI.fullwhite");
    private static readonly EmbeddedSprite dash_ico = new("UI.dash_ico");
    private static readonly EmbeddedSprite spell_ico = new("UI.spell_ico");

    internal static bool uiPresent = false;

    private TransitionSelectionModule? module;
    private List<SceneChoiceInfo> choiceInfos = [];
    private bool _canSelect = true;
    private bool hiddenPin;
    private SendSelectionCb? sendSelectionCb;
    private FinalizeSelectionCb? finalizeSelectionCb;
    private RerollCb? rerollCb;
    private FailedRerollCb? failedRerollCb;

    private RoomSelectionLayout? layout;

    internal static RoomSelectionUI Create(TransitionSelectionModule module, GateDirection gateDir, List<SceneChoiceInfo> infos, SendSelectionCb sendSelectionCb, FinalizeSelectionCb finalizeSelectionCb, RerollCb rerollCb, FailedRerollCb failedRerollCb)
    {
        GameObject obj = new("Room Selection");
        obj.SetActive(false);
        obj.transform.SetParent(GameObject.Find("_GameCameras/HudCamera").transform);
        obj.transform.position = Vector3.zero;

        var ui = obj.AddComponent<RoomSelectionUI>();
        ui.Init(module, gateDir, infos, sendSelectionCb, finalizeSelectionCb, rerollCb, failedRerollCb);

        obj.SetActive(true);
        uiPresent = true;
        return ui;
    }

    private void OnDestroy() => uiPresent = false;

    private void Init(TransitionSelectionModule module, GateDirection gateDir, List<SceneChoiceInfo> infos, SendSelectionCb sendSelectionCb, FinalizeSelectionCb finalizeSelectionCb, RerollCb rerollCb, FailedRerollCb failedRerollCb)
    {
        this.module = module;
        choiceInfos = [.. infos];
        _canSelect = choiceInfos.Any(i => i.CanAfford(module));
        hiddenPin = module.PinnedScene != null && !choiceInfos.Any(i => i.Pinned);
        this.sendSelectionCb = sendSelectionCb;
        this.finalizeSelectionCb = finalizeSelectionCb;
        this.rerollCb = rerollCb;
        this.failedRerollCb = failedRerollCb;

        layout = new(infos.Count);
        gameObject.AddComponent<PlayerLocker>().SetDirection(gateDir);

        audioSource = gameObject.AddComponent<AudioSource>();

        backgroundFade = gameObject.AddNewChild("BGFade");
        backgroundFade.transform.localScale = new(50, 50, 50);
        backgroundFade.SetActive(false);
        var bgRender = backgroundFade.AddComponent<SpriteRenderer>();
        bgRender.sprite = fullwhite.Value;
        bgRender.SetUILayer(UISortingOrder.Background);
        backgroundFade.FadeColor(Color.black.WithAlpha(0), Color.black.WithAlpha(0.5f), 1.5f);
        backgroundFade.SetActive(true);

        var redOutObj = gameObject.AddNewChild("RedOut");
        redOutObj.transform.localScale = new(50, 50, 50);
        redOutObj.SetActive(false);
        redOutRenderer = redOutObj.AddComponent<SpriteRenderer>();
        redOutRenderer.sprite = fullwhite.Value;
        redOutRenderer.SetUILayer(UISortingOrder.RedOut);
        redOutRenderer.color = Color.red.WithAlpha(0);
        redOutObj.SetActive(true);

        float invY = UIConstants.Y_MAIN - UIConstants.SCENE_Y_SPACE / 2 - UIConstants.INV_SLOT_BAR_Y_SPACE;
        Vector2 InvPos(int idx) => new((idx * 2 - 3) * UIConstants.INV_SLOT_BAR_X_SPACE / 2, invY);

        diceTotemSlot = InventorySlot.Create(gameObject, InvPos(0), () => module!.DiceTotems, DiceTotemItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE);
        coinSlot = InventorySlot.Create(gameObject, InvPos(1), () => module!.GetCoins(tempCostScene), CoinItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE);
        gemSlot = InventorySlot.Create(gameObject, InvPos(2), () => module!.GetGems(tempCostScene), GemItem.LargeSprite.Value, UIConstants.INV_SLOT_GEM_ITEM_SCALE);
        pushPinSlot = InventorySlot.Create(gameObject, InvPos(3), () => module!.GetPushPins(tempPinReceipt), PushPinItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE, hiddenPin);
        dashIco = MakeSprite(gameObject, InvPos(0) - new Vector2(0, 1.75f), dash_ico.Value);
        spellIco = MakeSprite(gameObject, InvPos(3) - new Vector2(0, 1.75f), spell_ico.Value);

        diceTotemSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
        coinSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
        gemSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
        pushPinSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
        dashIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
        spellIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
    }

    private void Awake() => StartCoroutine(LoadChoiceInfos());

    private readonly List<SceneChoice> choiceObjects = [];

    private static GameObject MakeSprite(GameObject parent, Vector2 pos, Sprite sprite)
    {
        var obj = parent.AddNewChild("Sprite");
        obj.transform.localPosition = pos;
        obj.transform.localScale = new(0.55f, 0.55f, 0.55f);

        var render = obj.AddComponent<SpriteRenderer>();
        render.sprite = sprite;
        render.SetUILayer(UISortingOrder.InventoryIcons);

        return obj;
    }

    private IEnumerator LoadChoiceInfos()
    {
        for (int i = 0; i < choiceInfos.Count; i++)
        {
            var choice = SceneChoice.Create(module!, gameObject, choiceInfos[i], layout![i].Pos);
            choiceObjects.Add(choice);
            if (i + 1 < choiceInfos.Count) yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_SCENE_STAGGER);
        }

        if (choiceObjects.Count > 0) yield return new WaitUntil(() => choiceObjects[choiceObjects.Count - 1].IsReady());
        else yield return new WaitForSeconds(UIConstants.SCENE_ASCEND_TIME);

        selection = choiceObjects.Count > 0 ? 0 : -1;
        newPinSelection = null;
        selectionCorners = SelectionCorners.Create(gameObject, choiceObjects.Count > 0 ? layout![0].Pos : new Vector2(0, UIConstants.Y_MAIN));
        acceptingInput = true;
    }

    private bool rerolled = false;

    private bool UsedPinShown => choiceInfos.Count > 0 && choiceInfos[choiceInfos.Count - 1].Pinned;
    private bool PinIsUsed => hiddenPin || UsedPinShown;
    private void ShakePinIndicators()
    {
        if (UsedPinShown) choiceObjects[choiceObjects.Count - 1].ShakePin();
        if (newPinSelection.HasValue) choiceObjects[newPinSelection.Value].ShakePin();
        pushPinSlot?.Shake();
    }

    private int selection = 0;
    private int? newPinSelection;
    private GameObject? backgroundFade;
    private SelectionCorners? selectionCorners;
    private AudioSource? audioSource;
    private InventorySlot? diceTotemSlot;
    private InventorySlot? coinSlot;
    private InventorySlot? gemSlot;
    private string? tempCostScene;
    private InventorySlot? pushPinSlot;
    private PinReceipt? tempPinReceipt;
    private GameObject? dashIco;
    private GameObject? spellIco;
    private bool acceptingInput = false;

    private void TryMoveToIndex(int? target)
    {
        if (target.HasValue)
        {
            selection = target.Value;
            selectionCorners!.UpdateTarget(layout![target.Value].Pos);
            audioSource!.PlayOneShot(SoundCache.ChangeSelection);
        }
        else
        {
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            selectionCorners?.Shake();
        }
    }

    private InventorySlot? GetInvSlot(SceneChoiceInfo info)
    {
        if (!info.Cost.HasValue) return null;

        return info.Cost.Value.Item1 switch
        {
            CostType.Coins => coinSlot,
            CostType.Gems => gemSlot,
            _ => throw info.Cost.Value.Item1.InvalidEnum()
        };
    }

    private void FadeOutAll()
    {
        selectionCorners?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
        dashIco?.FadeColor(Color.white.WithAlpha(0), UIConstants.SCENE_FADE_OUT_DURATION);
        diceTotemSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
        coinSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
        gemSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
        pushPinSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
        spellIco?.FadeColor(Color.white.WithAlpha(0), UIConstants.SCENE_FADE_OUT_DURATION);
    }

    private void TrySelectIndex(int selection)
    {
        if (selection == -1)
        {
            selectionCorners?.Shake();
            return;
        }

        var info = choiceInfos[selection];
        if (newPinSelection.HasValue && PinIsUsed && selection != newPinSelection.Value && !info.Pinned)
        {
            // Can't make 2 pins.
            ShakePinIndicators();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            return;
        }

        if (!info.CanAfford(module!))
        {
            choiceObjects[selection].ShakeCosts();
            GetInvSlot(info)?.Shake();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            return;
        }

        acceptingInput = false;
        if (newPinSelection == selection)
        {
            // Return the pin if immediately selected.
            choiceObjects[selection].SetPinned(false);
            newPinSelection = null;
            tempPinReceipt = null;
        }

        IEnumerator LockIn()
        {
            Wrapped<SwapTransitionsResponse?> response = new(null);
            sendSelectionCb!(new()
            {
                newPin = newPinSelection.HasValue ? choiceInfos[newPinSelection.Value] : null,
                chosen = info,
            }, resp => response.Value = resp);

            audioSource!.PlayOneShot(SoundCache.Confirm);

            if (info.Cost.HasValue)
            {
                audioSource.PlayOneShot(SoundCache.SpendResources);
                tempCostScene = info.Target.SceneName;
            }

            List<int> fadeOuts = [];
            for (int i = 0; i < choiceObjects.Count; i++) if (i != selection) fadeOuts.Add(i);
            fadeOuts.Shuffle(new());

            foreach (int i in fadeOuts)
            {
                choiceObjects[i].FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
                if (i != fadeOuts[fadeOuts.Count - 1]) yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_SCENE_STAGGER);
            }
            yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_FINAL_SCENE_STAGGER);
            choiceObjects[selection].FlyUp();
            FadeOutAll();

            yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_FINAL_DELAY);
            choiceObjects[selection].FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);

            while (response.Value == null) yield return null;
            finalizeSelectionCb!(response.Value);
        }
        StartCoroutine(LockIn());
    }

    private bool CanSelect()
    {
        if (!_canSelect) return false;
        return _canSelect = choiceInfos.Any(i => i.CanAfford(module!));
    }

    private void TryTogglePin(int selection)
    {
        if (!CanSelect())
        {
            pushPinSlot?.Shake();
            return;
        }

        if (hiddenPin)
        {
            pushPinSlot?.Shake();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            return;
        }

        var info = choiceInfos[selection];
        if (info.Pinned)
        {
            selectionCorners?.Shake();
            choiceObjects[selection].ShakePin();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
        }
        else if (newPinSelection.HasValue)
        {
            if (newPinSelection.Value == selection)
            {
                newPinSelection = null;
                tempPinReceipt = null;
                choiceObjects[selection].SetPinned(false);
                // TODO: Maybe undim eligible.
                audioSource!.PlayOneShot(SoundCache.Confirm);
            }
            else
            {
                selectionCorners?.Shake();
                choiceObjects[newPinSelection.Value].ShakePin();
                audioSource!.PlayOneShot(SoundCache.FailedMenu);
            }
        }
        else if (module!.PushPins == 0)
        {
            pushPinSlot?.Shake();
            selectionCorners?.Shake();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
        }
        else
        {
            newPinSelection = selection;
            tempPinReceipt = module.NextPinReceipt();
            choiceObjects[selection].SetPinned(true);
            audioSource!.PlayOneShot(SoundCache.Confirm);
            // TODO: Maybe dim ineligible.
        }
    }

    private void TryReroll()
    {
        if (!CanSelect())
        {
            diceTotemSlot?.Shake();
            return;
        }
        if (rerolled || module!.DiceTotems == 0)
        {
            diceTotemSlot?.Shake();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            return;
        }
        if (newPinSelection.HasValue)
        {
            ShakePinIndicators();
            audioSource!.PlayOneShot(SoundCache.FailedMenu);
            return;
        }

        acceptingInput = false;
        rerolled = true;
        var success = rerollCb!(out var newChoices);
        IEnumerator DoReroll()
        {
            audioSource!.PlayOneShot(SoundCache.RollTotem);
            selectionCorners?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            selectionCorners = null;
            if (success) audioSource!.PlayOneShot(SoundCache.SpendResources);

            List<int> indices = [];
            for (int i = 0; i < choiceInfos.Count; i++) indices.Add(i);
            indices.Shuffle(new());

            foreach (var idx in indices)
            {
                choiceObjects[idx].FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
                if (idx != indices[indices.Count - 1]) yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_SCENE_STAGGER);
            }

            if (!success)
            {
                FadeOutAll();
                yield return new WaitForSeconds(UIConstants.SCENE_FADE_OUT_DURATION);
                failedRerollCb!();
                yield break;
            }

            yield return new WaitForSeconds(UIConstants.SCENE_FADE_OUT_DURATION);
            choiceInfos = newChoices;
            layout = new(newChoices.Count);
            choiceObjects.Clear();

            StartCoroutine(LoadChoiceInfos());
        }

        StartCoroutine(DoReroll());
    }

    private const float START_RED_OUT = 3f;
    private const float RED_OUT = 6f;
    private float rejectionTimer;
    private bool rejected = false;
    private SpriteRenderer? redOutRenderer;

    private void Update()
    {
        if (!acceptingInput) return;

        var actions = InputHandler.Instance.inputActions;
        var current = selection != -1 ? layout![selection] : null;
        if (actions.left.WasPressed) TryMoveToIndex(current?.LeftIndex);
        else if (actions.right.WasPressed) TryMoveToIndex(current?.RightIndex);
        else if (actions.up.WasPressed) TryMoveToIndex(current?.UpIndex);
        else if (actions.down.WasPressed) TryMoveToIndex(current?.DownIndex);
        else if (actions.jump.WasPressed || actions.attack.WasPressed) TrySelectIndex(selection);
        else if (actions.cast.WasPressed) TryTogglePin(selection);
        else if (actions.dash.WasPressed) TryReroll();

        if (!CanSelect() && !rejected)
        {
            rejectionTimer += Time.deltaTime;
            if (rejectionTimer >= RED_OUT)
            {
                redOutRenderer?.SetAlpha(1f);
                rejected = true;
                sendSelectionCb!(new(), _ => { });
            }
            else if (rejectionTimer >= START_RED_OUT) redOutRenderer?.SetAlpha((rejectionTimer - START_RED_OUT) / (RED_OUT - START_RED_OUT));
        }
    }
}
