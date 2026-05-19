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
        Lang.Init(Assembly.GetExecutingAssembly(), Log);

        Enabled = LocalizedConfig.Bind(Config, DumperSection, "Enabled", true, "enabled", order: 100);
        PrettyPrint = LocalizedConfig.Bind(Config, DumperSection, "Pretty Print", true, "pretty_print", order: 99);
        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);

        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }
}
