namespace GameBalanceDumper;

[Harmony]
[HarmonyWrapSafe]
public static class Patches
{
    // Priority.First runs before unmarked postfixes (Normal = 400), so we capture the
    // pristine GameBalance state before EconomyReloaded / FasterCraftReloaded /
    // GiveMeMoar / AlchemyResearchRedux / GraveChangesRedux mutate it.
    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static void GameBalance_LoadGameBalance_Postfix()
    {
        Dumper.DumpOnce();
    }
}
