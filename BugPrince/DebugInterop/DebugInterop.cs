using BugPrince.IC;
using DebugMod;

namespace BugPrince.DebugInterop;

public static class DebugInterop
{
    internal static void Setup() => DebugMod.DebugMod.AddToKeyBindList(typeof(DebugInterop));

    private static bool BugPrinceEnabled(out TransitionSelectionModule mod)
    {
        mod = ItemChanger.ItemChangerMod.Modules.Get<TransitionSelectionModule>();
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

    [BindableMethod(name = "Give Nail Upgrade", category = "Bug Prince")]
    public static void GiveNailUpgrade()
    {
        var pd = PlayerData.instance;
        var upgrades = pd.GetInt(nameof(pd.nailSmithUpgrades));
        if (upgrades >= 4)
        {
            Console.AddLine("Already at maximum nail upgrades");
            return;
        }

        pd.SetBool(nameof(pd.honedNail), true);
        pd.IntAdd(nameof(pd.nailDamage), 4);
        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");
        pd.IncrementInt(nameof(pd.nailSmithUpgrades));
    }

#if DEBUG
    [BindableMethod(name = "Take Screenshot", category = "Bug Prince")]
    public static void TakeScreenshot()
    {
        var name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var file = $"C:/Users/danie/Documents/Scenes/{name}.png";

        ScreenCapture.CaptureScreenshot(file);
    }
#endif
}