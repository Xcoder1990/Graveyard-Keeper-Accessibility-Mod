namespace ShowMeMoar;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string UltrawideSection       = "── Ultrawide ──";
    private const string ScaleSection           = "── Scale ──";
    private const string PositionsSection       = "── Positions ──";
    private const string ZoomSection            = "── Zoom ──";
    private const string FogSection             = "── Fog ──";
    private const string ColorCorrectionSection = "── Color Correction ──";
    private const string DisplaySection         = "── Display ──";
    private const string HighDpiSection         = "── High-DPI Fix ──";
    private const string UpdatesSection         = "── Updates ──";

    internal static ConfigEntry<bool> Ultrawide { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> ZoomIn { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> ZoomOut { get; private set; }

    internal const float NativeAspect = 16f / 9f;
    internal static float CurrentAspect => (float) Display.main.systemWidth / Display.main.systemHeight;
    internal static bool ScreenIsUltrawide => CurrentAspect > NativeAspect;
    internal static float ScaleFactor => CurrentAspect / NativeAspect;
    internal static ConfigEntry<float> HudScale { get; private set; }
    internal static ConfigEntry<float> HorizontalHudPosition { get; private set; }
    internal static ConfigEntry<float> VerticalHudPosition { get; private set; }
    internal static ConfigEntry<float> Zoom { get; private set; }
    private static ConfigEntry<float> CraftIconAboveStations { get; set; }

    internal static ConfigEntry<bool> RemoveFog { get; private set; }

    internal static ConfigEntry<bool> BorderlessWindowed { get; private set; }
    internal static ConfigEntry<bool> SetVsyncLimitToMaxRefreshRate { get; private set; }
    internal static ConfigEntry<bool> ColorCorrection { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static ConfigEntry<string> DpiDetectedStatus { get; private set; }
    internal static ConfigEntry<string> DpiAppliedStatus { get; private set; }
    internal static ConfigEntry<bool> ApplyDpiFix { get; private set; }
    internal static ConfigEntry<bool> AskDpiFixAtStartup { get; private set; }
    internal static int CurrentDpi { get; private set; }
    internal static int ScalingPercent { get; private set; }
    internal static HighDpiFix.Host DpiHost { get; private set; }
    // Wine/Proton and native Linux/macOS ignore the Windows compatibility flag, so don't
    // prompt on those.
    internal static bool HighDpiDetected => DpiHost == HighDpiFix.Host.Windows && CurrentDpi > 96;

    private static GameObject Icons { get; set; }
    internal static TimestampedLogger Log { get; private set; }

    internal static float MaxRefreshRate => Screen.resolutions.Max(a => a.refreshRate);

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        SceneManager.sceneLoaded += (_, _) =>
        {
            var smallFont = GameObject.Find("UI Root/Label size calculators/small_font");
            if (smallFont == null) return;
            Log.LogInfo("Hiding small font. We can't disable it as it breaks UI.");
            var widget = smallFont.GetComponent<UIWidget>();
            if (widget != null) widget.color = new Color(0, 0, 0, 0);
        };

        DpiHost = HighDpiFix.DetectHost();
        if (DpiHost == HighDpiFix.Host.Windows)
        {
            CurrentDpi = HighDpiFix.DetectDpi();
            ScalingPercent = HighDpiFix.DpiToScalingPercent(CurrentDpi);
            Log.LogInfo($"[HighDpiFix] Host=Windows, DPI={CurrentDpi} ({ScalingPercent}% scaling). Fix needed: {HighDpiDetected}");
        }
        else
        {
            CurrentDpi = 96;
            ScalingPercent = 100;
            Log.LogInfo($"[HighDpiFix] Host={DpiHost}, skipping DPI fix entirely (this is a Windows-only workaround).");
        }

        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        RefreshDpiStatus();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    internal static void RefreshDpiStatus()
    {
        if (DpiDetectedStatus != null)
        {
            DpiDetectedStatus.Value = DpiHost switch
            {
                HighDpiFix.Host.Windows => string.Format(
                    Lang.Get(HighDpiDetected ? "DpiStatusWindowsFixRecommended" : "DpiStatusWindowsNoFixNeeded"),
                    ScalingPercent, CurrentDpi),
                HighDpiFix.Host.WineProton => Lang.Get("DpiStatusWineProton"),
                _                          => Lang.Get("DpiStatusNativeNonWindows"),
            };
        }
        if (DpiAppliedStatus != null)
        {
            if (DpiHost != HighDpiFix.Host.Windows)
            {
                DpiAppliedStatus.Value = Lang.Get("DpiAppliedNotApplicable");
            }
            else
            {
                var reg = Lang.Get(HighDpiFix.IsRegistryFlagSet() ? "DpiYes" : "DpiNo");
                var man = Lang.Get(HighDpiFix.IsManifestPresent() ? "DpiYes" : "DpiNo");
                DpiAppliedStatus.Value = string.Format(Lang.Get("DpiAppliedFormat"), reg, man);
            }
        }
    }

    internal static void OnGameStartedPlaying()
    {
        if (!MainGame.game_started) return;

        Patches.ScreenSize = GameObject.Find("UI Root/Screen size panel").transform;
        if (Patches.ScreenSize == null)
        {
            Log.LogError("Screen size panel not found!");
        }

        var setting = Zoom.Value;
        var defaultZoom = GameSettings.current_resolution.y / 2f;
        Camera.main!.orthographicSize = defaultZoom + setting;
    }

    private void InitConfiguration()
    {
        var defaultZoom = Screen.currentResolution.height / 2f;
        var min = 0 - defaultZoom;

        Ultrawide = LocalizedConfig.Bind(Config, UltrawideSection, "Ultrawide", ScreenIsUltrawide, "ultrawide", order: 7);

        CraftIconAboveStations = LocalizedConfig.Bind(Config, ScaleSection, "Interaction Bubble Scale", 1f, "interaction_bubble_scale", new AcceptableValueRange<float>(0.1f, 10f), order: 6);
        CraftIconAboveStations.SettingChanged += (_, _) =>
        {
            if (!MainGame.game_started) return;
            UpdateCraftIconScale(CraftIconAboveStations.Value);
        };
        HudScale = LocalizedConfig.Bind(Config, ScaleSection, "HUD Scale", 1f, "hud_scale", new AcceptableValueRange<float>(0.1f, 10f), order: 5);
        HudScale.SettingChanged += (_, _) =>
        {
            if (!MainGame.game_started) return;
            if (Patches.HUD != null) Patches.HUD.transform.localScale = new Vector3(HudScale.Value, HudScale.Value, 1);
        };

        HorizontalHudPosition = LocalizedConfig.Bind(Config, PositionsSection, "Horizontal HUD Position", 1f, "horizontal_hud_position", new AcceptableValueRange<float>(-5, 5), order: 4);
        HorizontalHudPosition.SettingChanged += (_, _) =>
        {
            if (!MainGame.game_started) return;
            if (Patches.ScreenSize != null) Patches.ScreenSize.transform.localScale = new Vector3(HorizontalHudPosition.Value, VerticalHudPosition.Value, 1);
        };

        VerticalHudPosition = LocalizedConfig.Bind(Config, PositionsSection, "Vertical HUD Position", 1f, "vertical_hud_position", new AcceptableValueRange<float>(-5, 5), order: 3);
        VerticalHudPosition.SettingChanged += (_, _) =>
        {
            if (!MainGame.game_started) return;
            if (Patches.ScreenSize != null) Patches.ScreenSize.transform.localScale = new Vector3(HorizontalHudPosition.Value, VerticalHudPosition.Value, 1);
        };


        Zoom = LocalizedConfig.Bind(Config, ZoomSection, "Zoom", 0f, "zoom", new AcceptableValueRange<float>(min + 10, defaultZoom * 2), order: 2);
        Zoom.SettingChanged += (_, _) =>
        {
            if (!MainGame.game_started) return;
            Camera.main!.orthographicSize = defaultZoom + Zoom.Value;
        };

        ZoomIn = LocalizedConfig.Bind(Config, ZoomSection, "Zoom In", new KeyboardShortcut(KeyCode.KeypadPlus), "zoom_in", order: 1);
        ZoomOut = LocalizedConfig.Bind(Config, ZoomSection, "Zoom Out", new KeyboardShortcut(KeyCode.KeypadMinus), "zoom_out", order: 0);

        RemoveFog = LocalizedConfig.Bind(Config, FogSection, "Remove Fog", true, "remove_fog", order: 0);
        ColorCorrection = LocalizedConfig.Bind(Config, ColorCorrectionSection, "Color Correction", true, "color_correction", order: 0);
        ColorCorrection.SettingChanged += (_, _) => UpdateCC();

        BorderlessWindowed = LocalizedConfig.Bind(Config, DisplaySection, "Borderless Windowed", false, "borderless_windowed", order: 1);

        SetVsyncLimitToMaxRefreshRate = LocalizedConfig.Bind(Config, DisplaySection, "Set Vsync Limit To Max Refresh Rate", true, "set_vsync_limit_to_max_refresh_rate", order: 0);

        DpiDetectedStatus = LocalizedConfig.Bind(Config, HighDpiSection, "Detected display scaling", "", "detected_display_scaling", order: 100,
            extra: a => { a.ReadOnly = true; a.HideDefaultButton = true; });

        DpiAppliedStatus = LocalizedConfig.Bind(Config, HighDpiSection, "Fix status", "", "fix_status", order: 99,
            extra: a => { a.ReadOnly = true; a.HideDefaultButton = true; });

        ApplyDpiFix = LocalizedConfig.Bind(Config, HighDpiSection, "Apply high-DPI fix", false, "apply_high_dpi_fix", order: 98);
        ApplyDpiFix.SettingChanged += (_, _) => OnApplyDpiFixToggled();

        AskDpiFixAtStartup = LocalizedConfig.Bind(Config, HighDpiSection, "Offer the fix at startup", true, "offer_the_fix_at_startup", order: 97);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 0);

        SceneManager.sceneLoaded += (_, _) => UpdateCC();
    }

    private static bool _applyingDpiFix;
    internal static void OnApplyDpiFixToggled()
    {
        if (_applyingDpiFix) return;
        _applyingDpiFix = true;
        try
        {
            if (DpiHost != HighDpiFix.Host.Windows)
            {
                ShowDialog(Lang.Get(DpiHost == HighDpiFix.Host.WineProton
                    ? "DpiWineProtonDialog"
                    : "DpiNativeNonWindowsDialog"));
                ApplyDpiFix.Value = false;
                return;
            }

            if (ApplyDpiFix.Value)
            {
                var result = HighDpiFix.Apply();
                RefreshDpiStatus();
                var key = result.FullSuccess
                    ? "DpiApplyFullSuccess"
                    : result.AnySuccess
                        ? "DpiApplyPartialSuccess"
                        : "DpiApplyFailed";
                ShowDialog(Lang.Get(key));
                if (!result.AnySuccess)
                {
                    ApplyDpiFix.Value = false;
                }
            }
            else
            {
                HighDpiFix.Remove();
                RefreshDpiStatus();
                ShowDialog(Lang.Get("DpiRemoved"));
            }
        }
        finally
        {
            _applyingDpiFix = false;
        }
    }

    private static void ShowDialog(string message)
    {
        try
        {
            if (GUIElements.me?.dialog != null && MainGame.game_started)
            {
                GUIElements.me.dialog.OpenOK(MyPluginInfo.PLUGIN_NAME, null, message, true, string.Empty);
            }
            else
            {
                Log.LogInfo($"[HighDpiFix] {message}");
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning($"[HighDpiFix] Dialog failed: {ex.Message}. Message was: {message}");
        }
    }

    internal static void UpdateCC()
    {
        var cc = Resources.FindObjectsOfTypeAll<AmplifyColorEffect>();
        foreach (var c in cc)
        {
            c.enabled = ColorCorrection.Value;
        }
    }

    private static void UpdateCraftIconScale(float scale)
    {
        Icons ??= GameObject.Find("UI Root/Interaction bubbles");
        if (Icons != null)
        {
            Icons.transform.localScale = new Vector3(scale, scale, 1);
        }
    }
}