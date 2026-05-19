namespace WheresMaVeggies;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection = "── Advanced ──";
    private const string HarvestSection  = "── Harvest ──";
    private const string UpdatesSection  = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;

    internal static ConfigEntry<bool> RequireFarmerPerk { get; private set; }
    internal static ConfigEntry<int> CascadeRadius { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static TimestampedLogger Log { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);

        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        RequireFarmerPerk = LocalizedConfig.Bind(Config, HarvestSection, "Require Farmer Perk", true, "require_farmer_perk", order: 100);

        CascadeRadius = LocalizedConfig.Bind(Config, HarvestSection, "Cascade Radius (tiles)", 1, "cascade_radius_tiles", new AcceptableValueRange<int>(1, 5), order: 90);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);

        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    internal static void WriteLog(string message, bool error = false)
    {
        if (error)
        {
            LogHelper.Error(message);
        }
        else
        {
            LogHelper.Info(message);
        }
    }

}
