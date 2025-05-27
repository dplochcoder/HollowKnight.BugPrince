using RandoSettingsManager;
using RandoSettingsManager.SettingsManagement;
using RandoSettingsManager.SettingsManagement.Versioning;

namespace BugPrince.Rando;

internal class SettingsProxy : RandoSettingsProxy<RandomizationSettings, string>
{
    public static void Setup() => RandoSettingsManagerMod.Instance.RegisterConnection(new SettingsProxy());

    public override string ModKey => nameof(BugPrinceMod);

    public override VersioningPolicy<string> VersioningPolicy => new StrictModVersioningPolicy(BugPrinceMod.Instance!);

    public override bool TryProvideSettings(out RandomizationSettings? settings)
    {
        settings = BugPrinceMod.GS.RandoSettings;
        return settings.Enabled;
    }

    public override void ReceiveSettings(RandomizationSettings? settings) => ConnectionMenu.Instance!.ApplySettings(settings ?? new());
}
