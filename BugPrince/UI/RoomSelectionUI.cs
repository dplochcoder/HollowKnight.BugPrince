using BugPrince.IC;
using BugPrince.Util;
using PurenailCore.SystemUtil;
using System.Collections;
using System.Collections.Generic;
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
    internal delegate void SelectionCb(RoomSelectionDecision);
    internal delegate List<SceneChoiceInfo>? RerollCb(RoomSelectionRerollDecision);

    private BugPrinceModule? module;
    private List<SceneChoiceInfo> choiceInfos = [];
    private SelectionCb selectionCb = _ => { };
    private RerollCb rerollCb = _ => null;

    private RoomSelectionLayout? layout;

    internal static RoomSelectionUI Create(BugPrinceModule module, List<SceneChoiceInfo> infos, SelectionCb selectionCb, RerollCb rerollCb)
    {
        GameObject obj = new("Room Selection");
        var ui = obj.AddComponent<RoomSelectionUI>();
        ui.Init(module, infos, selectionCb, rerollCb);

        obj.SetActive(true);
        return ui;
    }

    private void Init(BugPrinceModule module, List<SceneChoiceInfo> infos, SelectionCb selectionCb, RerollCb rerollCb)
    {
        this.module = module;
        choiceInfos = [.. infos];
        this.selectionCb = selectionCb;
        this.rerollCb = rerollCb;

        layout = new(infos.Count);
    }

    private void Awake() => StartCoroutine(LoadIn());

    private const float SCENE_STAGGER = 0.15f;
    private const float FADE_OUT_DURATION = 0.65f;
    private const float FINAL_SCENE_STAGGER = 0.5f;
    private const float FINAL_DELAY = 0.5f;

    private List<SceneChoice> choiceObjects = [];

    private IEnumerator LoadIn()
    {
        for (int i = 0; i < choiceInfos.Count; i++)
        {
            var choice = SceneChoice.Create(choiceInfos[i], layout![i].Pos);
            choiceObjects.Add(choice);
            if (i + 1 < choiceInfos.Count) yield return new WaitForSeconds(SCENE_STAGGER);
        }

        yield return new WaitUntil(() => choiceObjects[choiceObjects.Count - 1].IsReady());

        selection = 0;
        newPinSelection = null;
        selectionCorners ??= SelectionCorners.Create(layout![0].Pos);
        audioSource ??= gameObject.AddComponent<AudioSource>();
        acceptingInput = true;
    }

    private bool rerolled = false;
    private bool havePinned => choiceInfos[choiceInfos.Count - 1].Pinned;

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
        if (newPinSelection.HasValue && havePinned && selection != newPinSelection.Value && selection != choiceInfos.Count - 1)
        {
            // Can't make 2 pins.
            selectionCorners?.Shake();
            choiceObjects[newPinSelection.Value].Shake();
            choiceObjects[choiceInfos.Count - 1].Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        if (!info.CanAfford(module!))
        {
            selectionCorners?.Shake();
            choiceObjects[selection].Shake();
            // TODO: Shake relevant inventory.
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            return;
        }

        acceptingInput = false;
        if (newPinSelection == selection)
        {
            // Return the pin if immediately selected.
            choiceObjects[selection].Pin(false);
            newPinSelection = null;
        }

        IEnumerator LockIn()
        {
            if (info.Cost.HasValue)
            {
                audioSource!.PlayOneShot(SoundCache.spend_resources);
                // TODO: Cost anim
            }

            List<int> fadeOuts = [];
            for (int i = 0; i < choiceObjects.Count; i++) if (i != selection) fadeOuts.Add(i);
            fadeOuts.Shuffle(new());

            foreach (int i in fadeOuts)
            {
                choiceObjects[i].FadeOut(FADE_OUT_DURATION);
                yield return new WaitForSeconds(SCENE_STAGGER);
            }
            yield return new WaitForSeconds(FINAL_SCENE_STAGGER - SCENE_STAGGER);
            choiceObjects[selection].FlyUp();
            yield return new WaitForSeconds(FINAL_DELAY);

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
        var info = choiceInfos[selection];
        if (info.Pinned)
        {
            selectionCorners?.Shake();
            choiceObjects[selection].Shake();
            audioSource!.PlayOneShot(SoundCache.failed_menu);
        }
        else if (newPinSelection.HasValue)
        {
            if (newPinSelection.Value == selection)
            {
                newPinSelection = null;
                choiceObjects[selection].Pin(false);
                // TODO: Maybe undim eligible.
                audioSource!.PlayOneShot(SoundCache.confirm);
            }
            else
            {
                selectionCorners?.Shake();
                choiceObjects[newPinSelection.Value].Shake();
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
            choiceObjects[selection].Pin(true);
            audioSource!.PlayOneShot(SoundCache.confirm);
            // TODO: Maybe dim ineligible.
        }
    }

    private void TryReroll()
    {
        if (module!.DiceTotems == 0)
        {
            audioSource!.PlayOneShot(SoundCache.failed_menu);
            // TODO: Shake totems
        }
        else
        {
            // FIXME
        }
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
