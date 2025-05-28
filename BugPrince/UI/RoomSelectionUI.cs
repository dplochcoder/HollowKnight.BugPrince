using BugPrince.Data;
using BugPrince.IC;
using BugPrince.Util;
using PurenailCore.GOUtil;
using PurenailCore.SystemUtil;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BugPrince.UI;

internal record RoomSelectionRerollDecision
{
    public SceneChoiceInfo? newPin;
}

internal record RoomSelectionDecision
{
    public SceneChoiceInfo? chosen;  // Can be null if no choices affordable.
    public SceneChoiceInfo? newPin;  // Set if pin used.
}

internal class RoomSelectionUI : MonoBehaviour
{
    internal delegate void SelectionCb(RoomSelectionDecision decision);
    internal delegate List<SceneChoiceInfo>? RerollCb(RoomSelectionRerollDecision decision);

    private static readonly EmbeddedSprite fullwhite = new("UI.fullwhite");
    private static readonly EmbeddedSprite dash_ico = new("UI.dash_ico");
    private static readonly EmbeddedSprite spell_ico = new("UI.spell_ico");

    internal static bool uiPresent = false;

    private BugPrinceModule? module;
    private List<SceneChoiceInfo> choiceInfos = [];
    private bool hiddenPin;
    private SelectionCb selectionCb = _ => { };
    private RerollCb rerollCb = _ => null;

    private RoomSelectionLayout? layout;

    internal static RoomSelectionUI Create(BugPrinceModule module, GateDirection gateDir, List<SceneChoiceInfo> infos, SelectionCb selectionCb, RerollCb rerollCb)
    {
        GameObject obj = new("Room Selection");
        obj.SetActive(false);
        obj.transform.SetParent(GameObject.Find("_GameCameras/HudCamera").transform);
        obj.transform.position = Vector3.zero;

        var ui = obj.AddComponent<RoomSelectionUI>();
        ui.Init(module, gateDir, infos, selectionCb, rerollCb);

        obj.SetActive(true);
        uiPresent = true;
        return ui;
    }

    private void OnDestroy() => uiPresent = false;

    private void Init(BugPrinceModule module, GateDirection gateDir, List<SceneChoiceInfo> infos, SelectionCb selectionCb, RerollCb rerollCb)
    {
        this.module = module;
        choiceInfos = [.. infos];
        hiddenPin = module.PinnedScene != null && !choiceInfos.Any(i => i.Pinned);
        this.selectionCb = selectionCb;
        this.rerollCb = rerollCb;

        layout = new(infos.Count);

        // Lock player so they don't fall off screen.
        gameObject.AddComponent<PlayerLocker>().SetDirection(gateDir);
    }

    private void Awake() => StartCoroutine(LoadIn());

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

    private IEnumerator LoadIn()
    {
        bool first = backgroundFade == null;
        if (first)
        {
            backgroundFade = gameObject.AddNewChild("BGFade");
            backgroundFade.transform.localScale = new(50, 50, 50);
            backgroundFade.SetActive(false);
            var bgRender = backgroundFade.AddComponent<SpriteRenderer>();
            bgRender.sprite = fullwhite.Value;
            bgRender.SetUILayer(UISortingOrder.Background);
            backgroundFade.FadeColor(Color.black.WithAlpha(0), Color.black.WithAlpha(0.5f), 1.5f);
            backgroundFade.SetActive(true);
        }

        float bottom = layout![choiceInfos.Count - 1].Pos.y;
        float invY = bottom - UIConstants.INV_SLOT_BAR_Y_SPACE;
        Vector2 InvPos(int idx) => new((idx * 2 - 3) * UIConstants.INV_SLOT_BAR_X_SPACE / 2, invY);

        if (first)
        {
            diceTotemSlot ??= InventorySlot.Create(gameObject, InvPos(0), module!.DiceTotems, DiceTotemItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE);
            coinSlot ??= InventorySlot.Create(gameObject, InvPos(1), module!.Coins, CoinItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE);
            gemSlot ??= InventorySlot.Create(gameObject, InvPos(2), module!.Gems, GemItem.LargeSprite.Value, UIConstants.INV_SLOT_GEM_ITEM_SCALE);
            pushPinSlot ??= InventorySlot.Create(gameObject, InvPos(3), module!.PushPins, PushPinItem.LargeSprite.Value, UIConstants.INV_SLOT_ITEM_SCALE);
            dashIco ??= MakeSprite(gameObject, InvPos(0) - new Vector2(0, 1.75f), dash_ico.Value);
            spellIco ??= MakeSprite(gameObject, InvPos(3) - new Vector2(0, 1.75f), spell_ico.Value);

            diceTotemSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
            coinSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
            gemSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
            pushPinSlot.FadeIn(UIConstants.SCENE_ASCEND_TIME);
            dashIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
            spellIco.FadeColor(Color.white.WithAlpha(0), Color.white, UIConstants.SCENE_ASCEND_TIME);
        }

        for (int i = 0; i < choiceInfos.Count; i++)
        {
            var choice = SceneChoice.Create(gameObject, choiceInfos[i], layout[i].Pos);
            choiceObjects.Add(choice);
            if (i + 1 < choiceInfos.Count) yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_SCENE_STAGGER);
        }

