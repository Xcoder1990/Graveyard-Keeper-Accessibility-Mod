namespace BeamMeUpGerry;

[Harmony]
[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection        = "── Advanced ──";
    private const string FeaturesSection        = "── Features ──";
    private const string KeybindsSection        = "── Keybinds ──";
    private const string ControllerSection      = "── Controller ──";
    private const string LocationsSection       = "── Locations ──";
    private const string CustomLocationsSection = "── Custom Locations ──";
    private const string UpdatesSection         = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<bool> IncreaseMenuAnimationSpeed { get; private set; }
    internal static ConfigEntry<bool> EnableListExpansion { get; private set; }
    internal static ConfigEntry<bool> GerryAppears { get; private set; }
    internal static ConfigEntry<bool> GerryCharges { get; private set; }

    internal static ConfigEntry<bool> CinematicEffect { get; private set; }
    internal static ConfigEntry<bool> EnablePreviousPageChoices { get; private set; }
    internal static ConfigEntry<bool> PreviousPageChoiceAtTop { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> TeleportMenuKeyBind { get; set; }
    internal static ConfigEntry<string> TeleportMenuControllerButton { get; set; }
    internal static ConfigEntry<int> LocationsPerPage { get; private set; }
    internal static ConfigEntry<bool> SortAlphabetically { get; private set; }
    internal static ConfigEntry<bool> EnableCustomLocations { get; set; }
    internal static ConfigEntry<bool> RestrictToFoundLocations { get; set; }
    internal static ConfigEntry<bool> OpenNewLocationFileOnSave { get; private set; }
    internal static ConfigEntry<bool> CustomLocationMessage { get; set; }
    internal static ConfigEntry<KeyboardShortcut> SaveCustomLocationKeybind { get; set; }
    internal static ConfigEntry<KeyboardShortcut> ReloadCustomLocationsKeybind { get; set; }
    internal static ConfigEntry<string> SaveCustomLocationControllerButton { get; set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static TimestampedLogger Log { get; private set; }

    internal static Player CachedPlayer { get; set; }
    internal static Item CachedHearthstone { get; set; }

    private static ConfigFile ConfigInstance { get; set; }

    private void Awake()
    {
        ConfigInstance = Config;
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        InitInternalConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitInternalConfiguration()
    {
        CustomLocationMessage = Config.Bind(AdvancedSection, "Custom Location Shown", false,
            new ConfigDescription("Internal: tracks whether the one-time 'Custom Locations were enabled' notice has already been shown.", null,
                new ConfigurationManagerAttributes {Browsable = false, HideDefaultButton = true, IsAdvanced = true, ReadOnly = true, Order = 6}));

        LocalizedConfig.Bind(Config, AdvancedSection, "Print Known", false, "print_known", order: 5,
            extra: a => { a.CustomDrawer = PrintKnown; a.HideDefaultButton = true; });
    }
    private static void PrintKnown(ConfigEntryBase __obj)
    {
        var button = GUILayout.Button(Lang.Get("print_known_button"), GUILayout.ExpandWidth(true));
        if (button)
        {
            Log.LogInfo("\n");
            Log.LogInfo("Known NPC:");
            foreach (var npc in MainGame.me.save.known_npcs.npcs)
            {
                Log.LogInfo(npc.npc_id);
            }
            Log.LogInfo("\n");
            Log.LogInfo("Known Zones:");
            foreach (var zone in MainGame.me.save.known_world_zones)
            {
                Log.LogInfo(zone);
            }
            Log.LogInfo("\n");
            Log.LogInfo("One-Time Crafts:");
            foreach (var craft in MainGame.me.save.completed_one_time_crafts)
            {
                Log.LogInfo(craft);
            }
            Log.LogInfo("\n");
            Log.LogInfo("Unlocked Phrases:");
            foreach (var phrase in MainGame.me.save.unlocked_phrases)
            {
                Log.LogInfo(phrase);
            }
            Log.LogInfo("\n");
            Log.LogInfo("Blacklisted Phrases:");
            foreach (var phrase in MainGame.me.save.black_list_of_phrases)
            {
                Log.LogInfo(phrase);
            }
            Log.LogInfo("\n");
        }
    }

    internal static void InitConfiguration()
    {
        LocationLists.LoadCustomZones();

        Debug = LocalizedConfig.Bind(ConfigInstance, AdvancedSection, "Debug", false, "debug_logging", order: 803);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        IncreaseMenuAnimationSpeed = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Increase Menu Animation Speed", true, "increase_menu_animation_speed", order: 801);
        EnableListExpansion = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Enable List Expansion", true, "enable_list_expansion", order: 799);
        GerryAppears = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Gerry Appears", false, "gerry_appears", order: 798);
        CinematicEffect = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Cinematic Effect", true, "cinematic_effect", order: 797);
        GerryCharges = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Gerry Charges", false, "gerry_charges", order: 796);
        EnablePreviousPageChoices = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Enable Previous Page Choices", true, "enable_previous_page_choices", order: 795);
        PreviousPageChoiceAtTop = LocalizedConfig.Bind(ConfigInstance, FeaturesSection, "Previous Page Choice At Top", true, "previous_page_choice_at_top", order: 794, dispNamePrefix: "    └ ");

        TeleportMenuKeyBind = LocalizedConfig.Bind(ConfigInstance, KeybindsSection, "Teleport Menu Keybind", new KeyboardShortcut(KeyCode.Z), "teleport_menu_keybind", order: 796);
        TeleportMenuControllerButton = LocalizedConfig.Bind(ConfigInstance, ControllerSection, "Teleport Menu Controller Button", Enum.GetName(typeof(GamePadButton), GamePadButton.RB), "teleport_menu_controller_button", new AcceptableValueList<string>(Enum.GetNames(typeof(GamePadButton))), order: 795);

        var maxPages = Constants.MaxPages;
        var minPages = Mathf.CeilToInt(LocationLists.AllLocations.Count / (float) Constants.MaxPages);

        if (minPages >= Constants.MaxPages)
        {
            maxPages = minPages + 1;
        }

        Log.LogInfo($"Min Pages: {minPages} | Max Pages: {maxPages}");

        LocationsPerPage = LocalizedConfig.Bind(ConfigInstance, LocationsSection, "Locations Per Page", 8, "locations_per_page", new AcceptableValueRange<int>(minPages, maxPages), order: 794, extra: a => a.ShowRangeAsPercent = false);
        SortAlphabetically = LocalizedConfig.Bind(ConfigInstance, LocationsSection, "Sort Alphabetically", false, "sort_alphabetically", order: 793);
        RestrictToFoundLocations = LocalizedConfig.Bind(ConfigInstance, LocationsSection, "Restrict To Found Locations", true, "restrict_to_found_locations", order: 792);

        EnableCustomLocations = LocalizedConfig.Bind(ConfigInstance, CustomLocationsSection, "Enable Custom Locations", false, "enable_custom_locations", order: 792);
        SaveCustomLocationKeybind = LocalizedConfig.Bind(ConfigInstance, CustomLocationsSection, "Save Custom Location Keybind", new KeyboardShortcut(KeyCode.X), "save_custom_location_keybind", order: 791, dispNamePrefix: "    └ ");
        ReloadCustomLocationsKeybind = LocalizedConfig.Bind(ConfigInstance, CustomLocationsSection, "Reload Custom Locations Keybind", new KeyboardShortcut(KeyCode.X, KeyCode.LeftControl), "reload_custom_locations_keybind", order: 790, dispNamePrefix: "    └ ");
        SaveCustomLocationControllerButton = LocalizedConfig.Bind(ConfigInstance, CustomLocationsSection, "Save Custom Location Controller Button", Enum.GetName(typeof(GamePadButton), GamePadButton.None), "save_custom_location_controller_button", new AcceptableValueList<string>(Enum.GetNames(typeof(GamePadButton))), order: 789, dispNamePrefix: "    └ ");
        OpenNewLocationFileOnSave = LocalizedConfig.Bind(ConfigInstance, CustomLocationsSection, "Open New Location File On Save", true, "open_new_location_file_on_save", order: 788, dispNamePrefix: "    └ ");

        CheckForUpdates = LocalizedConfig.Bind(ConfigInstance, UpdatesSection, "Check for Updates", true, "check_for_updates");
    }

    internal static readonly Dictionary<string, ConfigEntry<bool>> LocationSettings = [];

    // Attrs are kept by english cfg key so we can rewrite DispName/Category in place
    // when the language changes and UpdateLists runs again. CM reads attrs by
    // reference at F1 open, so updating the same object is enough.
    private static readonly Dictionary<string, ConfigurationManagerAttributes> LocationAttrs = [];


    internal static void UpdateLists()
    {
        LocationLists.LoadCustomZones();

        // Capture each zone's localized display name in the user's current
        // language, indexed by the raw zone key (constant across languages).
        var localizedNames = new Dictionary<string, string>();
        foreach (var location in LocationLists.AllLocations)
        {
            localizedNames[location.zone] = Helpers.RemoveCharacters(location.zone);
        }

        Lang.Reload();
        var sectionCategory = Lang.Get("cfg.section.locations");
        var descTemplate = Lang.Get("cfg.location.toggle_desc");

        // Force English for the duration of the loop so the config key stays language-stable.
        // GJL.LoadLanguageResource handles the game's own SpeechText machinery; flipping _cur_lng
        // + Lang.Reload makes our SpeechBubble postfix return English from Lang.Get too.
        var originalLanguage = GameSettings._cur_lng;
        GJL.LoadLanguageResource("en");
        GameSettings._cur_lng = "en";
        Lang.Reload();

        foreach (var location in LocationLists.AllLocations.OrderByDescending(a => Helpers.RemoveCharacters(a.zone)))
        {
            var key = Helpers.RemoveCharacters(location.zone);
            var displayName = localizedNames.TryGetValue(location.zone, out var dn) ? dn : key;

            if (!LocationAttrs.TryGetValue(key, out var attrs))
            {
                attrs = new ConfigurationManagerAttributes();
                LocationAttrs[key] = attrs;
            }
            attrs.Category = sectionCategory;
            attrs.DispName = displayName;
            attrs.Description = string.Format(descTemplate, displayName);

            var configEntry = ConfigInstance.Bind(LocationsSection, key, true,
                new ConfigDescription(attrs.Description, null, attrs));
            location.enabled = configEntry.Value;
            configEntry.SettingChanged += (_, _) =>
            {
                LocationLists.CreatePages();
                LocationLists.AllLocations.Find(a => a.zone == location.zone).enabled = configEntry.Value;
            };

            LocationSettings[location.zone] = configEntry;
        }

        if (!originalLanguage.IsNullOrWhiteSpace())
        {
            GJL.LoadLanguageResource(originalLanguage);
            GameSettings._cur_lng = originalLanguage;
            Lang.Reload();
        }

        LocationLists.CreatePages();
    }
}
