namespace IBuildWhereIWant;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection   = "── Advanced ──";
    private const string CollisionSection  = "── Collision ──";
    private const string DisplaySection    = "── Display ──";
    private const string KeybindsSection   = "── Keybinds ──";
    private const string ControllerSection = "── Controller ──";
    private const string UpdatesSection    = "── Updates ──";

    private const string Zone = "mf_wood";
    private const string BuildDeskConst = "buildanywhere_desk";

    internal static TimestampedLogger Log { get; private set; }
    internal static ConfigEntry<bool> Grid { get; private set; }
    internal static ConfigEntry<bool> GreyOverlay { get; private set; }
    internal static ConfigEntry<bool> BuildingCollision { get; private set; }
    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<KeyboardShortcut> MenuKeyBind { get; private set; }
    internal static ConfigEntry<string> MenuControllerButton { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static WorldGameObject BuildDesk { get; set; }
    internal static WorldGameObject BuildDeskClone { get; set; }
    internal static CraftsInventory CraftsInventory { get; set; }
    internal static Dictionary<string, string> CraftDictionary { get; set; } = new();
    internal static bool CraftAnywhere { get; set; }
    internal static string ZoneId => Zone;
    internal static string BuildDeskName => BuildDeskConst;

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
                theirGuid: "PandaModding.gyk.moveobjects",
                theirName: "Move Objects (No Rebuilding)",
                feature: Lang.Get("Conflict.MoveObjects.Feature"),
                severity: ConflictSeverity.Hint,
                note: Lang.Get("Conflict.MoveObjects.Note")),
        });
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 599);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        BuildingCollision = LocalizedConfig.Bind(Config, CollisionSection, "Building Collision", true, "building_collision", order: 604);
        Grid = LocalizedConfig.Bind(Config, DisplaySection, "Grid", false, "grid", order: 603);
        GreyOverlay = LocalizedConfig.Bind(Config, DisplaySection, "Grey Overlay", false, "grey_overlay", order: 602);
        MenuKeyBind = LocalizedConfig.Bind(Config, KeybindsSection, "Menu Key Bind", new KeyboardShortcut(KeyCode.Q), "menu_key_bind", order: 601);
        MenuControllerButton = LocalizedConfig.Bind(Config, ControllerSection, "Menu Controller Button", Enum.GetName(typeof(GamePadButton), GamePadButton.LB), "menu_controller_button", new AcceptableValueList<string>(Enum.GetNames(typeof(GamePadButton))), order: 600);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");
    }

    internal static bool CanOpenCraftAnywhere()
    {
        return MainGame.game_started && !MainGame.me.player.is_dead && !MainGame.me.player.IsDisabled() &&
               !MainGame.paused && BaseGUI.all_guis_closed &&
               !MainGame.me.player.GetMyWorldZoneId().Contains("refugee");
    }

    internal static void OpenCraftAnywhere()
    {
        if (MainGame.me.player.GetMyWorldZoneId().Contains("refugee")) return;
        if (MainGame.me.player.GetParamInt("in_tutorial") == 1 &&
            MainGame.me.player.GetParamInt("tut_shown_tut_1") == 0)
        {
            MainGame.me.player.Say("cant_do_it_now");
            return;
        }

        CraftsInventory ??= new CraftsInventory();

        CraftDictionary ??= new Dictionary<string, string>();

        if (BuildDesk == null)
        {
            BuildDesk = FindObjectsOfType<WorldGameObject>(true)
                .FirstOrDefault(x => string.Equals(x.obj_id, "mf_wood_builddesk"));
        }

        WriteLog(
            BuildDesk != null
                ? $"Found Build Desk: {BuildDesk}, Zone: {BuildDesk.GetMyWorldZone()}"
                : "Unable to locate a build desk.", BuildDesk == null);

        if (BuildDeskClone != null)
        {
            Destroy(BuildDeskClone);
        }

        BuildDeskClone = Instantiate(BuildDesk);

        BuildDeskClone.name = BuildDeskConst;

        // Rebuild the craft list only when the visible set actually changed.
        // Keyed by craft.id (unique) -> display name (used for sorting). Keying by display
        // name silently dropped distinct crafts that happened to localize to the same string,
        // e.g. multiple mods adding their own "Pallet".
        var freshDict = new Dictionary<string, string>();
        foreach (var craft in GameBalance.me.craft_obj_data
                     .Where(x => x.build_type == ObjectCraftDefinition.BuildType.Put)
                     .Where(a => a.icon.Length > 0)
                     .Where(b => !b.id.Contains("refugee"))
                     .Where(d => MainGame.me.save.IsCraftVisible(d)))
        {
            if (!freshDict.ContainsKey(craft.id))
            {
                freshDict.Add(craft.id, GJL.L(craft.GetNameNonLocalized()));
            }
        }

        var unchanged = CraftsInventory != null
                        && freshDict.Count == CraftDictionary.Count
                        && freshDict.All(kv => CraftDictionary.TryGetValue(kv.Key, out var name) && name == kv.Value);

        if (!unchanged)
        {
            CraftDictionary = freshDict;
            CraftsInventory = new CraftsInventory();

            var craftList = CraftDictionary.ToList();
            craftList.Sort((pair1, pair2) => string.CompareOrdinal(pair1.Value, pair2.Value));

            foreach (var craft in craftList)
            {
                CraftsInventory.AddCraft(craft.Key);
            }
        }

        CraftAnywhere = true;

        BuildModeLogics.last_build_desk = BuildDeskClone;

        MainGame.me.build_mode_logics.SetCurrentBuildZone(BuildDeskClone.obj_def.zone_id, "");
        GUIElements.me.craft.OpenAsBuild(BuildDeskClone, CraftsInventory);
        MainGame.paused = false;
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
