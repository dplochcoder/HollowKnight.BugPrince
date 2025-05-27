using BugPrince.Util;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using PurenailCore.SystemUtil;
using RandomizerMod.Menu;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BugPrince.Rando;

internal class ConnectionMenu
{
    internal static ConnectionMenu? Instance { get; private set; }

    private static RandomizationSettings Settings => BugPrinceMod.GS.RandoSettings;

    internal static void Setup()
    {
        RandomizerMenuAPI.AddMenuPage(OnRandomizerMenuConstruction, TryGetMenuButton);
        MenuChangerMod.OnExitMainMenu += () => Instance = null;
    }

    private readonly SmallButton entryButton;
    private readonly MenuElementFactory<RandomizationSettings> factory;
    private readonly List<ILockable> coreLockables = [];
    private readonly List<ILockable> costLockables = [];

    private static List<IValueElement> GetElements<T>(MenuElementFactory<RandomizationSettings> factory) where T : Attribute
    {
        List<IValueElement> ret = [];
        foreach (var e in factory.ElementLookup)
        {
            var name = e.Key;
            if (typeof(RandomizationSettings).GetField(name).GetCustomAttribute<T>() != null) ret.Add(e.Value);
        }
        return ret;
    }

    private ConnectionMenu(MenuPage connectionsPage)
    {
        MenuPage bugPrincePage = new("Bug Prince", connectionsPage);
        entryButton = new(connectionsPage, "Bug Prince");
        entryButton.AddHideAndShowEvent(bugPrincePage);

        factory = new(bugPrincePage, BugPrinceMod.GS.RandoSettings);
        foreach (var e in factory.ElementLookup)
        {
            if (e.Key == nameof(Settings.Enabled) || e.Value is not ILockable l) continue;
                
            if (typeof(RandomizationSettings).GetField(e.Key).GetCustomAttribute<CostFieldAttribute>() != null) costLockables.Add(l);
            else coreLockables.Add(l);
        }

        var s = Settings;
        var enabled = (factory.ElementLookup[nameof(s.Enabled)] as MenuItem<bool>)!;
        enabled.SelfChanged += _ => SetLocksAndColors();
        MenuLabel coinsLabel = new(bugPrincePage, "Coins", MenuLabel.Style.Body);
        var costsEnabled = (factory.ElementLookup[nameof(s.CostsEnabled)] as MenuItem<bool>)!;
        costsEnabled.SelfChanged += _ => SetLocksAndColors();
        MenuLabel gemsLabel = new(bugPrincePage, "Gems", MenuLabel.Style.Body);

        GridItemPanel mainSettings = new(bugPrincePage, new(), 2, SpaceParameters.VSPACE_MEDIUM, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<MainSettingAttribute>(factory)]);
        GridItemPanel costSettingsHeader = new(bugPrincePage, new(), 3, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_MEDIUM, false, [coinsLabel, costsEnabled, gemsLabel]);
        GridItemPanel costSettings = new(bugPrincePage, new(), 4, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<CostFieldAttribute>(factory)]);
        GridItemPanel locationSettings = new(bugPrincePage, new(), 3, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<LocationFieldAttribute>(factory)]);

        List<GridItemPanel> order = [mainSettings, costSettingsHeader, costSettings, locationSettings];
        VerticalItemPanel main = new(bugPrincePage, SpaceParameters.TOP_CENTER_UNDER_TITLE, SpaceParameters.VSPACE_MEDIUM, true, [enabled, .. order]);
        main.Reposition();
        for (int i = 1; i < order.Count; i++)
        {
            // Give more space for the 2-row grid.
            order[i].Translate(new(0, -SpaceParameters.VSPACE_MEDIUM));
            order[i].Reposition();
        }

        SetLocksAndColors();
    }

    internal static void OnRandomizerMenuConstruction(MenuPage page) => Instance = new(page);
    
    public static bool TryGetMenuButton(MenuPage page, out SmallButton button)
    {
        button = Instance!.entryButton;
        return true;
    }

    private IEnumerable<ILockable> AllLockables()
    {
        foreach (var lockable in coreLockables) yield return lockable;
        foreach (var lockable in costLockables) yield return lockable;
    }

    private void SetLocksAndColors()
    {
        entryButton.Text.color = Settings.Enabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        coreLockables.ForEach(l => l.SetUnlocked(Settings.Enabled));
        costLockables.ForEach(l => l.SetUnlocked(Settings.Enabled && Settings.CostsEnabled));
    }

    internal void ApplySettings(RandomizationSettings settings)
    {
        AllLockables().ForEach(l => l.Unlock());
        factory.SetMenuValues(settings);
        SetLocksAndColors();
    }
}
