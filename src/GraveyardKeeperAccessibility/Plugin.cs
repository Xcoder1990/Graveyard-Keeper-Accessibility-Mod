namespace GraveyardKeeperAccessibility;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log { get; private set; }

    private void Awake()
    {
        Log = Logger;
        ScreenReader.Init(Log);

        var harmony = new HarmonyLib.Harmony(MyPluginInfo.PLUGIN_GUID);

        TryPatch(harmony, typeof(MainMenuPatches), nameof(MainMenuPatches.MainMenuGUI_Open_Postfix),
            typeof(MainMenuGUI), "Open", new[] { typeof(bool) });

        TryPatch(harmony, typeof(MainMenuPatches), nameof(MainMenuPatches.BaseGUI_Hide_Postfix),
            typeof(BaseGUI), "Hide", new[] { typeof(bool) });

        TryPatch(harmony, typeof(MainMenuPatches),
            nameof(MainMenuPatches.UIButtonColor_OnHover_Postfix),
            typeof(UIButtonColor), "OnHover", new[] { typeof(bool) });

        Log.LogInfo("Graveyard Keeper Accessibility loaded");
    }

    private int _rebuildCounter;

    private void Update()
    {
        if (!MainMenuPatches.IsMenuOpen) return;

        if (++_rebuildCounter % 60 == 0)
            MainMenuPatches.RebuildActiveList();

        if (MainMenuPatches.ButtonOrder.Count == 0) return;

        var count = MainMenuPatches.ButtonOrder.Count;
        var idx = MainMenuPatches.SelectedIndex;

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            MainMenuPatches.SelectIndex((idx + 1) % count);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            MainMenuPatches.SelectIndex((idx - 1 + count) % count);
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            MainMenuPatches.ActivateSelected();
        }
    }

    private static bool TryPatch(HarmonyLib.Harmony harmony, Type patchClass, string methodName,
        Type targetType, string targetMethod, Type[] parameters)
    {
        try
        {
            var original = AccessTools.Method(targetType, targetMethod, parameters);
            if (original == null)
            {
                Log.LogWarning($"Method {targetType.Name}.{targetMethod} not found, skipping");
                return false;
            }

            var postfix = new HarmonyMethod(AccessTools.Method(patchClass, methodName));
            harmony.Patch(original, postfix: postfix);
            Log.LogInfo($"Patched {targetType.Name}.{targetMethod}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to patch {targetType.Name}.{targetMethod}: {ex.Message}");
            return false;
        }
    }

    private void OnDestroy()
    {
        ScreenReader.Shutdown();
    }
}
