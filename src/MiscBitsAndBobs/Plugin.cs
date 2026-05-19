namespace MiscBitsAndBobs;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection = "── Advanced ──";
    private const string AudioSection    = "── Audio ──";
    private const string UISection       = "── UI ──";
    private const string GameplaySection = "── Gameplay ──";
    private const string MovementSection = "── Movement ──";
    private const string ChurchSection   = "── Church ──";
    private const string MiscSection     = "── Misc ──";
    private const string UpdatesSection  = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<bool> QuietMusicInGuiConfig { get; private set; }
    internal static ConfigEntry<bool> CondenseXpBarConfig { get; private set; }
    internal static ConfigEntry<bool> ModifyPlayerMovementSpeedConfig { get; private set; }
    internal static ConfigEntry<float> PlayerMovementSpeedConfig { get; private set; }
    internal static ConfigEntry<bool> ModifyPorterMovementSpeedConfig { get; private set; }
    internal static ConfigEntry<float> PorterMovementSpeedConfig { get; private set; }
    internal static ConfigEntry<bool> HalloweenNowConfig { get; private set; }
    internal static ConfigEntry<bool> HideCreditsButtonOnMainMenuConfig { get; private set; }
    internal static ConfigEntry<bool> SkipIntroVideoOnNewGameConfig { get; private set; }
    internal static ConfigEntry<bool> CinematicLetterboxingConfig { get; private set; }
    internal static ConfigEntry<bool> KitsuneKitoModeConfig { get; private set; }
    internal static ConfigEntry<bool> LessenFootprintImpactConfig { get; private set; }
    internal static ConfigEntry<bool> RemovePrayerOnUseConfig { get; private set; }
    internal static ConfigEntry<bool> AddCoalToTavernOvenConfig { get; private set; }
    internal static ConfigEntry<bool> AddZombiesToPyreAndCrematoriumConfig { get; private set; }
    internal static ConfigEntry<bool> OldEnglishThrowback { get; private set; }
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
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        QuietMusicInGuiConfig = LocalizedConfig.Bind(Config, AudioSection, "Quiet Music In GUI", true, "quiet_music_in_gui", order: 100);

        CondenseXpBarConfig = LocalizedConfig.Bind(Config, UISection, "Condense XP Bar", true, "condense_xp_bar", order: 100);
        HideCreditsButtonOnMainMenuConfig = LocalizedConfig.Bind(Config, UISection, "Hide Credits Button On Main Menu", true, "hide_credits_button_on_main_menu", order: 99);
        CinematicLetterboxingConfig = LocalizedConfig.Bind(Config, UISection, "Remove Cinematic Letterboxing", true, "remove_cinematic_letterboxing", order: 98);

        HalloweenNowConfig = LocalizedConfig.Bind(Config, GameplaySection, "Halloween Now", false, "halloween_now", order: 100);
        SkipIntroVideoOnNewGameConfig = LocalizedConfig.Bind(Config, GameplaySection, "Skip Intro Video On New Game", false, "skip_intro_video_on_new_game", order: 99);
        LessenFootprintImpactConfig = LocalizedConfig.Bind(Config, GameplaySection, "Lessen Footprint Impact", false, "lessen_footprint_impact", order: 98);
        RemovePrayerOnUseConfig = LocalizedConfig.Bind(Config, GameplaySection, "Remove Prayer On Use", false, "remove_prayer_on_use", order: 97);
        AddCoalToTavernOvenConfig = LocalizedConfig.Bind(Config, GameplaySection, "Add Coal To Tavern Oven", true, "add_coal_to_tavern_oven", order: 96);
        AddZombiesToPyreAndCrematoriumConfig = LocalizedConfig.Bind(Config, GameplaySection, "Add Zombies To Pyre And Crematorium", true, "add_zombies_to_pyre_and_crematorium", order: 95);

        ModifyPlayerMovementSpeedConfig = LocalizedConfig.Bind(Config, MovementSection, "Modify Player Movement Speed", true, "modify_player_movement_speed", order: 100);
        PlayerMovementSpeedConfig = LocalizedConfig.Bind(Config, MovementSection, "Player Movement Speed", 1.0f, "player_movement_speed", new AcceptableValueRange<float>(1.0f, 100f), order: 99, dispNamePrefix: "    └ ");
        ModifyPorterMovementSpeedConfig = LocalizedConfig.Bind(Config, MovementSection, "Modify Porter Movement Speed", true, "modify_porter_movement_speed", order: 98);
        PorterMovementSpeedConfig = LocalizedConfig.Bind(Config, MovementSection, "Porter Movement Speed", 1.0f, "porter_movement_speed", new AcceptableValueRange<float>(1.0f, 100f), order: 97, dispNamePrefix: "    └ ");

        LocalizedConfig.Bind(Config, ChurchSection, "Evict All Church Visitors", true, "evict_all_church_visitors", order: 100,
            extra: a => { a.HideDefaultButton = true; a.CustomDrawer = EvictVisitorsButton; });

        KitsuneKitoModeConfig = LocalizedConfig.Bind(Config, MiscSection, "KitsuneKito Mode", false, "kitsunekito_mode", order: 100);
        OldEnglishThrowback = LocalizedConfig.Bind(Config, MiscSection, "Old English Throwback", false, "old_english_throwback", order: 99);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);
    }

    private static bool _showEvictConfirmation;

    private static void EvictVisitorsButton(ConfigEntryBase entry)
    {
        if (_showEvictConfirmation)
        {
            Lang.Reload();
            GUILayout.Label(Lang.Get("EvictConfirmText"));
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(Lang.Get("EvictYes"), GUILayout.ExpandWidth(true)))
                {
                    EvictVisitors();
                    _showEvictConfirmation = false;
                }
                if (GUILayout.Button(Lang.Get("EvictNo"), GUILayout.ExpandWidth(true)))
                {
                    _showEvictConfirmation = false;
                }
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            if (GUILayout.Button(Lang.Get("EvictButton"), GUILayout.ExpandWidth(true)))
            {
                _showEvictConfirmation = true;
            }
        }
    }

    private static void EvictVisitors()
    {
        var churchVisitors = WorldMap._objs.FindAll(a => a.obj_id.Contains("npc_church_visitor"));
        if (churchVisitors.Count == 0)
        {
            if (DebugEnabled) Helpers.Log("[Evict] No church visitors found to evict.");
            return;
        }

        if (DebugEnabled) Helpers.Log($"[Evict] Evicting {churchVisitors.Count} church visitor(s).");

        var zones = new List<WorldZone>();

        foreach (var visitor in churchVisitors.Where(visitor => visitor.obj_def.IsNPC()))
        {
            zones.Add(visitor._zone);

            if (visitor.is_removed)
            {
                if (DebugEnabled) Helpers.Log($"[Evict] Skipping {visitor.obj_id} - already marked removed.");
                continue;
            }

            visitor.components.craft.enabled = false;
            visitor.components.timer.enabled = false;
            visitor.components.hp.enabled = false;
            ChunkManager.OnDestroyObject(visitor);
            if (visitor._bubble != null)
            {
                InteractionBubbleGUI.RemoveBubble(visitor.unique_id, true);
                visitor._bubble = null;
            }

            visitor.UnlinkWithSpawnerIfExists();
            visitor.is_removed = true;
            Destroy(visitor.gameObject);
            if (!visitor._was_ever_active)
            {
                visitor.OnDestroy();
            }

            if (DebugEnabled) Helpers.Log($"[Evict] Removed visitor {visitor.obj_id} from zone {visitor._zone?.id ?? "<null>"}.");
        }

        foreach (var zone in zones.Distinct())
        {
            if (DebugEnabled) Helpers.Log($"[Evict] Recalculating zone '{zone.id}' after visitor eviction.");
            zone.Recalculate();
        }
    }
}
