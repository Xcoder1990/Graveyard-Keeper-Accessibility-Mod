namespace GameBalanceDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string DumperSection  = "── Dumper ──";
    private const string UpdatesSection = "── Updates ──";

    internal static TimestampedLogger Log { get; private set; }

    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<bool> PrettyPrint { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;

        Enabled = Config.Bind(DumperSection, "Enabled", true,
            new ConfigDescription("Master switch. When on, the entire vanilla GameBalance is dumped to BepInEx/plugins/GameBalanceDumper/dump/ on game start.", null,
                new ConfigurationManagerAttributes { Order = 100 }));

        PrettyPrint = Config.Bind(DumperSection, "Pretty Print", true,
            new ConfigDescription("On: indent the JSON for readability. Off: minified single-line output (smaller files, faster to write).", null,
                new ConfigurationManagerAttributes { Order = 99 }));

        CheckForUpdates = Config.Bind(UpdatesSection, "Check for Updates", true,
            new ConfigDescription("Show a notice on the main menu when a newer version of this mod is available on NexusMods. Click the notice to open the mod's page.", null,
                new ConfigurationManagerAttributes { Order = 100 }));

        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }
}
