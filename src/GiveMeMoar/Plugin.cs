namespace GiveMeMoar;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection    = "── Advanced ──";
    private const string MultipliersSection = "── Multipliers ──";
    private const string CategoriesSection  = "── Categories ──";
    private const string CraftingSection    = "── Crafting ──";
    private const string UpdatesSection     = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;

    // ── Multipliers ──
    internal static ConfigEntry<int> ResourceMultiplier { get; private set; }
    internal static ConfigEntry<int> FaithMultiplier { get; private set; }
    internal static ConfigEntry<int> DonationMultiplier { get; private set; }
    internal static ConfigEntry<int> GratitudeMultiplier { get; private set; }
    internal static ConfigEntry<int> SinShardMultiplier { get; private set; }
    internal static ConfigEntry<int> HappinessMultiplier { get; private set; }
    internal static ConfigEntry<int> RedTechPointMultiplier { get; private set; }
    internal static ConfigEntry<int> GreenTechPointMultiplier { get; private set; }
    internal static ConfigEntry<int> BlueTechPointMultiplier { get; private set; }
    internal static ConfigEntry<int> WaterOutputMultiplier { get; private set; }
    internal static ConfigEntry<int> NuggetGoldMultiplier { get; private set; }

    // ── Categories ── (gates which item groups the Resource Multiplier touches)
    internal static ConfigEntry<bool> MultiplySticks { get; private set; }
    internal static ConfigEntry<bool> MultiplyCrops { get; private set; }
    internal static ConfigEntry<bool> MultiplySeeds { get; private set; }
    internal static ConfigEntry<bool> MultiplyLogs { get; private set; }
    internal static ConfigEntry<bool> MultiplyOres { get; private set; }
    internal static ConfigEntry<bool> MultiplyBugs { get; private set; }
    internal static ConfigEntry<bool> MultiplyEnemyDrops { get; private set; }
    internal static ConfigEntry<bool> MultiplyMisc { get; private set; }
    internal static ConfigEntry<bool> MultiplyBodyParts { get; private set; }

    // ── Crafting ── (craft-output scaling)
    internal static ConfigEntry<int>    CraftOutputMultiplier { get; private set; }
    internal static ConfigEntry<bool>   CraftExcludeToolsAndEquipment { get; private set; }
    internal static ConfigEntry<bool>   CraftExcludeProgressionCrafts { get; private set; }
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
                theirGuid: "codesprint.more_resouces",
                theirName: "Even More Resources",
                feature: Lang.Get("Conflict.EvenMoreResources.Feature"),
                severity: ConflictSeverity.Race,
                note: Lang.Get("Conflict.EvenMoreResources.Note")),
        });
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        ResourceMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Resource Multiplier", 1, "resource_multiplier", new AcceptableValueRange<int>(1, 50), order: 100);
        FaithMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Faith Multiplier", 1, "faith_multiplier", new AcceptableValueRange<int>(1, 50), order: 99);
        DonationMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Donation Multiplier", 1, "donation_multiplier", new AcceptableValueRange<int>(1, 50), order: 98);
        GratitudeMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Gratitude Multiplier", 1, "gratitude_multiplier", new AcceptableValueRange<int>(1, 50), order: 97);
        SinShardMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Sin Shard Multiplier", 1, "sin_shard_multiplier", new AcceptableValueRange<int>(1, 50), order: 96);
        HappinessMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Happiness Multiplier", 1, "happiness_multiplier", new AcceptableValueRange<int>(1, 50), order: 95);
        RedTechPointMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Red Tech Point Multiplier", 1, "red_tech_point_multiplier", new AcceptableValueRange<int>(1, 50), order: 94);
        GreenTechPointMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Green Tech Point Multiplier", 1, "green_tech_point_multiplier", new AcceptableValueRange<int>(1, 50), order: 93);
        BlueTechPointMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Blue Tech Point Multiplier", 1, "blue_tech_point_multiplier", new AcceptableValueRange<int>(1, 50), order: 92);
        WaterOutputMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Water Output Multiplier", 1, "water_output_multiplier", new AcceptableValueRange<int>(1, 50), order: 91);
        NuggetGoldMultiplier = LocalizedConfig.Bind(Config, MultipliersSection, "Gold Nugget Multiplier", 1, "gold_nugget_multiplier", new AcceptableValueRange<int>(1, 50), order: 90);

        MultiplySticks = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Sticks", true, "multiply_sticks", order: 100);
        MultiplyCrops = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Crops", true, "multiply_crops", order: 99);
        MultiplySeeds = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Seeds", false, "multiply_seeds", order: 98);
        MultiplyLogs = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Logs", true, "multiply_logs", order: 97);
        MultiplyOres = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Ores", true, "multiply_ores", order: 96);
        MultiplyBugs = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Bugs", true, "multiply_bugs", order: 95);
        MultiplyEnemyDrops = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Enemy Drops", true, "multiply_enemy_drops", order: 94);
        MultiplyMisc = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Miscellaneous", false, "multiply_miscellaneous", order: 93);
        MultiplyBodyParts = LocalizedConfig.Bind(Config, CategoriesSection, "Multiply Body Parts", false, "multiply_body_parts", order: 92);

        CraftOutputMultiplier = LocalizedConfig.Bind(Config, CraftingSection, "Craft Output Multiplier", 1, "craft_output_multiplier", new AcceptableValueRange<int>(1, 50), order: 100);
        CraftExcludeToolsAndEquipment = LocalizedConfig.Bind(Config, CraftingSection, "Exclude Tools And Equipment", true, "exclude_tools_and_equipment", order: 80);
        CraftExcludeProgressionCrafts = LocalizedConfig.Bind(Config, CraftingSection, "Exclude Progression Crafts", true, "exclude_progression_crafts", order: 79);

        // The craft multiplier reads live config values inside the runtime postfix,
        // so only the water_to_wgo path needs an explicit re-apply on setting change.
        WaterOutputMultiplier.SettingChanged += OnWaterToWgoSettingChanged;

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);
    }

    private static void OnWaterToWgoSettingChanged(object sender, EventArgs e)
    {
        Patches.RequestWaterToWgoReapply();
    }
}
