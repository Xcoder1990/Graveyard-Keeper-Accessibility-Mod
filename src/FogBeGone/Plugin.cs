namespace FogBeGone;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string GeneralSection = "── General ──";
    private const string UpdatesSection = "── Updates ──";

    private static TimestampedLogger Log { get; set; }

    internal static ConfigEntry<bool> RemoveFog { get; private set; }
    internal static ConfigEntry<bool> RemoveWind { get; private set; }
    internal static ConfigEntry<bool> RemoveRain { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static bool RemoveFogCached;
    internal static bool RemoveWindCached;
    internal static bool RemoveRainCached;

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
        RemoveFog = LocalizedConfig.Bind(Config, GeneralSection, "Remove Fog", true, "remove_fog", order: 3);
        RemoveWind = LocalizedConfig.Bind(Config, GeneralSection, "Remove Wind", false, "remove_wind", order: 2);
        RemoveRain = LocalizedConfig.Bind(Config, GeneralSection, "Remove Rain", false, "remove_rain", order: 1);

        RemoveFogCached = RemoveFog.Value;
        RemoveWindCached = RemoveWind.Value;
        RemoveRainCached = RemoveRain.Value;

        RemoveFog.SettingChanged += (_, _) => RemoveFogCached = RemoveFog.Value;
        RemoveWind.SettingChanged += (_, _) => RemoveWindCached = RemoveWind.Value;
        RemoveRain.SettingChanged += (_, _) => RemoveRainCached = RemoveRain.Value;

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");
    }
}