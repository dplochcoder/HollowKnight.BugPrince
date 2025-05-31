using BugPrince.Util;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
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
    private GridItemPanel relicSettings;
    private GridItemPanel costSettingsHeader;
    private GridItemPanel costSettings;

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

        var s = Settings;
        var enabled = (factory.ElementLookup[nameof(s.EnableTransitionChoices)] as MenuItem<bool>)!;
        enabled.SelfChanged += _ => UpdateColorsAndVisibility();
        var costsEnabled = (factory.ElementLookup[nameof(s.CostsEnabled)] as MenuItem<bool>)!;
        costsEnabled.SelfChanged += _ => UpdateColorsAndVisibility();

        GridItemPanel mainSettings = new(bugPrincePage, new(), 2, SpaceParameters.VSPACE_MEDIUM, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<TransitionSettingAttribute>(factory)]);
        relicSettings = new(bugPrincePage, new(), 4, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<RelicSettingAttribute>(factory)]);
        costSettingsHeader = new(bugPrincePage, new(), 3, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_MEDIUM, false, [costsEnabled]);
        costSettings = new(bugPrincePage, new(), 4, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<CostSettingAttribute>(factory)]);
        GridItemPanel locationSettings = new(bugPrincePage, new(), 3, SpaceParameters.VSPACE_SMALL, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<LocationSettingAttribute>(factory)]);

        List<GridItemPanel> order = [mainSettings, costSettingsHeader, costSettings, locationSettings];
        VerticalItemPanel main = new(bugPrincePage, SpaceParameters.TOP_CENTER_UNDER_TITLE, SpaceParameters.VSPACE_MEDIUM, true, [enabled, .. order]);
        main.Reposition();
        for (int i = 1; i < order.Count; i++)
        {
            // Give more space for the 2-row grid.
            order[i].Translate(new(0, -SpaceParameters.VSPACE_MEDIUM));
            order[i].Reposition();
        }

        UpdateColorsAndVisibility();
    }

    internal static void OnRandomizerMenuConstruction(MenuPage page) => Instance = new(page);
    
    public static bool TryGetMenuButton(MenuPage page, out SmallButton button)
    {
        button = Instance!.entryButton;
        return true;
    }

    private void UpdateColorsAndVisibility()
    {
        entryButton.Text.color = Settings.EnableTransitionChoices ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        relicSettings.SetShown(Settings.EnableTransitionChoices);
        costSettingsHeader.SetShown(Settings.EnableTransitionChoices);
        costSettings.SetShown(RandoInterop.AreCostsEnabled);
    }

    internal void ApplySettings(RandomizationSettings settings)
    {
        factory.SetMenuValues(settings);
        UpdateColorsAndVisibility();
    }
}
