using BugPrince.IC;
using DebugMod;

namespace BugPrince.DebugInterop;

public static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    private static bool BugPrinceEnabled(out BugPrinceModule mod)
    {
        mod = ItemChanger.ItemChangerMod.Modules.Get<BugPrinceModule>();
        if (mod == null)
        {
            Console.AddLine("Bug Prince not enabled in this save; doing nothing");
            return false;
        }

        return true;
    }

    [BindableMethod(name = "Give Gem", category = "Bug Prince")]
    public static void GiveGem()
    {
        if (!BugPrinceEnabled(out var mod)) return;
        mod.Gems++;
    }

    [BindableMethod(name = "Give Coin", category = "Bug Prince")]
    public static void GiveCoin()
    {
        if (!BugPrinceEnabled(out var mod)) return;
        mod.Coins++;
    }

    [BindableMethod(name = "Give Dice Totem", category = "Bug Prince")]
    public static void GiveDiceTotem()
    {
        if (!BugPrinceEnabled(out var mod)) return;
        mod.DiceTotems++;
    }

    [BindableMethod(name = "Give Push Pin", category = "Bug Prince")]
    public static void GivePushPin()
    {
        if (!BugPrinceEnabled(out var mod)) return;
        mod.PushPins++;
    }
}