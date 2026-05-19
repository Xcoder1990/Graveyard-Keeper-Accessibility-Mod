namespace AppleTreesEnhanced;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string PlayerGardenSection     = "── Player Garden ──";
    private const string HarvestingSection       = "── Harvesting ──";
    private const string WorldEnvironmentSection = "── World Environment ──";
    private const string EconomySection          = "── Economy ──";
    private const string AdvancedSection         = "── Advanced ──";
    private const string UpdatesSection          = "── Updates ──";

    internal static TimestampedLogger Log { get; private set; }

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;

    internal static ConfigEntry<bool> IncludeGardenBerryBushes { get; private set; }
    internal static ConfigEntry<bool> IncludeGardenTrees { get; private set; }
    internal static ConfigEntry<bool> IncludeWorldBerryBushes { get; private set; }
    internal static ConfigEntry<bool> ShowHarvestReadyMessages { get; private set; }
    internal static ConfigEntry<bool> RealisticHarvest { get; private set; }
    internal static ConfigEntry<bool> IncludeGardenBeeHives { get; private set; }
    internal static ConfigEntry<bool> BeeKeeperBuyback { get; private set; }
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
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 2);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        IncludeGardenBerryBushes = LocalizedConfig.Bind(Config, PlayerGardenSection, "Include Garden Berry Bushes", true, "include_garden_berry_bushes", order: 9);
        IncludeGardenTrees = LocalizedConfig.Bind(Config, PlayerGardenSection, "Include Garden Trees", true, "include_garden_trees", order: 8);
        IncludeGardenBeeHives = LocalizedConfig.Bind(Config, PlayerGardenSection, "Include Garden Bee Hives", false, "include_garden_bee_hives", order: 7);

        RealisticHarvest = LocalizedConfig.Bind(Config, HarvestingSection, "Realistic Harvest", true, "realistic_harvest", order: 6);
        ShowHarvestReadyMessages = LocalizedConfig.Bind(Config, HarvestingSection, "Show Harvest Ready Messages", true, "show_harvest_ready_messages", order: 5);

        IncludeWorldBerryBushes = LocalizedConfig.Bind(Config, WorldEnvironmentSection, "Include World Berry Bushes", false, "include_world_berry_bushes", order: 4);
        BeeKeeperBuyback = LocalizedConfig.Bind(Config, EconomySection, "Bee Keeper Buyback", false, "bee_keeper_buyback", order: 3);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 1);
    }

    internal static void CleanUpTrees()
    {
        if (!MainGame.game_started) return;
        Log.LogInfo($"Running CleanUpTrees as Player has spawned in.");
        ProcessDudBees();
        ProcessDudTrees();
        ProcessDudBushes();
        ProcessReadyObjects();
    }

    private static void ProcessDudBees()
    {
        var dudBees = FindObjectsOfType<WorldGameObject>(true)
            .Where(a => a.obj_id == Helpers.Constants.HarvestGrowing.BeeHouse).Where(b => b.progress <= 0)
            .Where(Helpers.IsPlayerBeeHive);

        var dudBeesCount = 0;
        foreach (var dudBee in dudBees)
        {
            dudBeesCount++;
            Helpers.ProcessBeeRespawn(dudBee);

            if (DebugEnabled)
            {
                Log.LogInfo($"Fixed DudBee {dudBeesCount}");
            }
        }
    }

    private static void ProcessDudTrees()
    {
        var dudTrees = FindObjectsOfType<WorldGameObject>(true)
            .Where(a => a.obj_id == Helpers.Constants.HarvestGrowing.GardenAppleTree).Where(b => b.progress <= 0);

        var dudTreeCount = 0;
        foreach (var dudTree in dudTrees)
        {
            dudTreeCount++;
            Helpers.ProcessRespawn(dudTree, Helpers.Constants.HarvestGrowing.GardenAppleTree,
                Helpers.Constants.HarvestSpawner.GardenAppleTree);

            if (DebugEnabled)
            {
                Log.LogInfo($"Fixed DudGardenTree {dudTreeCount}");
            }
        }
    }

    private static void ProcessDudBushes()
    {
        var dudBushes = FindObjectsOfType<WorldGameObject>(true)
            .Where(a => a.obj_id == Helpers.Constants.HarvestGrowing.GardenBerryBush).Where(b => b.progress <= 0);

        var dudBushCount = 0;
        foreach (var dudBush in dudBushes)
        {
            dudBushCount++;
            Helpers.ProcessRespawn(dudBush, Helpers.Constants.HarvestGrowing.GardenBerryBush,
                Helpers.Constants.HarvestSpawner.GardenBerryBush);

            if (DebugEnabled)
            {
                Log.LogInfo($"Fixed DudGardenBush {dudBushCount}");
            }
        }
    }

    private static void ProcessReadyObjects()
    {
        var readyBees = FindObjectsOfType<WorldGameObject>(true).Where(a => a.obj_id == Helpers.Constants.HarvestReady.BeeHouse)
            .Where(Helpers.IsPlayerBeeHive);
        var readyGardenTrees = FindObjectsOfType<WorldGameObject>(true).Where(a => a.obj_id == Helpers.Constants.HarvestReady.GardenAppleTree);
        var readyGardenBushes = FindObjectsOfType<WorldGameObject>(true).Where(a => a.obj_id == Helpers.Constants.HarvestReady.GardenBerryBush);
        var readyWorldBushes = FindObjectsOfType<WorldGameObject>(true).Where(a => Helpers.WorldReadyHarvests.Contains(a.obj_id));

        foreach (var item in readyBees)
        {
            Helpers.ProcessGardenBeeHive(item);
        }

        foreach (var item in readyGardenTrees)
        {
            Helpers.ProcessGardenAppleTree(item);
        }

        foreach (var item in readyGardenBushes)
        {
            Helpers.ProcessGardenBerryBush(item);
        }

        foreach (var item in readyWorldBushes)
        {
            switch (item.obj_id)
            {
                case Helpers.Constants.HarvestReady.WorldBerryBush1:
                    Helpers.ProcessBerryBush1(item);
                    break;

                case Helpers.Constants.HarvestReady.WorldBerryBush2:
                    Helpers.ProcessBerryBush2(item);
                    break;

                case Helpers.Constants.HarvestReady.WorldBerryBush3:
                    Helpers.ProcessBerryBush3(item);
                    break;
            }
        }
    }
}