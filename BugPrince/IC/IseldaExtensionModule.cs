using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using ItemChanger;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using ItemChanger.Items;
using Modding;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace BugPrince.IC;

internal class IseldaExtensionModule : ItemChanger.Modules.Module
{
    private static readonly FsmID FSM_ID = new("Iselda", "Conversation Control");

    // Store these separately so RandoMapMod still works.
    public HashSet<string> RealFieldNames = [];

    [JsonIgnore]
    public int NumMaps => RealFieldNames.Count;

    public override void Initialize()
    {
        AbstractItem.ModifyItemGlobal += MaybeUpgradeMap;
        ModHooks.GetPlayerBoolHook += OverrideGetBool;
        Events.AddLanguageEdit(new(MAPS_5_CONV_KEY), Do5MapsConvo);
        Events.AddLanguageEdit(new(MAPS_11_CONV_KEY), Do11MapsConvo);
        Events.AddFsmEdit(SceneNames.Room_mapper, FSM_ID, ModifyIselda);
    }

    public override void Unload()
    {
        AbstractItem.ModifyItemGlobal -= MaybeUpgradeMap;
        ModHooks.GetPlayerBoolHook -= OverrideGetBool;
        Events.RemoveFsmEdit(SceneNames.Room_mapper, FSM_ID, ModifyIselda);
    }

    private void MaybeUpgradeMap(GiveEventArgs args)
    {
        if (args.Item is MapItem mapItem) args.Item = new ExtensionMapItem(mapItem);
    }

    private bool OverrideGetBool(string name, bool orig) => name switch
    {
        "bugPrince_have5Maps" => NumMaps >= 5,
        "bugPrince_have11Maps" => NumMaps >= 11,
        _ => orig
    };

    public bool SpokenMaps5;
    public bool SpokenMaps11;

    private static FsmState AddConvo(PlayMakerFSM fsm, string name, string key)
    {
        var audioTemplate = fsm.GetState("Meet").GetFirstActionOfType<AudioPlayerOneShotSingle>();
        var dialogue = fsm.FsmVariables.GetFsmGameObject("DialogueText").Value.GetComponent<DialogueBox>();

        var state = fsm.AddState(name);
        state.AddTransition("CONVO_FINISH", "To Shop");
        state.AddLastAction(new AudioPlayerOneShotSingle()
        {
            audioPlayer = audioTemplate.audioPlayer,
            spawnPoint = audioTemplate.spawnPoint,
            audioClip = audioTemplate.audioClip,
            storePlayer = audioTemplate.storePlayer
        });
        state.AddLastAction(new Lambda(() => dialogue.StartConversation(key, "")));
        return state;
    }

    private const string MAPS_5_CONV_KEY = "BLUE_PRINCE_ISELDA_MAPS_5";
    private const string MAPS_11_CONV_KEY = "BLUE_PRINCE_ISELDA_MAPS_11";

    private void Do5MapsConvo(ref string value)
    {
        List<string> lines = [
            "Ah, that's a number of them you've gathered already. Keeping my husband busy!",
            "Let me see if I've anything else in the back to offer you.",
            $"<page>Looks like he's got at least {11 - NumMaps} regions to charter if I'm not mistaken."];
        value = string.Join("<br>", lines);
    }

    private void Do11MapsConvo(ref string value)
    {
        string middle = NumMaps == 13 ? "I don't think there are any left." : "There can't be more than 1 or 2 left.";
        List<string> lines = [
            "You're going to run me out of business!",
            middle,
            "<page>Surely he's got something hidden in the bunk here he doesn't need..."];
        value = string.Join("<br>", lines);
    }

    private const string MAPS_5_EVENT = "Blue Prince Maps 5";
    private const string MAPS_11_EVENT = "Blue Prince Maps 11";

    private void ModifyIselda(PlayMakerFSM fsm)
    {
        var maps5 = AddConvo(fsm, "Maps 5", MAPS_5_CONV_KEY);
        var maps11 = AddConvo(fsm, "Maps 11", MAPS_11_CONV_KEY);

        var choice = fsm.GetState("Convo Choice");
        choice.AddFirstAction(new Lambda(() =>
        {
            if (!SpokenMaps11 && NumMaps >= 11)
            {
                SpokenMaps5 = true;
                SpokenMaps11 = true;
                fsm.SendEvent(MAPS_11_EVENT);
            }
            else if (!SpokenMaps5 && NumMaps >= 5)
            {
                SpokenMaps5 = true;
                fsm.SendEvent(MAPS_5_EVENT);
            }
        }));
        choice.AddTransition(MAPS_5_EVENT, maps5);
        choice.AddTransition(MAPS_11_EVENT, maps11);
    }
}

internal class ExtensionMapItem : AbstractItem
{
    public MapItem mapItem;

    [JsonConstructor]
    private ExtensionMapItem() { }

    public ExtensionMapItem(MapItem mapItem)
    {
        this.mapItem = mapItem;
        UIDef = mapItem.UIDef;
    }

    public override void GiveImmediate(GiveInfo info)
    {
        mapItem.GiveImmediate(info);
        ItemChangerMod.Modules.Get<IseldaExtensionModule>()?.RealFieldNames.Add(mapItem.fieldName);
    }

    public override bool Redundant() => ItemChangerMod.Modules.Get<IseldaExtensionModule>()?.RealFieldNames.Contains(mapItem.fieldName) ?? mapItem.Redundant();
}
