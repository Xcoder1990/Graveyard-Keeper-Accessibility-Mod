using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Shared;

internal class ConflictLabelPositioner : MonoBehaviour
{
    // Mirrors UpdateChecker's 0.495 on the right - same fractional offset, opposite side.
    private const float PositionRatio = 0.495f;

    private UIRoot _root;

    private void LateUpdate()
    {
        if (!_root)
        {
            _root = GetComponentInParent<UIRoot>();
            if (!_root) return;
        }

        var h = _root.activeHeight;
        var aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        var w = h * aspect;
        transform.localPosition = new Vector3(-w / 2f * PositionRatio, h / 2f * PositionRatio, 0f);
    }
}

[Harmony]
internal static class ConflictWarningUI
{
    private const string LabelName = "GYK_ConflictLabel";
    private const string LogSourceName = "GYK_ConflictWarning";

    // High explicit depth keeps the labels above main-menu art (moon, GYK logos).
    private const int HeaderDepth = 5000;
    private const int EntryDepth  = 5001;
    private const int FontSizeBump = 0;

    // Traffic-light colors. Red = blocking, orange = race, green = info.
    private const string HardColor = "[CC4040]";
    private const string RaceColor = "[F7B000]";
    private const string HintColor = "[5BC44E]";
    private const string CloseTag  = "[-]";

