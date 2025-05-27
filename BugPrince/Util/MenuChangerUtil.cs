using MenuChanger.MenuElements;

namespace BugPrince.Util;

internal static class MenuChangerUtil
{
    internal static void SetLocked(this ILockable self, bool locked)
    {
        if (locked) self.Lock();
        else self.Unlock();
    }

    internal static void SetUnlocked(this ILockable self, bool unlocked) => self.SetLocked(!unlocked);
}
