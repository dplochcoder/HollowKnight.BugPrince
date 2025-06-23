using BugPrince.Data;
using BugPrince.IC;
using ItemChanger;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using System;
using System.Collections.Generic;

namespace BugPrince.Rando;

internal class BugPrinceVariableResolver : VariableResolver
{
    internal const string BUG_PRINCE_ACCESS_PREFIX = "$BugPrinceAccess";

    private ICostGroupProgressionProvider? progressionProviderOverride;

    [JsonConstructor]
    private BugPrinceVariableResolver() { }

    public BugPrinceVariableResolver(VariableResolver inner) => Inner = inner;

    internal ICostGroupProgressionProvider? GetProgressionProvider()
    {
        if (progressionProviderOverride != null) return progressionProviderOverride;
        if (RandoInterop.LS != null) return RandoInterop.LS;
        return ItemChangerMod.Modules.Get<TransitionSelectionModule>();
    }

    internal void OverrideProgressionProvider(ICostGroupProgressionProvider? progressionProvider) => progressionProviderOverride = progressionProvider;

    public override bool TryMatch(LogicManager lm, string term, out LogicVariable variable)
    {
        if (TryMatchPrefix(term, BUG_PRINCE_ACCESS_PREFIX, out var args) && args.Length == 1)
        {
            if (!args[0].ToTransition(out var transition)) throw new ArgumentException($"Invalid transition argument: {term}");
            variable = new BugPrinceAccessLogicInt(this, transition, CostType.Coins.GetTerm(lm), CostType.Gems.GetTerm(lm));
            return true;
        }

        return Inner!.TryMatch(lm, term, out variable);
    }
}

internal class BugPrinceAccessLogicInt : LogicInt
{
    private readonly BugPrinceVariableResolver resolver;
    private readonly Transition transition;
    private readonly Term coinsTerm;
    private readonly Term gemsTerm;

    public override string Name => $"{BugPrinceVariableResolver.BUG_PRINCE_ACCESS_PREFIX}[{transition}]";

    internal BugPrinceAccessLogicInt(BugPrinceVariableResolver resolver, Transition transition, Term coinsTerm, Term gemsTerm)
    {
        this.resolver = resolver;
        this.transition = transition;
        this.coinsTerm = coinsTerm;
        this.gemsTerm = gemsTerm;
    }

    public override IEnumerable<Term> GetTerms() => [coinsTerm, gemsTerm];

    private ICostGroupProgressionProvider? cachedProvider;
    private Term? cachedTerm;
    private int cachedCost;

    public override int GetValue(object? sender, ProgressionManager pm)
    {
        var provider = resolver.GetProgressionProvider() ?? throw new ArgumentException("ProgressionProvider disappeared");
        if (cachedProvider == null || provider.Generation() != cachedProvider.Generation())
        {
            cachedProvider = provider;
            if (provider.GetProgressiveCostByScene(transition.SceneName, out var costType, out var cost))
            {
                cachedTerm = costType == CostType.Coins ? coinsTerm : gemsTerm;
                cachedCost = cost;
            }
            else cachedCost = -1;
        }

        return (cachedCost < 0 || pm.Get(cachedTerm!) >= cachedCost) ? TRUE : FALSE;
    }
}
