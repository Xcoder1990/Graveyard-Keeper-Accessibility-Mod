namespace PrayTheDayAway;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection      = "── Advanced ──";
    private const string GeneralSection       = "── General ──";
    private const string ModeSection          = "── Mode ──";
    private const string NotificationsSection = "── Notifications ──";
    private const string UpgradesSection      = "── Upgrades ──";
    private const string SpeedSection         = "── Speed ──";
    private const string CheatsSection        = "── Cheats ──";
    private const string UpdatesSection       = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static TimestampedLogger Log { get; private set; }

    internal static ConfigEntry<bool> EverydayIsSermonDay { get; private set; }
    internal static ConfigEntry<bool> SermonOverAndOver { get; private set; }
    internal static ConfigEntry<bool> NotifyOnPrayerLoss { get; private set; }
    internal static ConfigEntry<bool> AlternateMode { get; private set; }
    internal static ConfigEntry<bool> NoLossOnDailySermons { get; private set; }
    internal static ConfigEntry<bool> RandomlyUpgradeBasicPrayer { get; private set; }
    internal static ConfigEntry<bool> SpeedUpSermon { get; private set; }
    internal static ConfigEntry<int> SermonSpeed { get; private set; }
    internal static ConfigEntry<bool> CheatModeConfig { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 597);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        EverydayIsSermonDay = LocalizedConfig.Bind(Config, GeneralSection, "Everyday Is Sermon Day", true, "everyday_is_sermon_day", order: 606);
        SermonOverAndOver = LocalizedConfig.Bind(Config, GeneralSection, "Sermon Over And Over", false, "sermon_over_and_over", order: 605);
        AlternateMode = LocalizedConfig.Bind(Config, ModeSection, "Alternate Mode", true, "alternate_mode", order: 603);
        NoLossOnDailySermons = LocalizedConfig.Bind(Config, ModeSection, "No Loss On Daily Sermons", false, "no_loss_on_daily_sermons", order: 602, dispNamePrefix: "    └ ");
        NotifyOnPrayerLoss = LocalizedConfig.Bind(Config, NotificationsSection, "Notify On Prayer Loss", true, "notify_on_prayer_loss", order: 602);
        RandomlyUpgradeBasicPrayer = LocalizedConfig.Bind(Config, UpgradesSection, "Randomly Upgrade Basic Prayer", true, "randomly_upgrade_basic_prayer", order: 601);
        SpeedUpSermon = LocalizedConfig.Bind(Config, SpeedSection, "Speed Up Sermon", false, "speed_up_sermon", order: 600);
        SermonSpeed = LocalizedConfig.Bind(Config, SpeedSection, "Sermon Speed", 5, "sermon_speed", new AcceptableValueRange<int>(2, 10), order: 599, dispNamePrefix: "    └ ");
        CheatModeConfig = LocalizedConfig.Bind(Config, CheatsSection, "Cheat Mode", false, "cheat_mode", order: 598);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");
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
