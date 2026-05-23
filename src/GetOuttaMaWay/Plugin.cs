namespace GetOuttaMaWay;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string AdvancedSection = "── Advanced ──";
    private const string GeneralSection  = "── General ──";
    private const string UpdatesSection  = "── Updates ──";

    private const string Donkey = "donkey";
    private const string NpcPrefix = "[wgo] ";
    internal static TimestampedLogger Log { get; private set; }
    internal static bool DebugEnabled;
    private static ConfigEntry<bool> Debug { get; set; }
    private static ConfigEntry<bool> NpcCollision { get; set; }
    internal static ConfigEntry<bool> WalkThroughHeavyDrops { get; private set; }
    internal static ConfigEntry<bool> IncludeEveryDroppedItem { get; private set; }
    internal static ConfigEntry<bool> DropHeaviesAwayFromPlayer { get; private set; }
    internal static ConfigEntry<bool> HeavyCollisionGracePeriod { get; private set; }
    internal static ConfigEntry<float> GracePeriodSeconds { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        Lang.Init(Assembly.GetExecutingAssembly(), Log);

        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        NpcCollision = LocalizedConfig.Bind(Config, GeneralSection, "NPC", false, "npc_collision", order: 100);
        NpcCollision.SettingChanged += (_, _) => GameStartedPlaying();

        DropHeaviesAwayFromPlayer = LocalizedConfig.Bind(Config, GeneralSection, "Drop Heavies Away From Player", true, "drop_heavies_away_from_player", order: 90);
        HeavyCollisionGracePeriod = LocalizedConfig.Bind(Config, GeneralSection, "Heavy Drop Grace Period", true, "heavy_drop_grace_period", order: 80);
        GracePeriodSeconds = LocalizedConfig.Bind(Config, GeneralSection, "Grace Period Seconds", 1.5f, "grace_period_seconds", new AcceptableValueRange<float>(0.25f, 5f), order: 79, dispNamePrefix: "    └ ", extra: a => a.ShowRangeAsPercent = false);

        WalkThroughHeavyDrops = LocalizedConfig.Bind(Config, GeneralSection, "Walk Through Heavy Drops", true, "walk_through_heavy_drops", order: 70);
        WalkThroughHeavyDrops.SettingChanged += (_, _) => Patches.ReapplyHeavyDropTriggers();
        IncludeEveryDroppedItem = LocalizedConfig.Bind(Config, GeneralSection, "Include Every Dropped Item", false, "include_every_dropped_item", order: 69, dispNamePrefix: "    └ ");
        IncludeEveryDroppedItem.SettingChanged += (_, _) => Patches.ReapplyHeavyDropTriggers();

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates");

        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
        SceneManager.sceneLoaded += (_, _) =>
        {
            GameStartedPlaying();
            Patches.ReapplyHeavyDropTriggers();
        };
    }

    internal static void GameStartedPlaying()
    {
        if (!MainGame.game_started) return;
        if (!MainGame.me.player) return;

        var allNpc = WorldMap._npcs;
        var playerCollider = MainGame.me.player.GetComponentInChildren<CircleCollider2D>();

        if (!playerCollider)
        {
            Log.LogWarning("Player collider not found!");
            return;
        }

        foreach (var npc in allNpc)
        {
            // _npcs can hold destroyed entries; the Unity null check skips them safely.
            if (!npc) continue;
            var name = npc.name;
            if (name.StartsWith(NpcPrefix))
            {
                name = name.Substring(NpcPrefix.Length);
            }
            name = name.Trim();

            if (name.Equals(Donkey))
            {
                HandleDonkeyCollision(npc, playerCollider);
            }
            else
            {
                HandleCollision(npc.gameObject.GetComponentInChildren<CircleCollider2D>(), playerCollider, NpcCollision.Value);
            }
        }
    }

    private static void HandleDonkeyCollision(WorldGameObject npc, Collider2D playerCollider)
    {
        var boxCollider = npc.gameObject.GetComponentInChildren<BoxCollider2D>();
        var capsuleCollider = npc.gameObject.GetComponentInChildren<CapsuleCollider2D>();

        if (!boxCollider || !capsuleCollider) return;

        Physics2D.IgnoreCollision(boxCollider, playerCollider, NpcCollision.Value);
        Physics2D.IgnoreCollision(capsuleCollider, playerCollider, NpcCollision.Value);
    }

    private static void HandleCollision(Collider2D npcCollider, Collider2D playerCollider, bool collisionValue)
    {
        if (!npcCollider) return;

        Physics2D.IgnoreCollision(npcCollider, playerCollider, !collisionValue);
    }
}
