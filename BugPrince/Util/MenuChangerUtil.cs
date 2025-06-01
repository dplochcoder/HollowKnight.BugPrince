using MenuChanger.MenuElements;

namespace BugPrince.Util;

internal static class MenuChangerUtil
{
    internal static void SetShown(this IMenuElement self, bool show)
    {
        if (show) self.Show(); else self.Hide();
    }
}
