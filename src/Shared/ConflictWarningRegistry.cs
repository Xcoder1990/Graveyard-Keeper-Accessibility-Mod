using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace Shared;

// Cross-mod conflict registry. Each mod's Awake() calls Register with a provider that
// returns fresh ConflictEntry instances; the registry rebuilds the entry GameObjects from
// the provider every time the game's GJL.LoadLanguageResource fires (i.e. on language
// load/change). The provider captures the mod's Lang.Get reference, so each refresh
// produces strings in the current language without any cross-assembly resolution.
//
// Sharing works the same way UpdateChecker does: a named sentinel GameObject holds the
// data as child GameObjects with name-encoded fields. Each mod has its own per-assembly
// copy of this static class, but they all see the same children via GameObject.Find.

public enum ConflictSeverity
{
    Hard, // Plugin GUID collision: only one of the two can load.
    Race, // Both write the same shared state. Documented mitigation usually applies.
    Hint, // Not a conflict; tells the player to enable an interop toggle.
}

public sealed class ConflictEntry
{
    public readonly string TheirGuid;
    public readonly string TheirName;
    public readonly string Feature;
    public readonly ConflictSeverity Severity;
    public readonly string Note;

    public ConflictEntry(string theirGuid, string theirName, string feature, ConflictSeverity severity, string note)
    {
        TheirGuid = theirGuid;
        TheirName = theirName;
        Feature = feature;
        Severity = severity;
        Note = note;
    }
}

public static class ConflictWarningRegistry
{
    internal const string SentinelName = "GYK_ConflictRegistry";
    internal const string ChildPrefix = "CONFLICT|";
    private const char Sep = '|';
    private const char EscapedSep = '¦';

    // Per-assembly state. Each mod's compile keeps track of its own mod name + provider,
    // so Refresh() only touches that mod's GameObject children.
    private static string _ourMod;
    private static Func<IEnumerable<ConflictEntry>> _provider;

    // Register a provider that returns fresh entries each time it's called. The provider
    // should call Lang.Get(...) inside it so each refresh picks up the current language.
    public static void Register(string ourMod, Func<IEnumerable<ConflictEntry>> entriesProvider)
    {
        if (string.IsNullOrEmpty(ourMod) || entriesProvider == null) return;
        _ourMod = ourMod;
        _provider = entriesProvider;
        Refresh();
    }

    // Recreate this mod's GameObject children from the provider. Called at Register time
    // and again on GJL.LoadLanguageResource (via the patch below) when language changes.
    internal static void Refresh()
    {
        try
        {
            if (_provider == null || string.IsNullOrEmpty(_ourMod)) return;

            var sentinel = GameObject.Find(SentinelName);
            if (!sentinel)
            {
                sentinel = new GameObject(SentinelName);
                UnityEngine.Object.DontDestroyOnLoad(sentinel);
            }

            // Remove old children that belong to this mod, leaving other mods' entries alone.
            var ownPrefix = ChildPrefix + Esc(_ourMod) + Sep;
            for (var i = sentinel.transform.childCount - 1; i >= 0; i--)
            {
                var child = sentinel.transform.GetChild(i);
                if (child != null && !string.IsNullOrEmpty(child.name) && child.name.StartsWith(ownPrefix))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // Re-create with whatever the provider returns now (resolved against current lang).
            var entries = _provider();
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.TheirGuid)) continue;
                var name = ChildPrefix
                    + Esc(_ourMod) + Sep
                    + Esc(entry.TheirGuid) + Sep
                    + entry.Severity + Sep
                    + Esc(entry.Feature ?? string.Empty) + Sep
                    + Esc(entry.Note ?? string.Empty);
                var child = new GameObject(name);
                child.transform.SetParent(sentinel.transform, false);
            }
        }
        catch (Exception)
        {
            // Refresh failure must never break mod load or main menu render.
        }
    }

    internal static List<RegisteredConflict> GetActive()
    {
        var list = new List<RegisteredConflict>();
        var sentinel = GameObject.Find(SentinelName);
        if (!sentinel) return list;

        foreach (Transform child in sentinel.transform)
        {
            var n = child.name;
            if (string.IsNullOrEmpty(n) || !n.StartsWith(ChildPrefix)) continue;
            var parts = n.Substring(ChildPrefix.Length).Split(Sep);
            if (parts.Length < 5) continue;

            var theirGuid = Unesc(parts[1]);
            if (!Chainloader.PluginInfos.ContainsKey(theirGuid)) continue;

            if (!Enum.TryParse(parts[2], out ConflictSeverity severity)) continue;

            list.Add(new RegisteredConflict
            {
                OurMod = Unesc(parts[0]),
                TheirGuid = theirGuid,
                TheirName = LookupLoadedName(theirGuid),
                Severity = severity,
                Feature = Unesc(parts[3]),
                Note = Unesc(parts[4]),
            });
        }
        return list;
    }

    private static string LookupLoadedName(string guid)
    {
        if (Chainloader.PluginInfos.TryGetValue(guid, out PluginInfo info) && info?.Metadata != null)
        {
            return info.Metadata.Name ?? guid;
        }
        return guid;
    }

    private static string Esc(string s) { return string.IsNullOrEmpty(s) ? string.Empty : s.Replace(Sep, EscapedSep); }
    private static string Unesc(string s) { return string.IsNullOrEmpty(s) ? string.Empty : s.Replace(EscapedSep, Sep); }

    internal class RegisteredConflict
    {
        public string OurMod;
        public string TheirGuid;
        public string TheirName;
        public ConflictSeverity Severity;
        public string Feature;
        public string Note;
    }
}

// Re-resolve this mod's conflict entries whenever the game loads a language. Each mod's
// Harmony patch fires once on every language load (per assembly), and only updates that
// mod's own GameObject children, so we get the right translations after the language
// switch without any cross-assembly coordination.
[Harmony]
internal static class ConflictWarningRegistry_LangPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GJL), nameof(GJL.LoadLanguageResource))]
    public static void GJL_LoadLanguageResource_Postfix()
    {
        ConflictWarningRegistry.Refresh();
        ConflictWarningUI.RefreshIfMenuOpen();
    }
}
