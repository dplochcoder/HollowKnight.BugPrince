using BugPrince.Util;
using MenuChanger;
using MenuChanger.Extensions;
using MenuChanger.MenuElements;
using MenuChanger.MenuPanels;
using PurenailCore.SystemUtil;
using RandomizerMod.Menu;
using System;
using System.Collections.Generic;
using System.Linq;
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
    private GridItemPanel transitionSettings;
    private GridItemPanel relicSettings;
    private MenuItem<bool> enableCoinsAndGems;
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

    private const float VSPACE = 90;

    private ConnectionMenu(MenuPage connectionsPage)
    {
        MenuPage bugPrincePage = new("Bug Prince", connectionsPage);
        entryButton = new(connectionsPage, "Bug Prince");
        entryButton.AddHideAndShowEvent(bugPrincePage);

        factory = new(bugPrincePage, BugPrinceMod.GS.RandoSettings);
        factory.Elements.Select(e => e as MenuItem<bool>).Where(e => e != null).ForEach(e => e.SelfChanged += _ => UpdateColorsAndVisibility());

        var s = Settings;
        var enableTransitionChoices = (factory.ElementLookup[nameof(s.EnableTransitionChoices)] as MenuItem<bool>)!;
        enableCoinsAndGems = (factory.ElementLookup[nameof(s.EnableCoinsAndGems)] as MenuItem<bool>)!;

        transitionSettings = new(bugPrincePage, new(), 2, VSPACE, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<TransitionSettingAttribute>(factory)]);
        relicSettings = new(bugPrincePage, new(), 4, VSPACE, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<RelicSettingAttribute>(factory)]);
        costSettings = new(bugPrincePage, new(), 4, VSPACE, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<CostSettingAttribute>(factory)]);
        var mapShop = (factory.ElementLookup[nameof(s.MapShop)] as MenuItem<bool>)!;
        mapShop.SelfChanged += _ => UpdateColorsAndVisibility();
        mapShopSettings = new(bugPrincePage, new(), 3, VSPACE, SpaceParameters.HSPACE_MEDIUM, false, [.. GetElements<MapShopSettingAttribute>(factory)]);
        GridItemPanel locationSettings = new(bugPrincePage, new(), 3, VSPACE, SpaceParameters.HSPACE_SMALL, false, [.. GetElements<LocationSettingAttribute>(factory).Where(m => m != mapShop)]);

        VerticalItemPanel main = new(
            bugPrincePage, SpaceParameters.TOP_CENTER, VSPACE, true,
            [enableTransitionChoices, transitionSettings, relicSettings, enableCoinsAndGems, costSettings, mapShop, mapShopSettings, locationSettings]);
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
        transitionSettings.SetShown(Settings.EnableTransitionChoices);
        relicSettings.SetShown(Settings.EnableTransitionChoices);
        enableCoinsAndGems.SetShown(Settings.EnableTransitionChoices);
        costSettings.SetShown(RandoInterop.AreCostsEnabled);
        mapShopSettings.SetShown(Settings.MapShop);
    }

    internal void ApplySettings(RandomizationSettings settings)
    {
        factory.SetMenuValues(settings);
        UpdateColorsAndVisibility();
    }
}
