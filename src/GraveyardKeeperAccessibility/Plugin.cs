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

        TryPatch(harmony, typeof(Patches), nameof(Patches.UIButtonColor_OnHover_Postfix),
            typeof(UIButtonColor), "OnHover", new[] { typeof(bool) });

        Log.LogInfo("Graveyard Keeper Accessibility loaded");
    }

    private int _tickCounter;

    private void Update()
    {
        try
        {
            var guiCheck = ++_tickCounter % 10 == 0;

            if (Input.GetKeyDown(KeyCode.Escape))
                guiCheck = true;

            if (guiCheck)
                GUIAccessibility.CheckForNewGUI();

            if (!GUIAccessibility.HasActiveGUI) return;

            var active = GUIAccessibility.GetActiveElements();
            var count = active.Count;
            if (count == 0) return;

            var idx = GUIAccessibility.SelectedIndex;

            if (Input.GetKeyDown(KeyCode.DownArrow))
                GUIAccessibility.SelectIndex((idx + 1) % count);
            else if (Input.GetKeyDown(KeyCode.UpArrow))
                GUIAccessibility.SelectIndex((idx - 1 + count) % count);
            else if (Input.GetKeyDown(KeyCode.LeftArrow))
                GUIAccessibility.AdjustLeft();
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                GUIAccessibility.AdjustRight();
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                GUIAccessibility.ActivateSelected();
                GUIAccessibility.CheckForNewGUI();
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Update exception: {ex.Message}");
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
