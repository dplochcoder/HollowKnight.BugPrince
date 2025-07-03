using BugPrince.IC;
using BugPrince.ItemSyncInterop;
using BugPrince.Util;
using DebugMod;
using System;
using System.Collections.Generic;

namespace BugPrince.DebugInterop;

public static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    private static bool TransitionSelectionEnabled(out TransitionSelectionModule mod)
    {
#pragma warning disable CS8601 // Possible null reference assignment.
        mod = ItemChanger.ItemChangerMod.Modules.Get<TransitionSelectionModule>();
#pragma warning restore CS8601 // Possible null reference assignment.
        if (mod == null)
        {
            DebugMod.Console.AddLine("Bug Prince not enabled in this save; doing nothing");
            return false;
        }

        return true;
    }

    [BindableMethod(name = "Give Gem", category = "Bug Prince")]
    public static void GiveGem()
    {
        if (!TransitionSelectionEnabled(out var mod)) return;
        mod.TotalGems++;
    }

    [BindableMethod(name = "Give Coin", category = "Bug Prince")]
    public static void GiveCoin()
    {
        if (!TransitionSelectionEnabled(out var mod)) return;
        mod.TotalCoins++;
    }

    [BindableMethod(name = "Give Dice Totem", category = "Bug Prince")]
    public static void GiveDiceTotem()
    {
        if (!TransitionSelectionEnabled(out var mod)) return;
        mod.DiceTotems++;
    }

    [BindableMethod(name = "Give Push Pin", category = "Bug Prince")]
    public static void GivePushPin()
    {
        if (!TransitionSelectionEnabled(out var mod)) return;
        mod.TotalPushPins++;
    }

    [BindableMethod(name = "Give Nail Upgrade", category = "Bug Prince")]
    public static void GiveNailUpgrade()
    {
        var pd = PlayerData.instance;
        var upgrades = pd.GetInt(nameof(pd.nailSmithUpgrades));
        if (upgrades >= 4)
        {
            DebugMod.Console.AddLine("Already at maximum nail upgrades");
            return;
        }

        pd.SetBool(nameof(pd.honedNail), true);
        pd.IntAdd(nameof(pd.nailDamage), 4);
        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        pd.IncrementInt(nameof(pd.nailSmithUpgrades));
    }

#if DEBUG
    [BindableMethod(name = "Replay Updates", category = "Bug Prince")]
    public static void ReplayUpdates()
    {
        if (!TransitionSelectionEnabled(out var mod)) return;

        mod.TotalCoins = Math.Max(mod.TotalCoins, 100);
        mod.TotalGems = Math.Max(mod.TotalGems, 100);
        mod.TotalPushPins = Math.Max(mod.TotalPushPins, 100);
        mod.DiceTotems = Math.Max(mod.DiceTotems, 100);

        try
        {
            var file = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(BugPrinceMod).Assembly.Location), "replay.json");
            List<TransitionSwapUpdate> updates = PurenailCore.SystemUtil.JsonUtil<BugPrinceMod>.DeserializeFromPath<List<TransitionSwapUpdate>>(file);
            int success = 0;
            foreach (var update in updates)
            {
                update.SequenceNumber = -1;
                SwapTransitionsRequest req = new() { Update = update };
                Wrapped<SwapTransitionsResponse?> resp = new(null);
                mod.SwapTransitions(req, r => resp.Value = r);

                if (!resp.Value!.Accepted) break;
                else ++success;
            }
            DebugMod.Console.AddLine($"Applied {success} of {updates.Count} transition swap updates");
        }
        catch (Exception ex)
        {
            DebugMod.Console.AddLine($"Failed to apply updates: {ex}");
        }
    }

    [BindableMethod(name = "Take Screenshot", category = "Bug Prince")]
    public static void TakeScreenshot()
    {
        var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var file = $"C:/Users/danie/Documents/Scenes/{name}.png";

        UnityEngine.ScreenCapture.CaptureScreenshot(file);
    }
#endif
}