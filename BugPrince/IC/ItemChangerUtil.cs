using ItemChanger.Util;
using ItemChanger;
using UnityEngine;

namespace BugPrince.IC;

enum FlingDirection
{
    Any,
    Left,
    Right,
    Down
}

internal static class ItemChangerUtil
{
    internal static bool GiveOrFling(this AbstractItem self, AbstractPlacement placement, Transform src, FlingDirection dir = FlingDirection.Any)
    {
        if (self.IsObtained()) return false;

        GiveInfo info = new()
        {
            Container = Container.Chest,
            FlingType = FlingType.Everywhere,
            Transform = src,
            MessageType = MessageType.Corner
        };

        if (self.GiveEarly(Container.Chest)) self.Give(placement, info);
        else
        {
            var shiny = ShinyUtility.MakeNewShiny(placement, self, FlingType.Everywhere);
            var fsm = shiny.LocateMyFSM("Shiny Control");
            switch (dir)
            {
                case FlingDirection.Left: ShinyUtility.FlingShinyLeft(fsm); break;
                case FlingDirection.Right: ShinyUtility.FlingShinyRight(fsm); break;
                case FlingDirection.Down: ShinyUtility.FlingShinyDown(fsm); break;
                default: ShinyUtility.FlingShinyRandomly(fsm); break;
            }
        }
        return true;
    }
}
