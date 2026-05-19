namespace TreesNoMore;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection = "── Advanced ──";
    private const string TreesSection    = "── Trees ──";
    private const string StumpsSection   = "── Stumps ──";
    private const string ResetSection    = "── Reset ──";
    private const string UpdatesSection  = "── Updates ──";

    private static bool ShowConfirmationDialog { get; set; }
    internal static TimestampedLogger Log { get; private set; }

    internal static List<Tree> Trees { get; private set; } = [];

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<int> TreeSearchDistance { get; private set; }
    internal static ConfigEntry<bool> InstantStumpRemoval { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }
    private static string FilePath => Path.Combine(Application.persistentDataPath, $"{MainGame.me.save_slot.filename_no_extension}_trees.json");

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
        Application.quitting += SaveTrees;
    }


    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        TreeSearchDistance = LocalizedConfig.Bind(Config, TreesSection, "Tree Search Distance", 2, "tree_search_distance", order: 100);
        InstantStumpRemoval = LocalizedConfig.Bind(Config, StumpsSection, "Instant Stump Removal", true, "instant_stump_removal", order: 100);
        LocalizedConfig.Bind(Config, ResetSection, "Reset Trees", true, "reset_trees", order: 100,
            extra: a => { a.HideDefaultButton = true; a.CustomDrawer = RestoreTrees; });

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);
    }

    private static void RestoreTrees(ConfigEntryBase entry)
    {
        if (ShowConfirmationDialog)
        {
            GUILayout.Label(Lang.Get("ConfirmText"));
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(Lang.Get("Yes"), GUILayout.ExpandWidth(true)))
                {
                    if (DebugEnabled) Helpers.Log($"[Reset] User confirmed - clearing {Trees.Count} tracked tree(s) and deleting {FilePath}");
                    Trees.Clear();
                    File.Delete(FilePath);
                    ShowConfirmationDialog = false;
                }

                if (GUILayout.Button(Lang.Get("No"), GUILayout.ExpandWidth(true)))
                {
                    ShowConfirmationDialog = false;
                }
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button(Lang.Get("ResetButton"), GUILayout.ExpandWidth(true)))
            {
                ShowConfirmationDialog = true;
            }
        }
    }

    internal static bool LoadTrees()
    {
        if (MainGame.me.save_slot.linked_save == null)
        {
            if (DebugEnabled) Helpers.Log("[LoadTrees] save_slot.linked_save is null - nothing to load");
            return false;
        }

        if (!File.Exists(FilePath))
        {
            if (DebugEnabled) Helpers.Log($"[LoadTrees] no state file at {FilePath} - starting with empty tracked-tree list");
            return false;
        }
        var jsonString = File.ReadAllText(FilePath);
        Trees = JsonConvert.DeserializeObject<List<Tree>>(jsonString);
        if (DebugEnabled) Helpers.Log($"[LoadTrees] loaded {Trees.Count} tracked tree(s) from {FilePath}");
        return true;
    }

    internal static void SaveTrees()
    {
        if (MainGame.me.save_slot.linked_save == null)
        {
            if (DebugEnabled) Helpers.Log("[SaveTrees] save_slot.linked_save is null - skipping save");
            return;
        }
        var seen = new HashSet<Vector3>();
        var count = Trees.RemoveAll(x => !seen.Add(x.location));
        if (count > 0 && DebugEnabled)
        {
            Helpers.Log($"[SaveTrees] removed {count} duplicate tree entry/entries before write");
        }
        var jsonString = JsonConvert.SerializeObject(Trees, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        });

        File.WriteAllText(FilePath, jsonString);
        if (DebugEnabled) Helpers.Log($"[SaveTrees] wrote {Trees.Count} tracked tree(s) to {FilePath}");
    }

}
