using System;

namespace BugPrince.Util;

internal class Timer
{
    private DateTime prev = DateTime.Now;

    internal void Log(string name)
    {
        var next = DateTime.Now;
        BugPrinceMod.DebugLog($"{name} took {(next - prev).Milliseconds / 1000.0:0.000} seconds");
        prev = next;
    }
}
