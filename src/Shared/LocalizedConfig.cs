using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Shared;

// Binds a setting whose F1 label, category and tooltip come from the mod's lang JSON.
// The on-disk .cfg keeps the English section + key, so switching the game language
// doesn't churn the player's saved values.
//
// We keep the attribute object the F1 menu reads and rewrite its strings just
// before BuildSettingList runs. The Prefix that does this is attached from a
// watcher (see bottom of file) rather than via [HarmonyPatch], so it doesn't
// matter whether the mod loaded before or after ConfigurationManager.
internal static class LocalizedConfig
{
    private sealed class Binding
    {
        public ConfigurationManagerAttributes Attrs;
        public string SectionKey;
        public string LangKey;
        public string DispNamePrefix;
    }

    private static readonly List<Binding> Bindings = [];

    // Either CM works: the upstream BepInEx one or the GYK fork.
    private static readonly (string Guid, string TypeName)[] Variants =
    {
        ("com.bepis.bepinex.configurationmanager", "ConfigurationManager.ConfigurationManager"),
        ("p1xel8ted.gyk.configurationmanager",     "ConfigurationManager.ConfigurationManagerPlugin"),
    };

    private static bool _watcherSpawned;

    internal static ConfigEntry<T> Bind<T>(
        ConfigFile config,
        string section,
        string key,
        T defaultValue,
        string langKey,
        AcceptableValueBase acceptableValues = null,
        int order = 0,
        string dispNamePrefix = null,
        Action<ConfigurationManagerAttributes> extra = null)
    {
        var sectionKey = NormalizeSection(section);

        var attrs = new ConfigurationManagerAttributes
        {
            Order = order,
        };

        var binding = new Binding
        {
            Attrs = attrs,
            SectionKey = sectionKey,
            LangKey = langKey,
            DispNamePrefix = dispNamePrefix,
        };
        Apply(binding);
        Bindings.Add(binding);

        extra?.Invoke(attrs);

        EnsureCmWatcher();

        return config.Bind(section, key, defaultValue,
            new ConfigDescription(attrs.Description ?? string.Empty, acceptableValues, attrs));
    }

    internal static void RefreshAll()
    {
        foreach (var b in Bindings)
        {
            Apply(b);
        }
    }

    private static void Apply(Binding b)
    {
        var name = Lang.Get($"cfg.{b.LangKey}.name");
        // Wrap the localised section name with the section-bar decoration that
        // the English C# constants use, so every language matches the same style.
        b.Attrs.Category = $"── {Lang.Get($"cfg.section.{b.SectionKey}")} ──";
        b.Attrs.DispName = string.IsNullOrEmpty(b.DispNamePrefix) ? name : b.DispNamePrefix + name;
        b.Attrs.Description = Lang.Get($"cfg.{b.LangKey}.desc");
    }

    // "── Unlimited Stats ──" becomes "unlimited_stats". Drops the section-bar
    // decoration so the lang key stays the same if the bar style ever changes.
    private static string NormalizeSection(string section)
    {
        if (string.IsNullOrEmpty(section)) return "default";
        var sb = new StringBuilder(section.Length);
        foreach (var c in section)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsWhiteSpace(c))
            {
                if (sb.Length > 0 && sb[sb.Length - 1] != '_') sb.Append('_');
            }
        }
        while (sb.Length > 0 && sb[sb.Length - 1] == '_') sb.Length--;
        return sb.Length == 0 ? "default" : sb.ToString();
    }

    // One watcher per mod. It waits for ConfigurationManager to show up in
    // Chainloader, then patches BuildSettingList from this assembly. Doing the
    // patch from Update instead of via [HarmonyPatch] is what makes load order
    // between the mod and CM not matter.
    private static void EnsureCmWatcher()
    {
        if (_watcherSpawned) return;
        _watcherSpawned = true;
        var asmName = Assembly.GetExecutingAssembly().GetName().Name;
        var go = new GameObject($"~LocalizedConfigCMWatcher.{asmName}");
        UnityEngine.Object.DontDestroyOnLoad(go);
        var w = go.AddComponent<CmWatcher>();
        w.OwnerAssembly = asmName;
        w.Prefix = typeof(LocalizedConfig).GetMethod(nameof(CmPrefix), BindingFlags.Static | BindingFlags.NonPublic);
    }

    // Prefix that fires before BuildSettingList. Re-pulls every binding's
    // strings so the F1 menu shows the current language.
    private static void CmPrefix() => RefreshAll();

    private sealed class CmWatcher : MonoBehaviour
    {
        public string OwnerAssembly;
        public MethodInfo Prefix;
        private bool _patched;

        private void Update()
        {
            if (_patched) return;
            foreach (var (guid, typeName) in Variants)
            {
                if (!Chainloader.PluginInfos.ContainsKey(guid)) continue;
                var t = AccessTools.TypeByName(typeName);
                var m = t?.GetMethod("BuildSettingList", BindingFlags.Public | BindingFlags.Instance);
                if (m == null) continue;
                var harmony = new Harmony($"{OwnerAssembly}.LocalizedConfig.CmHook");
                harmony.Patch(m, prefix: new HarmonyMethod(Prefix));
                _patched = true;
            }
            if (_patched) Destroy(this);
        }
    }
}
