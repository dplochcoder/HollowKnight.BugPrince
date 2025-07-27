using BugPrince.Data;
using BugPrince.IC;
using BugPrince.IC.Items;
using BugPrince.Util;
using PurenailCore.RandoUtil;
using RandomizerCore.Logic;
using RandomizerMod.RC;
using RandomizerMod.Settings;
using System.Collections.Generic;

namespace BugPrince.Rando;

internal static class LogicPatcher
{
    internal static void Setup()
    {
        RCData.RuntimeLogicOverride.Subscribe(-2000f, AddDefinitions);
        RCData.RuntimeLogicOverride.Subscribe(200f, ModifyTransitions);  // Must run after MoreDoors
    }

    private static void AddDefinitions(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        if (!BugPrinceMod.RS.IsEnabled) return;

        RandoInterop.LS = new();

        if (BugPrinceMod.RS.EnableTransitionChoices)
        {
            lmb.AddItem(DiceTotemItem.LogicItem());
            lmb.AddItem(PushPinItem.LogicItem());
            if (BugPrinceMod.RS.AreCostsEnabled)
            {
                lmb.VariableResolver = new BugPrinceVariableResolver(lmb.VariableResolver);
                lmb.AddCostTypeTerms();
                lmb.AddItem(CoinItem.LogicItem());
                lmb.AddItem(GemItem.LogicItem());
            }
        }

        foreach (var e in Locations.GetLocations()) if (BugPrinceMod.RS.IsLocationPoolEnabled(e.Value.LocationPool)) lmb.AddLogicDef(new(e.Key, e.Value.Logic.Value));
        foreach (var e in Waypoints.GetWaypoints()) if (BugPrinceMod.RS.IsLocationPoolEnabled(e.Value.LocationPool)) lmb.AddWaypoint(new(e.Key, e.Value.Logic, true));
    }

    private static void ModifyTransitions(GenerationSettings gs, LogicManagerBuilder lmb)
    {
        foreach (var transition in Transitions.GetTransitions())
        {
            if (!BugPrinceMod.RS.IsLocationPoolEnabled(transition.Value.LocationPool)) continue;

            lmb.AddTransition(new(transition.Key, transition.Value.Logic));
            transition.Value.LogicEdits.ForEach(lmb.DoLogicEdit);
        }

        if (!BugPrinceMod.RS.AreCostsEnabled) return;

        HashSet<string> costScenes = [];
        foreach (var cgp in CostGroup.GetProducers().Values) costScenes.AddRange(cgp.RelevantSceneNames());

        // Add proxies for all transitions that might have costs, to support purchase logic.
        LogicReplacer replacer = new();
        foreach (var transitionName in lmb.Transitions)
        {
            if (!transitionName.ToTransition(out var transition)) continue;
            if (!costScenes.Contains(transition.SceneName)) continue;

            // This scenario is more complicated than MoreDoors. There, we protect access to the *source* transition through logic. Here, we protect the *destination* transition.
            // We do this by splitting the target item into three parts:
            //  - The term itself, obtained at the source transition location.
            //  - The internal proxy, guarding access to the rest of the room.
            //  - The external proxy, representing reachability from the other side.

            var internalProxy = $"BugPrinceInternalProxy_{transitionName}";
            var externalProxy = $"BugPrinceExternalProxy_{transitionName}";

            replacer.SimpleTokenReplacements.Add(transitionName, new(externalProxy));
            replacer.SimpleTokenReplacements.Add($"{transitionName}/", new($"{externalProxy}/"));

            // We define a proxy waypoint which is only accessible via the transition term
            lmb.GetOrAddTerm(transitionName, TermType.State);
            
            // The internal proxy is only accessible if we can purchase the transition.
            lmb.AddWaypoint(new(internalProxy, $"{transitionName} + {BugPrinceVariableResolver.BUG_PRINCE_ACCESS_PREFIX}[{transition}]"));
            replacer.IgnoredNames.Add(internalProxy);

            // The external proxy represents what the transition used to represent.
            lmb.AddWaypoint(new(externalProxy, lmb.LogicLookup[transitionName].ToInfix()));
            lmb.DoSubst(new(externalProxy, transitionName, internalProxy));
            lmb.DoSubst(new(externalProxy, $"{transitionName}/", $"{internalProxy}/"));

            // The transition location is only accessible from the external proxy.
            lmb.DoLogicEdit(new(transitionName, externalProxy));
            replacer.IgnoredNames.Add(transitionName);
        }
        replacer.Apply(lmb);
    }
}
