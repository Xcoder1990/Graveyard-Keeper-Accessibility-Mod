namespace FlowScriptDumper;

[Harmony]
[HarmonyWrapSafe]
public static class Patches
{
    private static bool _ranResourcesDump;
    private static bool _ranSceneDump;

    // First main-menu open is the earliest point where every Resources folder
    // is reliably populated. Fire the bulk dump here, once.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open))]
    public static void MainMenuGUI_Open_Postfix()
    {
        if (!Plugin.Enabled.Value) return;
        if (_ranResourcesDump) return;
        _ranResourcesDump = true;

        if (Plugin.DumpResources.Value)
        {
            Dumper.DumpResourcesFolder();
        }
    }

    // FlowScriptEngine lives in the gameplay scene, not the menu. By the time
    // its Awake runs, _scripts is populated with every inspector-assigned
    // controller. Dump once per game session.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(FlowScriptEngine), nameof(FlowScriptEngine.Awake))]
    public static void FlowScriptEngine_Awake_Postfix()
    {
        if (!Plugin.Enabled.Value) return;
        if (_ranSceneDump) return;
        _ranSceneDump = true;

        if (Plugin.DumpSceneScripts.Value)
        {
            Dumper.DumpSceneAttachedScripts();
        }
    }

    // Catch-all for runtime CustomFlowScript.GetGraph("...") calls. Anything
    // dynamically loaded by name that the Resources sweep missed still lands
    // in the dump folder.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CustomFlowScript), nameof(CustomFlowScript.GetGraph))]
    public static void CustomFlowScript_GetGraph_Postfix(string name, FlowGraph __result)
    {
        if (!Plugin.Enabled.Value || !Plugin.HookGetGraph.Value) return;
        if (__result == null) return;
        Dumper.TryDump(name, __result);
    }
}
