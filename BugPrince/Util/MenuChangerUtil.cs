using MenuChanger.MenuPanels;

namespace BugPrince.Util;

internal static class MenuChangerUtil
{
    internal static void SetShown(this GridItemPanel self, bool show)
    {
        if (show) self.Show(); else self.Hide();
    }
}