        yield return new WaitUntil(() => choiceObjects[choiceObjects.Count - 1].IsReady());

        selection = 0;
        newPinSelection = null;
        selectionCorners ??= SelectionCorners.Create(gameObject, layout![0].Pos);
        audioSource ??= gameObject.AddComponent<AudioSource>();
        acceptingInput = true;
    }

    private bool rerolled = false;
    private bool HavePinned => choiceInfos[choiceInfos.Count - 1].Pinned;

    private int selection = 0;
    private int? newPinSelection;
    private GameObject? backgroundFade;
    private SelectionCorners? selectionCorners;
    private AudioSource? audioSource;
    private InventorySlot? diceTotemSlot;
    private InventorySlot? coinSlot;
    private InventorySlot? gemSlot;
    private InventorySlot? pushPinSlot;
    private GameObject? dashIco;
    private GameObject? spellIco;
    private bool acceptingInput = false;

    private void TryMoveToIndex(int? target)
    {
        if (target.HasValue)
        {
            selection = target.Value;
            selectionCorners!.UpdateTarget(layout![target.Value].Pos);
            audioSource!.PlayOneShot(SoundCache.change_selection);
        }
        else
        {
            audioSource!.PlayOneShot(SoundCache.failed_menu);
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
        };
    }

    private void TrySelectIndex(int selection)
    {
        var info = choiceInfos[selection];
        if (newPinSelection.HasValue && HavePinned && selection != newPinSelection.Value && selection != choiceInfos.Count - 1)
        {
            // Can't make 2 pins.
            choiceObjects[newPinSelection.Value].ShakePin();
            choiceObjects[choiceInfos.Count - 1].ShakePin();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        if (!info.CanAfford(module!))
        {
            choiceObjects[selection].ShakeCosts();
            GetInvSlot(info)?.Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        acceptingInput = false;
        if (newPinSelection == selection)
        {
            // Return the pin if immediately selected.
            choiceObjects[selection].SetPinned(false);
            newPinSelection = null;
            pushPinSlot?.Give(1);
        }

        IEnumerator LockIn()
        {
            audioSource!.PlayOneShot(SoundCache.confirm);
            if (info.Cost.HasValue)
            {
                audioSource!.PlayOneShot(SoundCache.spend_resources);
                GetInvSlot(info)?.Take(info.Cost.Value.Item2);
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
            selectionCorners?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            dashIco?.FadeColor(Color.white.WithAlpha(0), UIConstants.SCENE_FADE_OUT_DURATION);
            diceTotemSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            coinSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            gemSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            pushPinSlot?.FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);
            spellIco?.FadeColor(Color.white.WithAlpha(0), UIConstants.SCENE_FADE_OUT_DURATION);

            yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_FINAL_DELAY);
            choiceObjects[selection].FadeOut(UIConstants.SCENE_FADE_OUT_DURATION);

            selectionCb(new()
            {
                newPin = newPinSelection.HasValue ? choiceInfos[newPinSelection.Value] : null,
                chosen = info,
            });
        };
        StartCoroutine(LockIn());
    }

    private void TryTogglePin(int selection)
    {
        if (hiddenPin)
        {
            pushPinSlot?.Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        var info = choiceInfos[selection];
        if (info.Pinned)
        {
            selectionCorners?.Shake();
            choiceObjects[selection].ShakePin();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
        }
        else if (newPinSelection.HasValue)
        {
            if (newPinSelection.Value == selection)
            {
                newPinSelection = null;
                pushPinSlot?.Give(1);
                choiceObjects[selection].SetPinned(false);
                // TODO: Maybe undim eligible.
                audioSource!.PlayOneShot(SoundCache.confirm);
            }
            else
            {
                selectionCorners?.Shake();
                choiceObjects[newPinSelection.Value].ShakePin();
                audioSource!.PlayOneShot(SoundCache.failed_menu);
            }
        }
        else if (module!.PushPins == 0)
        {
            pushPinSlot?.Shake();
            selectionCorners?.Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
        }
        else
        {
            pushPinSlot?.Take(1);
            newPinSelection = selection;
            choiceObjects[selection].SetPinned(true);
            audioSource!.PlayOneShot(SoundCache.confirm);
            // TODO: Maybe dim ineligible.
        }
    }

    private void TryReroll()
    {
        if (rerolled || module!.DiceTotems == 0)
        {
            diceTotemSlot?.Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        // TODO: Reroll
    }

    private void Update()
    {
        if (!acceptingInput) return;

        var actions = InputHandler.Instance.inputActions;
        var current = layout![selection];
        if (actions.left.WasPressed) TryMoveToIndex(current.LeftIndex);
        else if (actions.right.WasPressed) TryMoveToIndex(current.RightIndex);
        else if (actions.up.WasPressed) TryMoveToIndex(current.UpIndex);
        else if (actions.down.WasPressed) TryMoveToIndex(current.DownIndex);
        else if (actions.attack.WasPressed) TrySelectIndex(selection);
        else if (actions.cast.WasPressed) TryTogglePin(selection);  // FIXME???
        else if (actions.dash.WasPressed) TryReroll();
    }
}
