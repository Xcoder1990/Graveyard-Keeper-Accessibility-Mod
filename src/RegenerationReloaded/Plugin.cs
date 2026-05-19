namespace RegenerationReloaded;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string RegenerationSection = "── Regeneration ──";
    private const string UpdatesSection      = "── Updates ──";

    private static TimestampedLogger Log { get; set; }
    internal static ConfigEntry<bool> ShowRegenUpdates { get; private set; }
    internal static ConfigEntry<float> LifeRegen { get; private set; }
    internal static ConfigEntry<float> EnergyRegen { get; private set; }
    internal static ConfigEntry<float> RegenDelay { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        ShowRegenUpdates = LocalizedConfig.Bind(Config, RegenerationSection, "Show Regeneration Updates", true, "show_regeneration_updates", order: 4);
        ShowRegenUpdates.SettingChanged += (_, _) => { Patches.ShowRegenUpdates = ShowRegenUpdates.Value; };
        LifeRegen = LocalizedConfig.Bind(Config, RegenerationSection, "Life Regeneration Rate", 2f, "life_regeneration_rate", new AcceptableValueRange<float>(0f, 10f), order: 3);
        LifeRegen.SettingChanged += (_, _) => { Patches.LifeRegen = LifeRegen.Value; };
        EnergyRegen = LocalizedConfig.Bind(Config, RegenerationSection, "Energy Regeneration Rate", 1f, "energy_regeneration_rate", new AcceptableValueRange<float>(0f, 10f), order: 2);
        EnergyRegen.SettingChanged += (_, _) => { Patches.EnergyRegen = EnergyRegen.Value; };
        RegenDelay = LocalizedConfig.Bind(Config, RegenerationSection, "Regeneration Delay", 5f, "regeneration_delay", new AcceptableValueRange<float>(0f, 10f), order: 1);
        RegenDelay.SettingChanged += (_, _) => { Patches.RegenDelay = RegenDelay.Value; };

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");
    }

}