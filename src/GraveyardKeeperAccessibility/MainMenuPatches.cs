namespace GraveyardKeeperAccessibility;

internal static class MainMenuPatches
{
    internal static bool IsMenuOpen { get; private set; }

    private static readonly Dictionary<GameObject, string> AllButtons = new();
    internal static readonly Dictionary<GameObject, string> ActiveButtons = new();
    internal static readonly List<GameObject> ButtonOrder = new();
    internal static int SelectedIndex = -1;

    public static void MainMenuGUI_Open_Postfix(MainMenuGUI __instance)
    {
        if (!__instance) return;

        IsMenuOpen = true;
        ScreenReader.ClearMenuContext();
        AllButtons.Clear();
        SelectedIndex = -1;

        foreach (var button in __instance.GetComponentsInChildren<UIButton>(true))
        {
            var label = button.GetComponentInChildren<UILabel>();
            if (label == null) continue;

            var text = ScreenReader.StripNguiCodes(label.text);
            if (string.IsNullOrWhiteSpace(text)) continue;

            AllButtons[button.gameObject] = text;
        }

        RebuildActiveList();
        Plugin.Log.LogInfo($"Main menu opened, {ActiveButtons.Count} active of {AllButtons.Count} total buttons");
        ScreenReader.Say("Main Menu");
    }

    public static void BaseGUI_Hide_Postfix(BaseGUI __instance)
    {
        if (!(__instance is MainMenuGUI)) return;
        IsMenuOpen = false;
        ScreenReader.ClearMenuContext();
        AllButtons.Clear();
        ActiveButtons.Clear();
        ButtonOrder.Clear();
        SelectedIndex = -1;
    }

    public static void UIButtonColor_OnHover_Postfix(UIButtonColor __instance, bool isOver)
    {
        if (!IsMenuOpen) return;

        if (!isOver)
        {
            ScreenReader.ClearMenuContext();
            return;
        }

        var go = __instance.gameObject;
        var label = GetButtonLabel(go);
        if (label == null) return;

        var idx = ButtonOrder.IndexOf(go);
        if (idx >= 0)
            SelectedIndex = idx;

        ScreenReader.SayMenu(label);
    }

    internal static void RebuildActiveList()
    {
        var prevSelected = (SelectedIndex >= 0 && SelectedIndex < ButtonOrder.Count)
            ? ButtonOrder[SelectedIndex]
            : null;

        ActiveButtons.Clear();
        ButtonOrder.Clear();

        foreach (var kvp in AllButtons)
        {
            if (kvp.Key == null || !kvp.Key.activeInHierarchy) continue;
            ActiveButtons[kvp.Key] = kvp.Value;
            ButtonOrder.Add(kvp.Key);
        }

        if (prevSelected != null)
            SelectedIndex = ButtonOrder.IndexOf(prevSelected);
        else
            SelectedIndex = -1;
    }

    internal static void SelectIndex(int index)
    {
        if (ButtonOrder.Count == 0) return;

        if (SelectedIndex >= 0 && SelectedIndex < ButtonOrder.Count)
        {
            var prev = ButtonOrder[SelectedIndex].GetComponent<UIButtonColor>();
            if (prev != null)
                prev.SetState(UIButtonColor.State.Normal, false);
        }

        SelectedIndex = index;
        var go = ButtonOrder[SelectedIndex];
        var label = ActiveButtons[go];

        var btn = go.GetComponent<UIButtonColor>();
        if (btn != null)
            btn.SetState(UIButtonColor.State.Hover, false);

        ScreenReader.Say(label);
    }

    internal static void ActivateSelected()
    {
        if (SelectedIndex < 0 || SelectedIndex >= ButtonOrder.Count) return;

        var go = ButtonOrder[SelectedIndex];
        var button = go.GetComponent<UIButton>();
        if (button == null) return;

        button.SetState(UIButtonColor.State.Pressed, false);
        go.SendMessage("OnPress", true, SendMessageOptions.DontRequireReceiver);
        go.SendMessage("OnPress", false, SendMessageOptions.DontRequireReceiver);
        go.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);
        button.SetState(UIButtonColor.State.Normal, false);
    }

    internal static string GetButtonLabel(GameObject go)
    {
        if (go == null) return null;
        if (ActiveButtons.TryGetValue(go, out var label)) return label;

        var parent = go.transform.parent;
        while (parent != null)
        {
            if (ActiveButtons.TryGetValue(parent.gameObject, out label)) return label;
            parent = parent.parent;
        }

        return null;
    }
}