    private static readonly TimestampedLogger Log = new(BepInEx.Logging.Logger.CreateLogSource(LogSourceName));

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MainMenuGUI), nameof(MainMenuGUI.Open), typeof(bool))]
    public static void MainMenuGUI_Open_Postfix(MainMenuGUI __instance)
    {
        if (!__instance) return;
        Render(__instance);
    }

    // Called by ConflictWarningRegistry_LangPatch when the game loads a new language,
    // so the on-screen notes refresh without needing the player to re-open the menu.
    internal static void RefreshIfMenuOpen()
    {
        if (!GUIElements.me) return;
        var menu = GUIElements.me.main_menu;
        if (!menu || !menu.gameObject.activeInHierarchy) return;
        Render(menu);
    }

    private static void Render(MainMenuGUI mainMenu)
    {
        var active = ConflictWarningRegistry.GetActive();
        var parent = mainMenu.version_txt ? mainMenu.version_txt.transform.parent : mainMenu.transform;
        var existing = parent.Find(LabelName);

        if (active.Count == 0)
        {
            if (existing) existing.gameObject.SetActive(false);
            return;
        }

        if (existing)
        {
            UnityEngine.Object.DestroyImmediate(existing.gameObject);
        }

        // Traffic-light order: Hard (red) first, Race (orange) next, Hint (green) last.
        active.Sort((a, b) => ((int)a.Severity).CompareTo((int)b.Severity));

        BuildContainer(mainMenu, parent, active);
    }

    private static void BuildContainer(MainMenuGUI mainMenu, Transform parent, List<ConflictWarningRegistry.RegisteredConflict> conflicts)
    {
        var reference = mainMenu.version_txt;
        if (!reference)
        {
            Log.LogWarning("version_txt missing - cannot build conflict label");
            return;
        }

        var container = new GameObject(LabelName) { layer = reference.gameObject.layer };
        container.transform.SetParent(parent, false);
        container.transform.localRotation = Quaternion.identity;
        container.transform.localScale = Vector3.one;
        container.AddComponent<ConflictLabelPositioner>();

        // Wrap-width: a fifth of NGUI virtual width so long notes wrap before they hit
        // the menu/logo area. Read live UIRoot.activeHeight + screen aspect so narrow
        // aspects (Steam Deck 16:10) get a tighter wrap and ultrawide gets a wider one.
        var uiRoot = mainMenu.GetComponentInParent<UIRoot>();
        var virtualHeight = uiRoot ? uiRoot.activeHeight : 1080;
        var aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        var virtualWidth = (int)(virtualHeight * aspect);
        var wrapWidth = virtualWidth / 5;

        var headerSeverity = HighestSeverity(conflicts);
        var header = CreateChildLabel(container, reference, BuildHeaderText(headerSeverity), HeaderDepth, wrapWidth);
        header.transform.localPosition = Vector3.zero;
        header.MakePixelPerfect();

        var lineHeight = (reference.fontSize > 0 ? reference.fontSize : 20) + FontSizeBump + 2;
        var y = -lineHeight;

        foreach (var c in conflicts)
        {
            var color = ColorFor(c.Severity);
            var text = color + c.OurMod + " + " + c.TheirName + " | " + c.Feature + ": " + c.Note;
            var entry = CreateChildLabel(container, reference, text, EntryDepth, wrapWidth);
            entry.transform.localPosition = new Vector3(0f, y, 0f);
            entry.MakePixelPerfect();
            // Step down by the rendered height of this entry so wrapped notes don't overlap.
            var rendered = entry.height > 0 ? entry.height : lineHeight;
            y -= rendered + 2;
        }
    }

    private static UILabel CreateChildLabel(GameObject parent, UILabel reference, string text, int depth, int wrapWidth)
    {
        var go = new GameObject("ConflictEntry") { layer = reference.gameObject.layer };
        go.transform.SetParent(parent.transform, false);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var label = go.AddComponent<UILabel>();
        if (reference.bitmapFont) label.bitmapFont = reference.bitmapFont;
        else if (reference.trueTypeFont) label.trueTypeFont = reference.trueTypeFont;
        label.fontSize = reference.fontSize + FontSizeBump;
        label.fontStyle = reference.fontStyle;
        label.pivot = UIWidget.Pivot.TopLeft;
        label.alignment = NGUIText.Alignment.Left;
        label.overflowMethod = UILabel.Overflow.ResizeHeight;
        label.width = wrapWidth;
        label.multiLine = true;
        label.supportEncoding = true;
        label.color = Color.white;
        label.depth = depth;
        // Dark outline keeps colour-coded text readable on light backdrops (moon, daylight sky).
        label.effectStyle = UILabel.Effect.Outline;
        label.effectColor = new Color(0f, 0f, 0f, 0.9f);
        label.effectDistance = new Vector2(1f, 1f);
        label.text = text;
        // GYK swaps to a different bitmap atlas for Japanese/Chinese/Korean. The reference
        // label we copied from may still hold the Latin atlas at the moment we run (our
        // GJL.LoadLanguageResource postfix fires before the menu refonts its own labels),
        // so apply the current-language atlas directly. do_cache:false avoids leaking
        // throwaway labels into GJL's internal cache - we recreate everything every refresh.
        try { GJL.EnsureLabelHasCorrectFont(label, false); }
        catch { /* never let font swap break the warning UI */ }
        return label;
    }

    private static string ColorFor(ConflictSeverity severity)
    {
        if (severity == ConflictSeverity.Hard) return HardColor;
        if (severity == ConflictSeverity.Race) return RaceColor;
        return HintColor;
    }

    private static ConflictSeverity HighestSeverity(List<ConflictWarningRegistry.RegisteredConflict> conflicts)
    {
        var highest = ConflictSeverity.Hint;
        foreach (var c in conflicts)
        {
            if (c.Severity == ConflictSeverity.Hard) return ConflictSeverity.Hard;
            if (c.Severity == ConflictSeverity.Race) highest = ConflictSeverity.Race;
        }
        return highest;
    }

    private static string BuildHeaderText(ConflictSeverity severity)
    {
        return ColorFor(severity) + GetHeaderTranslation() + CloseTag;
    }

    // Inline header translations so the conflict UI works for any language without each
    // mod having to add the same shared key to its own lang JSON.
    private static readonly Dictionary<string, string> HeaderTranslations = new()
    {
        ["en"]    = "Mod Compatibility Notes",
        ["de"]    = "Mod-Kompatibilitätshinweise",
        ["es"]    = "Notas de Compatibilidad de Mods",
        ["fr"]    = "Notes de Compatibilité des Mods",
        ["it"]    = "Note di Compatibilità Mod",
        ["ja"]    = "MOD互換性メモ",
        ["ko"]    = "모드 호환성 메모",
        ["pl"]    = "Uwagi o Zgodności Modów",
        ["pt-br"] = "Notas de Compatibilidade de Mods",
        ["ru"]    = "Заметки о совместимости модов",
        ["zh-cn"] = "模组兼容性说明",
    };

    private static string GetHeaderTranslation()
    {
        var lang = GameSettings._cur_lng ?? string.Empty;
        lang = lang.ToLowerInvariant().Replace('_', '-');
        return HeaderTranslations.TryGetValue(lang, out var t) ? t : HeaderTranslations["en"];
    }
}
