using BugPrince.IC;
using ItemChanger;

namespace BugPrince.Rando;

internal static class RandoInterop
{
    internal static void Hook()
    {
        DefineCustomItems();
    }

    private static void DefineCustomItems()
    {
        Finder.DefineCustomItem(new CoinItem());
        Finder.DefineCustomItem(new DiceTotemItem());
        Finder.DefineCustomItem(new GemItem());
        Finder.DefineCustomItem(new PushPinItem());
    }
}
