using BugPrince.Util;
using PurenailCore.SystemUtil;
using System;
using System.Collections.Generic;

namespace BugPrince.IC;

internal class BreakablesModule : ItemChanger.Modules.Module
{
    private Dictionary<Breakable, HashSet<Action>> actions = [];

    public override void Initialize() => On.Breakable.Break += OnBreak;

    public override void Unload() => On.Breakable.Break -= OnBreak;

    private void OnBreak(On.Breakable.orig_Break orig, Breakable self, float flingAngleMin, float flingAngleMax, float impactMultiplier)
    {
        if (actions.TryGetValue(self, out var set))
        {
            set.ForEach(a => a());
            actions.Remove(self);
        }

        orig(self, flingAngleMin, flingAngleMax, impactMultiplier);
    }

    internal void DoOnBreak(Breakable breakable, Action action)
    {
        actions.GetOrAddNew(breakable).Add(action);
        breakable.gameObject.DoOnDestroy(() => actions.Remove(breakable));
    }
}
