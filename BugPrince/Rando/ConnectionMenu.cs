using BugPrince.Util;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using RandomizerMod.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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
    private GridItemPanel mainSettings;
    private GridItemPanel relicSettings;
    private MenuItem<bool> costsEnabled;
    private GridItemPanel costSettings;
    private GridItemPanel mapShopSettings;

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

    private const float VSPACE_MEDIUM_LARGE = 100;

    private ConnectionMenu(MenuPage connectionsPage)
    {
        MenuPage bugPrincePage = new("Bug Prince", connectionsPage);
        entryButton = new(connectionsPage, "Bug Prince");
        entryButton.AddHideAndShowEvent(bugPrincePage);

        factory = new(bugPrincePage, BugPrinceMod.GS.RandoSettings);

        var s = Settings;
        var enabled = (factory.ElementLookup[nameof(s.EnableTransitionChoices)] as MenuItem<bool>)!;
        enabled.SelfChanged += _ => UpdateColorsAndVisibility();
        costsEnabled = (factory.ElementLookup[nameof(s.EnableCoinsAndGems)] as MenuItem<bool>)!;
        costsEnabled.SelfChanged += _ => UpdateColorsAndVisibility();

        mainSettings = new(bugPrincePage, new(), 2, VSPACE_MEDIUM_LARGE, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<TransitionSettingAttribute>(factory)]);
        relicSettings = new(bugPrincePage, new(), 4, VSPACE_MEDIUM_LARGE, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<RelicSettingAttribute>(factory)]);
        costSettings = new(bugPrincePage, new(), 4, VSPACE_MEDIUM_LARGE, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<CostSettingAttribute>(factory)]);
        var mapShopField = (factory.ElementLookup[nameof(s.MapShop)] as MenuItem<bool>)!;
        mapShopField.SelfChanged += _ => UpdateColorsAndVisibility();
        mapShopSettings = new(bugPrincePage, new(), 3, VSPACE_MEDIUM_LARGE, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<MapShopSettingAttribute>(factory)]);
        GridItemPanel locationSettings = new(bugPrincePage, new(), 3, VSPACE_MEDIUM_LARGE, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<LocationSettingAttribute>(factory).Where(m => m != mapShopField)]);

        VerticalItemPanel main = new(
            bugPrincePage, SpaceParameters.TOP_CENTER_UNDER_TITLE, VSPACE_MEDIUM_LARGE, true,
            [enabled, mainSettings, relicSettings, costsEnabled, costSettings, mapShopField, mapShopSettings, locationSettings]);
        main.Reposition();

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
        entryButton.Text.color = RandoInterop.IsEnabled ? Colors.TRUE_COLOR : Colors.DEFAULT_COLOR;
        mainSettings.SetShown(Settings.EnableTransitionChoices);
        relicSettings.SetShown(Settings.EnableTransitionChoices);
        costsEnabled.SetShown(Settings.EnableTransitionChoices);
        costSettings.SetShown(RandoInterop.AreCostsEnabled);
        mapShopSettings.SetShown(Settings.MapShop);
    }

    internal void ApplySettings(RandomizationSettings settings)
    {
        factory.SetMenuValues(settings);
        UpdateColorsAndVisibility();
    }
}
