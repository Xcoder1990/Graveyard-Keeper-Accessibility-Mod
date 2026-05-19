namespace FlowScriptDumper;

internal static class Dumper
{
    private static readonly HashSet<string> Seen = [];
    // Content hash of every raw _serializedGraph already written. The scene-
    // attached path (FlowScriptEngine._scripts) reaches the same assets the
    // Resources sweep already covered but under different .name values
    // (e.g. "WGO Scripts_npc_actress" vs "npc_actress"), so name dedupe alone
    // lets byte-identical duplicates through. Skip on content match instead.
    private static readonly HashSet<string> SeenContent = [];
    private static string _outDir;

    internal static string OutDir
    {
        get
        {
            if (!string.IsNullOrEmpty(_outDir)) return _outDir;
            var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _outDir = Path.Combine(asmDir ?? Paths.PluginPath, "dump");
            Directory.CreateDirectory(_outDir);
            return _outDir;
        }
    }

    internal static void DumpResourcesFolder()
    {
        FlowScript[] all;
        try
        {
            all = Resources.LoadAll<FlowScript>("FlowCanvas");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Dumper] Resources.LoadAll failed: {ex}");
            return;
        }

        LogHelper.Info($"[Dumper] Resources/FlowCanvas found {all.Length} FlowScript assets.");
        var written = 0;
        foreach (var fs in all)
        {
            if (fs == null) continue;
            if (TryDump(fs.name, fs)) written++;
        }
        LogHelper.Info($"[Dumper] Resources folder dump complete: {written} new files written to {OutDir}");
    }

    internal static void DumpSceneAttachedScripts()
    {
        if (FlowScriptEngine._me == null)
        {
            LogHelper.Info("[Dumper] FlowScriptEngine._me is null - no scene-attached scripts to dump yet.");
            return;
        }

        var scripts = FlowScriptEngine._me._scripts;
        if (scripts == null || scripts.Length == 0)
        {
            LogHelper.Info("[Dumper] FlowScriptEngine._scripts is empty.");
            return;
        }

        var written = 0;
        foreach (var ctrl in scripts)
        {
            if (ctrl == null) continue;
            var g = ctrl.graph as FlowGraph;
            if (g == null) continue;
            var name = !string.IsNullOrEmpty(g.name) ? g.name : ctrl.gameObject.name;
            if (TryDump(name, g)) written++;
        }
        LogHelper.Info($"[Dumper] Scene scripts dump complete: {written} new files (out of {scripts.Length} controllers).");
    }

    internal static bool TryDump(string name, FlowGraph graph)
    {
        if (graph == null || string.IsNullOrEmpty(name)) return false;
        var safe = Sanitize(name);
        if (!Seen.Add(safe)) return false;

        try
        {
            var raw = graph._serializedGraph ?? string.Empty;
            // Hash the raw bytes, not the pretty-printed string, so the result
            // is stable across PrettyPrint toggles and the two access paths.
            var hash = HashContent(raw);
            if (!SeenContent.Add(hash)) return false;

            var json = Plugin.PrettyPrint.Value && !string.IsNullOrEmpty(raw)
                ? PrettyJson(raw)
                : raw;
            File.WriteAllText(Path.Combine(OutDir, safe + ".json"), json);

            if (Plugin.WriteRefSidecars.Value)
            {
                WriteRefSidecar(safe, graph._objectReferences);
            }
            return true;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Dumper] Failed to dump '{name}': {ex.Message}");
            return false;
        }
    }

    private static string HashContent(string s)
    {
        // SHA1 is fine here - this is dedupe, not crypto. .NET's MD5 would
        // also work; SHA1 avoids the "is MD5 still available on this runtime"
        // wobble across BepInEx hosts.
        using var sha = System.Security.Cryptography.SHA1.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private static void WriteRefSidecar(string safeName, List<UnityEngine.Object> refs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("idx\ttype\tname");
        if (refs != null)
        {
            for (var i = 0; i < refs.Count; i++)
            {
                var o = refs[i];
                if (o == null)
                {
                    sb.Append(i).Append("\tnull\t").AppendLine();
                }
                else
                {
                    sb.Append(i).Append('\t').Append(o.GetType().FullName).Append('\t').AppendLine(o.name);
                }
            }
        }
        File.WriteAllText(Path.Combine(OutDir, safeName + ".refs.tsv"), sb.ToString());
    }

    private static string PrettyJson(string raw)
    {
        try
        {
            var token = Newtonsoft.Json.Linq.JToken.Parse(raw);
            return token.ToString(Newtonsoft.Json.Formatting.Indented);
        }
        catch
        {
            // FlowCanvas's JSON is well-formed in practice, but never trust it
            // enough to lose the original string on a parse failure.
            return raw;
        }
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }
        return sb.ToString();
    }
}
