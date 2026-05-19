namespace FasterCraftReloaded;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection   = "── Advanced ──";
    private const string SpeedSection      = "── Speed ──";
    private const string CompostingSection = "── Composting ──";
    private const string GardensSection    = "── Gardens ──";
    private const string WellsSection      = "── Wells ──";
    private const string ProductionSection = "── Zombie Production ──";
    private const string UpdatesSection    = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;

    internal static ConfigEntry<bool> IncreaseBuildAndDestroySpeed { get; private set; }
    internal static ConfigEntry<float> BuildAndDestroySpeed { get; private set; }
    internal static ConfigEntry<float> CraftSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyPlayerGardenSpeed { get; private set; }
    internal static ConfigEntry<float> PlayerGardenSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieGardenSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieGardenSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyRefugeeGardenSpeed { get; private set; }
    internal static ConfigEntry<float> RefugeeGardenSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieVineyardSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieVineyardSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieBrewerySpeed { get; private set; }
    internal static ConfigEntry<float> ZombieBrewerySpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieWinemakingSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieWinemakingSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieCraftingTableSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieCraftingTableSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieSawmillSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieSawmillSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyZombieMinesSpeed { get; private set; }
    internal static ConfigEntry<float> ZombieMinesSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyCompostSpeed { get; private set; }
    internal static ConfigEntry<float> CompostSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> ModifyWaterPumpSpeed { get; private set; }
    internal static ConfigEntry<float> WaterPumpSpeedMultiplier { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static TimestampedLogger Log { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        ConflictWarningRegistry.Register(MyPluginInfo.PLUGIN_NAME, () => new[]
        {
            new ConflictEntry(
                theirGuid: "com.bongk.craftspeedcontroller",
                theirName: "Craft Speed Controller",
                feature: Lang.Get("Conflict.CraftSpeedController.Feature"),
                severity: ConflictSeverity.Race,
                note: Lang.Get("Conflict.CraftSpeedController.Note")),
        });
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        CraftSpeedMultiplier = LocalizedConfig.Bind(Config, SpeedSection, "Craft Speed Multiplier", 2f, "craft_speed_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 100);

        IncreaseBuildAndDestroySpeed = LocalizedConfig.Bind(Config, SpeedSection, "Faster Build And Destroy", true, "faster_build_and_destroy", order: 99);
        BuildAndDestroySpeed = LocalizedConfig.Bind(Config, SpeedSection, "Build And Destroy Speed", 4f, "build_and_destroy_speed", new AcceptableValueRange<float>(2f, 10f), order: 98, dispNamePrefix: "    └ ");

        ModifyCompostSpeed = LocalizedConfig.Bind(Config, CompostingSection, "Speed Up Composting", false, "speed_up_composting", order: 100);
        CompostSpeedMultiplier = LocalizedConfig.Bind(Config, CompostingSection, "Composting Multiplier", 2f, "composting_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 99, dispNamePrefix: "    └ ");

        ModifyPlayerGardenSpeed = LocalizedConfig.Bind(Config, GardensSection, "Speed Up Your Garden", false, "speed_up_your_garden", order: 100);
        PlayerGardenSpeedMultiplier = LocalizedConfig.Bind(Config, GardensSection, "Your Garden Multiplier", 2f, "your_garden_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 99, dispNamePrefix: "    └ ");

        ModifyRefugeeGardenSpeed = LocalizedConfig.Bind(Config, GardensSection, "Speed Up Refugee Garden", false, "speed_up_refugee_garden", order: 98);
        RefugeeGardenSpeedMultiplier = LocalizedConfig.Bind(Config, GardensSection, "Refugee Garden Multiplier", 2f, "refugee_garden_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 97, dispNamePrefix: "    └ ");

        ModifyZombieGardenSpeed = LocalizedConfig.Bind(Config, GardensSection, "Speed Up Zombie Garden", false, "speed_up_zombie_garden", order: 96);
        ZombieGardenSpeedMultiplier = LocalizedConfig.Bind(Config, GardensSection, "Zombie Garden Multiplier", 2f, "zombie_garden_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 95, dispNamePrefix: "    └ ");

        ModifyWaterPumpSpeed = LocalizedConfig.Bind(Config, WellsSection, "Speed Up Water Pump", false, "speed_up_water_pump", order: 100);
        WaterPumpSpeedMultiplier = LocalizedConfig.Bind(Config, WellsSection, "Water Pump Multiplier", 2f, "water_pump_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 99, dispNamePrefix: "    └ ");

        ModifyZombieMinesSpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Mines", false, "speed_up_zombie_mines", order: 100);
        ZombieMinesSpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Mines Multiplier", 2f, "zombie_mines_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 99, dispNamePrefix: "    └ ");

        ModifyZombieSawmillSpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Sawmill", false, "speed_up_zombie_sawmill", order: 98);
        ZombieSawmillSpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Sawmill Multiplier", 2f, "zombie_sawmill_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 97, dispNamePrefix: "    └ ");

        ModifyZombieVineyardSpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Vineyard", false, "speed_up_zombie_vineyard", order: 96);
        ZombieVineyardSpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Vineyard Multiplier", 2f, "zombie_vineyard_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 95, dispNamePrefix: "    └ ");

        ModifyZombieBrewerySpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Brewery", false, "speed_up_zombie_brewery", order: 94);
        ZombieBrewerySpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Brewery Multiplier", 2f, "zombie_brewery_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 93, dispNamePrefix: "    └ ");

        ModifyZombieWinemakingSpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Winemaking", false, "speed_up_zombie_winemaking", order: 92);
        ZombieWinemakingSpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Winemaking Multiplier", 2f, "zombie_winemaking_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 91, dispNamePrefix: "    └ ");

        ModifyZombieCraftingTableSpeed = LocalizedConfig.Bind(Config, ProductionSection, "Speed Up Zombie Crafting Table", false, "speed_up_zombie_crafting_table", order: 90);
        ZombieCraftingTableSpeedMultiplier = LocalizedConfig.Bind(Config, ProductionSection, "Zombie Crafting Table Multiplier", 2f, "zombie_crafting_table_multiplier", new AcceptableValueRange<float>(1f, 50f), order: 89, dispNamePrefix: "    └ ");

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);
    }
}
