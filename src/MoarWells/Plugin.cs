namespace MoarWells;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal const string BasicWellCraftId = "mf_wood_builddesk::water_well_place";
    internal const string TemplateCraftId = "mf_wood_builddesk::well_pump_place";
    // "remove" in the id so QueueEverything's unsafe-id check leaves it alone.
    internal const string BasicWellRemoveCraftId = ":r:water_well_remove";
    // Beehouse remove takes a few seconds of holding - same feel we want for wells.
    internal const string RemoveTemplateCraftId = ":r:beehouse_1";
    internal const string BasicWellNameKey = "moarwells_basic_well";

    internal const string PumpWellCraftId = "mf_wood_builddesk::well_pump_buildable";
    internal const string PumpWellRemoveCraftId = ":r:well_pump_remove";
    internal const string PumpWellNameKey = "moarwells_pump_well";

    // Build costs. Basic well is half the flitch of vanilla's pump (which costs 8)
    // plus the same in stone. Pump well matches vanilla exactly.
    internal const int BasicWellFlitch = 4;
    internal const int BasicWellStone = 4;
    internal const int PumpWellFlitch = 8;
    internal const int PumpWellNails = 4;
    internal const int PumpWellDetail = 2;

    internal static TimestampedLogger Log { get; private set; }
    private static ConfigEntry<bool> CheckForUpdates { get; set; }
    internal static ConfigEntry<bool> RequireEngineerForPumpWell { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);

        CheckForUpdates = LocalizedConfig.Bind(Config, "── Updates ──", "Check for Updates", true, "check_for_updates");

        RequireEngineerForPumpWell = LocalizedConfig.Bind(Config, "── Unlocks ──", "Require Engineer Tech For Pump Well", true, "require_engineer_for_pump_well");

        UpdateChecker.Register(Info, CheckForUpdates);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }
}
