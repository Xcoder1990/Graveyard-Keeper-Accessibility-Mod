namespace BringOutYerDead;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection       = "── Advanced ──";
    private const string DeliveryTimesSection  = "── Delivery Times ──";
    private const string DonkeySection         = "── Donkey ──";
    private const string UpdatesSection        = "── Updates ──";
    private const string InternalSection       = "Internal (Dont Touch)";

    internal static ConfigEntry<bool> Debug;
    internal static bool DebugEnabled;
    internal static TimestampedLogger Log { get; private set; }

    internal static bool PrideDayLogged { get; set; }
    internal static WorldGameObject Donkey { get; set; }

    internal static ConfigEntry<bool> MorningDelivery { get; set; }
    internal static ConfigEntry<bool> DayDelivery { get; set; }
    internal static ConfigEntry<bool> NightDelivery { get; set; }
    internal static ConfigEntry<bool> EveningDelivery { get; set; }
    internal static ConfigEntry<int> DonkeySpeed { get; private set; }

    internal static ConfigEntry<bool> InternalMorningDelivery { get; private set; }
    internal static ConfigEntry<bool> InternalDayDelivery { get; private set; }
    internal static ConfigEntry<bool> InternalEveningDelivery { get; private set; }
    internal static ConfigEntry<bool> InternalNightDelivery { get; private set; }
    internal static ConfigEntry<bool> InternalDonkeySpawned { get; private set; }
    internal static ConfigEntry<bool> InternalTutMessageShown { get;  set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        InitInternalConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
        if (DebugEnabled)
        {
            Log.LogInfo($"[Init] {MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded. DonkeySpeed={DonkeySpeed.Value}, Morning={MorningDelivery.Value}, Day={DayDelivery.Value}, Evening={EveningDelivery.Value}, Night={NightDelivery.Value}");
        }
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 1);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        MorningDelivery = LocalizedConfig.Bind(Config, DeliveryTimesSection, "Morning Delivery", true, "morning_delivery", order: 6);
        DayDelivery = LocalizedConfig.Bind(Config, DeliveryTimesSection, "Day Delivery", false, "day_delivery", order: 5);
        EveningDelivery = LocalizedConfig.Bind(Config, DeliveryTimesSection, "Evening Delivery", true, "evening_delivery", order: 4);
        NightDelivery = LocalizedConfig.Bind(Config, DeliveryTimesSection, "Night Delivery", false, "night_delivery", order: 3);

        DonkeySpeed = LocalizedConfig.Bind(Config, DonkeySection, "Donkey Speed", 2, "donkey_speed", new AcceptableValueRange<int>(2, 20), order: 2);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 1);
    }

    private void InitInternalConfiguration()
    {
        InternalMorningDelivery = Config.Bind(InternalSection, "Morning Delivery Done", false, new ConfigDescription("Internal use. Used for tracking a days delivery state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 6}));
        InternalDayDelivery = Config.Bind(InternalSection, "Day Delivery Done", false, new ConfigDescription("Internal use. Used for tracking a days delivery state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 5}));
        InternalEveningDelivery = Config.Bind(InternalSection, "Evening Delivery Done", false, new ConfigDescription("Internal use. Used for tracking a days delivery state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 4}));
        InternalNightDelivery = Config.Bind(InternalSection, "Night Delivery Done", false, new ConfigDescription("Internal use. Used for tracking a days delivery state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 3}));
        InternalDonkeySpawned = Config.Bind(InternalSection, "Donkey Spawned Done", false, new ConfigDescription("Internal use. Used for tracking donkey spawn state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 2}));
        InternalTutMessageShown = Config.Bind(InternalSection, "Tut Message Shown", false, new ConfigDescription("Internal use. Used for tracking tutorial message state.", null, new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 1}));
    }
}
