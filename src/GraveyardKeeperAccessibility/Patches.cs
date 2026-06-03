namespace GraveyardKeeperAccessibility;

internal static class Patches
{
    public static void UIButtonColor_OnHover_Postfix(UIButtonColor __instance, bool isOver)
    {
        GUIAccessibility.OnHover(__instance, isOver);
    }
}
