namespace WheresMaPoints;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string UserInterfaceSection = "── User Interface ──";
    private const string VisualFeedbackSection = "── Visual Feedback ──";
    private const string AudioFeedbackSection = "── Audio Feedback ──";
    private const string UpdatesSection = "── Updates ──";

    internal static ConfigEntry<bool> ShowPointGainAboveKeeper { get; private set; }
    internal static ConfigEntry<bool> StillPlayCollectAudio { get; private set; }
    internal static ConfigEntry<bool> AlwaysShowXpBar { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }
    private static TimestampedLogger Log { get; set; }

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
        AlwaysShowXpBar = LocalizedConfig.Bind(Config, UserInterfaceSection, "Always Show XP Bar", true, "always_show_xp_bar", order: 6);
        ShowPointGainAboveKeeper = LocalizedConfig.Bind(Config, VisualFeedbackSection, "Show Point Gain Above Keeper", true, "show_point_gain_above_keeper", order: 5);
        StillPlayCollectAudio = LocalizedConfig.Bind(Config, AudioFeedbackSection, "Still Play Collect Audio", false, "still_play_collect_audio", order: 4);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");
    }

}