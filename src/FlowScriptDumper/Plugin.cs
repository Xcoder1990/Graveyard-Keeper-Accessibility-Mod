namespace FlowScriptDumper;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string Section = "── Dumper ──";

    internal static TimestampedLogger Log { get; private set; }

    internal static ConfigEntry<bool> Enabled { get; private set; }
    internal static ConfigEntry<bool> DumpResources { get; private set; }
    internal static ConfigEntry<bool> DumpSceneScripts { get; private set; }
    internal static ConfigEntry<bool> HookGetGraph { get; private set; }
    internal static ConfigEntry<bool> WriteRefSidecars { get; private set; }
    internal static ConfigEntry<bool> PrettyPrint { get; private set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);

        Enabled = LocalizedConfig.Bind(Config, Section, "Enabled", true, "enabled", order: 100);
        DumpResources = LocalizedConfig.Bind(Config, Section, "Dump Resources Folder", true, "dump_resources_folder", order: 90);
        DumpSceneScripts = LocalizedConfig.Bind(Config, Section, "Dump Scene-Attached Scripts", true, "dump_scene_attached_scripts", order: 80);
        HookGetGraph = LocalizedConfig.Bind(Config, Section, "Hook CustomFlowScript.GetGraph", true, "hook_get_graph", order: 70);
        WriteRefSidecars = LocalizedConfig.Bind(Config, Section, "Write Reference Sidecars", true, "write_reference_sidecars", order: 60);
        PrettyPrint = LocalizedConfig.Bind(Config, Section, "Pretty Print", true, "pretty_print", order: 50);

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }
}
