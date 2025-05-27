using BugPrince.IC;
using ItemChanger;
using Newtonsoft.Json;
using RandomizerCore.Logic;
using RandomizerCore.Logic.StateLogic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Rando;

internal class BugPrinceVariableResolver : VariableResolver
{
    private ICostGroupProgressionProvider? progressionProviderOverride;

    [JsonConstructor]
    private BugPrinceVariableResolver() { }

    public BugPrinceVariableResolver(VariableResolver inner) => Inner = inner;

    internal ICostGroupProgressionProvider? GetProgressionProvider()
    {
        if (progressionProviderOverride != null) return progressionProviderOverride;
        if (RandoInterop.LS != null) return RandoInterop.LS;
        return ItemChangerMod.Modules.Get<BugPrinceModule>()?.AsProgressionProvider();
    }

    internal void OverrideProgressionProvider(ICostGroupProgressionProvider? progressionProvider) => progressionProviderOverride = progressionProvider;

    public override bool TryMatch(LogicManager lm, string term, out LogicVariable variable) => TryMatchImpl(lm, term, out variable) || Inner!.TryMatch(lm, term, out variable);

    private bool TryMatchImpl(LogicManager lm, string term, out LogicVariable variable)
    {
        variable = default;
        if (term.StartsWith("$")) return false;

        var provider = GetProgressionProvider();
        if (provider == null) return false;
        if (!provider.IsRandomizedTransition(term)) return false;
        if (!term.ToTransition(out var transition)) return false;
        if (!provider.GetCostGroupByScene(transition.SceneName, out var groupName, out var group)) return false;
        if (Inner == null || !Inner.TryMatch(lm, term, out LogicVariable? inner)) return false;

        BugPrinceLogicVariableBase varBase = new(this, term, groupName, group.Type.GetTerm(lm));
        if (inner is StateModifier stateModifier) variable = new BugPrinceStateModifier(varBase, stateModifier);
        else if (inner is LogicInt logicInt) variable = new BugPrinceLogicInt(varBase, logicInt);
        return true;
    }
}

internal class BugPrinceLogicVariableBase
{
    private readonly BugPrinceVariableResolver resolver;
    private readonly string name;
    private readonly string costGroupName;
    private readonly Term term;

    internal BugPrinceLogicVariableBase(BugPrinceVariableResolver resolver, string name, string costGroupName, Term term)
    {
        this.resolver = resolver;
        this.name = name;
        this.costGroupName = costGroupName;
        this.term = term;
    }

    internal string Name() => $"BugPrince[{name}]";

    internal Term Term() => term;

    private ICostGroupProgressionProvider? cachedProvider;
    private int cachedCost;

    internal bool CanPayCost(ProgressionManager pm)
    {
        var provider = resolver.GetProgressionProvider() ?? throw new ArgumentException("ProgressionProvider disappeared");
        if (provider != cachedProvider)
        {
            cachedProvider = provider;
            if (!provider.GetProgressiveCost(costGroupName, out var _, out cachedCost)) throw new ArgumentException("CostGroup state changed");
        }

        return pm.Get(term) >= cachedCost;
    }
}

internal class BugPrinceStateModifier : StateModifier
{
    private readonly BugPrinceLogicVariableBase varBase;
    private readonly StateModifier inner;

    internal BugPrinceStateModifier(BugPrinceLogicVariableBase varBase, StateModifier inner)
    {
        this.varBase = varBase;
        this.inner = inner;
    }

    public override string Name => varBase.Name();

    public override IEnumerable<Term> GetTerms() => inner.GetTerms().Concat([varBase.Term()]);

    public override IEnumerable<LazyStateBuilder> ModifyState(object? sender, ProgressionManager pm, LazyStateBuilder state) => varBase.CanPayCost(pm) ? inner.ModifyState(sender, pm, state) : [];
}

internal class BugPrinceLogicInt : LogicInt
{
    private readonly BugPrinceLogicVariableBase varBase;
    private readonly LogicInt inner;

    internal BugPrinceLogicInt(BugPrinceLogicVariableBase varBase, LogicInt inner)
    {
        this.varBase = varBase;
        this.inner = inner;
    }

    public override string Name => varBase.Name();

    public override IEnumerable<Term> GetTerms() => inner.GetTerms().Concat([varBase.Term()]);

    public override int GetValue(object? sender, ProgressionManager pm) => varBase.CanPayCost(pm) ? inner.GetValue(sender, pm) : FALSE;
}
