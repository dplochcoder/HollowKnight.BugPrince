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
        return ItemChangerMod.Modules.Get<TransitionSelectionModule>()?.AsProgressionProvider();
    }

    internal void OverrideProgressionProvider(ICostGroupProgressionProvider? progressionProvider) => progressionProviderOverride = progressionProvider;

    public override bool TryMatch(LogicManager lm, string term, out LogicVariable variable)
    {
        if (TryMatchPrefix(term, BUG_PRINCE_ACCESS_PREFIX, out var args) && args.Length == 2)
        {
            variable = ParseBugPrinceAccess(lm, term, new(args[0], args[1]));
            return true;
        }

        return Inner!.TryMatch(lm, term, out variable);
    }

    private LogicInt ParseBugPrinceAccess(LogicManager lm, string name, Transition transition)
    {
        var provider = GetProgressionProvider();
        if (provider == null) throw new ArgumentException("Missing progression provider");
        if (!provider.IsRandomizedTransition(transition)) return new ConstantInt(LogicVariable.TRUE);
        if (!provider.GetCostGroupByScene(transition.SceneName, out var groupName, out var group)) return new ConstantInt(LogicVariable.TRUE);

        return new BugPrinceAccessLogicInt(this, name, groupName, group.Type.GetTerm(lm));
    }
}

internal class BugPrinceAccessLogicInt : LogicInt
{
    private readonly BugPrinceVariableResolver resolver;
    private readonly string name;
    private readonly string costGroupName;
    private readonly Term term;

    public override string Name => name;

    internal BugPrinceAccessLogicInt(BugPrinceVariableResolver resolver, string name, string costGroupName, Term term)
    {
        this.resolver = resolver;
        this.name = name;
        this.costGroupName = costGroupName;
        this.term = term;
    }

    public override IEnumerable<Term> GetTerms() => [term];

    private ICostGroupProgressionProvider? cachedProvider;
    private int cachedCost;

    public override int GetValue(object? sender, ProgressionManager pm)
    {
        var provider = resolver.GetProgressionProvider() ?? throw new ArgumentException("ProgressionProvider disappeared");
        if (provider != cachedProvider)
        {
            cachedProvider = provider;
            if (!provider.GetProgressiveCost(costGroupName, out var _, out cachedCost)) throw new ArgumentException("CostGroup state changed");
        }

        return pm.Get(term) >= cachedCost ? TRUE : FALSE;
    }
}
