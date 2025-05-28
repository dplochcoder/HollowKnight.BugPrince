using BugPrince.IC;
using BugPrince.Util;
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

    private IEnumerator LoadIn()
    {
        for (int i = 0; i < choiceInfos.Count; i++)
        {
            var choice = SceneChoice.Create(gameObject, choiceInfos[i], layout![i].Pos);
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
    private SelectionCorners? selectionCorners;
    private AudioSource? audioSource;
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
            // TODO: Shake relevant inventory.
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        acceptingInput = false;
        if (newPinSelection == selection)
        {
            // Return the pin if immediately selected.
            choiceObjects[selection].SetPinned(false);
            newPinSelection = null;
        }

        IEnumerator LockIn()
        {
            audioSource!.PlayOneShot(SoundCache.confirm);

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

            if (info.Cost.HasValue)
            {
                audioSource!.PlayOneShot(SoundCache.spend_resources);
                // TODO: Cost anim
            }
            yield return new WaitForSeconds(UIConstants.ROOM_SELECTION_FINAL_DELAY);

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
            // TODO: Shake push pins.
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
            // TODO: Shake push pins.
            selectionCorners?.Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
        }
        else
        {
            // TODO: Update push pin UI.
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
            // TODO: Shake totems
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
        else if (actions.cast.WasPressed) TryTogglePin(selection);
        else if (actions.dash.WasPressed) TryReroll();
    }
}
