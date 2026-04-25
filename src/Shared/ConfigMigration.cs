using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace Shared;

// Helpers for migrating BepInEx config INI files when a mod renames a config section
// header or a config key. Without this, renaming a Bind() argument silently orphans
// the user's saved value and resets to the new default.
//
// Each mod DLL compiles its own copy of this file — there are no cross-mod statics.
// Call from Plugin.Awake BEFORE InitConfiguration so the new Bind() picks up the
// migrated value on first read.
public static class ConfigMigration
{
    public sealed class KeyRename
    {
        public string Section { get; }
        public string OldKey { get; }
        public string NewKey { get; }
        public Func<string, string> ValueTransform { get; }

        public KeyRename(string section, string oldKey, string newKey, Func<string, string> valueTransform = null)
        {
            Section = section;
            OldKey = oldKey;
            NewKey = newKey;
            ValueTransform = valueTransform;
        }
    }

    // Section header rename: "[Old Section]" → "[New Section]". Idempotent.
    public static void MigrateRenamedSections(ConfigFile cfg, ManualLogSource log, IDictionary<string, string> renames)
    {
        if (renames == null || renames.Count == 0) return;
        var path = cfg.ConfigFilePath;
        if (!File.Exists(path)) return;

        string content;
        try
        {
            content = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ConfigMigration] Could not read {path} for section rename: {ex.Message}");
            return;
        }

        var renamed = 0;
        foreach (var kv in renames)
        {
            var oldHeader = $"[{kv.Key}]";
            var newHeader = $"[{kv.Value}]";
            if (!content.Contains(oldHeader)) continue;
            content = content.Replace(oldHeader, newHeader);
            renamed++;
        }
        if (renamed == 0) return;

        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ConfigMigration] Could not write {path} after section rename: {ex.Message}");
            return;
        }

        log?.LogInfo($"[ConfigMigration] Renamed {renamed} legacy config section header(s). Existing user values preserved.");
        cfg.Reload();
    }

    // Key rename inside a known section. Optional value transform for polarity flips
    // (e.g. invert a bool when "Disable Foo" becomes "Foo"). Idempotent — if the new
    // key is already present and the old key isn't, the file isn't touched.
    public static void MigrateRenamedKeys(ConfigFile cfg, ManualLogSource log, params KeyRename[] renames)
    {
        if (renames == null || renames.Length == 0) return;
        var path = cfg.ConfigFilePath;
        if (!File.Exists(path)) return;

        string[] lines;
        try
        {
            lines = File.ReadAllLines(path);
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ConfigMigration] Could not read {path} for key rename: {ex.Message}");
            return;
        }

        var renamed = 0;
        var currentSection = "";
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("[") && trimmed.Contains("]"))
            {
                var end = trimmed.IndexOf(']');
                currentSection = trimmed.Substring(1, end - 1);
                continue;
            }

            if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;

            var eq = line.IndexOf('=');
            if (eq <= 0) continue;
            var key = line.Substring(0, eq).Trim();
            var value = line.Substring(eq + 1).Trim();

            foreach (var rename in renames)
            {
                if (rename.Section != currentSection || rename.OldKey != key) continue;
                var newValue = rename.ValueTransform != null ? rename.ValueTransform(value) : value;
                lines[i] = $"{rename.NewKey} = {newValue}";
                renamed++;
                break;
            }
        }

        if (renamed == 0) return;

        try
        {
            File.WriteAllLines(path, lines);
        }
        catch (Exception ex)
        {
            log?.LogWarning($"[ConfigMigration] Could not write {path} after key rename: {ex.Message}");
            return;
        }

        log?.LogInfo($"[ConfigMigration] Renamed {renamed} legacy config key(s). Existing user values preserved.");
        cfg.Reload();
    }

    // Convenience: invert a "true"/"false" string. Use for polarity-flip key renames
    // like "Disable Foo = true" → "Foo = false".
    public static string InvertBool(string value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
}
