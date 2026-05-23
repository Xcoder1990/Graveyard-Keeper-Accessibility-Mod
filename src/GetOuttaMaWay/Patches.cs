using System.Runtime.CompilerServices;

namespace GetOuttaMaWay;

[Harmony]
public static class Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSave), nameof(GameSave.GlobalEventsCheck))]
    [HarmonyPatch(typeof(WorldMap), nameof(WorldMap.RescanWGOsList))]
    [HarmonyPatch(typeof(WorldZone), nameof(WorldZone.OnPlayerEnter))]
    public static void WorldMap_RescanWGOsList()
    {
        Plugin.GameStartedPlaying();
    }

    // Stop heavies from flying at the player on drop. They still scatter with a kick.
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.Drop),
        [typeof(Vector3), typeof(Item), typeof(Transform), typeof(Direction),
         typeof(float), typeof(int), typeof(bool), typeof(bool)])]
    public static void DropResGameObject_Drop_Prefix(Item item, ref Direction direction)
    {
        if (!Plugin.DropHeaviesAwayFromPlayer.Value)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[Drop_Prefix] skip (DropHeaviesAwayFromPlayer=false), item='{item?.id ?? "null"}', dir={direction}");
            return;
        }
        if (direction != Direction.ToPlayer)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[Drop_Prefix] skip (dir={direction} not ToPlayer), item='{item?.id ?? "null"}'");
            return;
        }
        if (item == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[Drop_Prefix] skip (item is null)");
            return;
        }
        if (item.definition == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[Drop_Prefix] skip (item.definition null), item.id='{item.id ?? "null"}'");
            return;
        }
        if (!item.definition.is_big)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[Drop_Prefix] skip (not heavy), item='{item.id}', item_size={item.definition.item_size}");
            return;
        }
        if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[Drop_Prefix] redirecting heavy '{item.id}' ToPlayer -> None");
        direction = Direction.None;
    }

    // Ignore collision between player and a freshly dropped heavy for a short window.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.DoDrop),
        [typeof(Item), typeof(int), typeof(bool)])]
    public static void DropResGameObject_DoDrop_Postfix(DropResGameObject __instance)
    {
        if (!Plugin.HeavyCollisionGracePeriod.Value)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo("[DoDrop_Postfix] skip (HeavyCollisionGracePeriod=false)");
            return;
        }
        if (__instance == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[DoDrop_Postfix] skip (__instance is null)");
            return;
        }
        if (__instance.res == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[DoDrop_Postfix] skip (res is null)");
            return;
        }
        if (__instance.res.definition == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[DoDrop_Postfix] skip (res.definition null), res.id='{__instance.res.id ?? "null"}'");
            return;
        }
        if (!__instance.res.definition.is_big)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[DoDrop_Postfix] skip (not heavy), res='{__instance.res.id}', item_size={__instance.res.definition.item_size}");
            return;
        }
        if (MainGame.me == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[DoDrop_Postfix] skip (MainGame.me is null)");
            return;
        }
        if (MainGame.me.player == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[DoDrop_Postfix] skip (MainGame.me.player is null)");
            return;
        }

        var heavyCollider = __instance.GetComponent<CapsuleCollider2D>();
        if (heavyCollider == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning($"[DoDrop_Postfix] skip (CapsuleCollider2D missing on heavy '{__instance.res.id}')");
            return;
        }
        var playerCollider = MainGame.me.player.GetComponentInChildren<CircleCollider2D>();
        if (playerCollider == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning("[DoDrop_Postfix] skip (player CircleCollider2D missing)");
            return;
        }

        var seconds = Plugin.GracePeriodSeconds.Value;
        Physics2D.IgnoreCollision(heavyCollider, playerCollider, true);
        if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[DoDrop_Postfix] grace started on heavy '{__instance.res.id}' for {seconds:0.##}s");
        MainGame.me.StartCoroutine(RestoreHeavyCollisionAfterDelay(heavyCollider, playerCollider, seconds, __instance.res.id));
    }

    private static IEnumerator RestoreHeavyCollisionAfterDelay(Collider2D heavy, Collider2D player, float seconds, string heavyId)
    {
        yield return new WaitForSeconds(seconds);
        if (heavy == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[RestoreGrace] heavy '{heavyId}' already destroyed, nothing to restore");
            yield break;
        }
        if (player == null)
        {
            if (Plugin.DebugEnabled) Plugin.Log.LogWarning($"[RestoreGrace] player collider gone when restoring grace for heavy '{heavyId}'");
            yield break;
        }
        Physics2D.IgnoreCollision(heavy, player, false);
        if (Plugin.DebugEnabled) Plugin.Log.LogInfo($"[RestoreGrace] grace ended for heavy '{heavyId}', collision restored");
    }

    // Heavy drops settle into solid colliders, and stacked piles (the quarry is the worst
    // case) get shoved apart by the physics solver every frame, which drifts them into spots
    // the player can't reach and tanks performance. Turning the collider into a trigger keeps
    // pickup working but stops the per-frame shoving, so the piles stay put and you walk
    // through them.
    [HarmonyPostfix]
    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.DoDrop),
        [typeof(Item), typeof(int), typeof(bool)])]
    public static void DropResGameObject_DoDrop_TriggerPostfix(DropResGameObject __instance)
    {
        if (__instance == null || __instance.res == null) return;
        if (!ShouldWalkThrough(__instance.res)) return;
        SetColliderTrigger(__instance, true);
    }

    // Re-apply to every live drop. Called when the player flips either setting and on scene
    // load, so rock already sitting in a save gets caught too, not just new drops.
    internal static void ReapplyHeavyDropTriggers()
    {
        var list = DropsList._instance;
        if (list == null || list.drops == null) return;
        foreach (var drop in list.drops)
        {
            if (!drop) continue;
            var item = drop.res;
            if (item == null) continue;
            if (ShouldWalkThrough(item))
            {
                SetColliderTrigger(drop, true);
            }
            else
            {
                RevertColliderTrigger(drop);
            }
        }
    }

    private static bool ShouldWalkThrough(Item item)
    {
        if (item == null || item.definition == null) return false;
        if (!Plugin.WalkThroughHeavyDrops.Value) return false;
        if (Plugin.IncludeEveryDroppedItem.Value) return true;
        return item.definition.is_big;
    }

    // Remember the collider's original trigger state the first time we touch it, so flipping
    // the setting off restores whatever was really there. Entries are GC'd with the collider.
    private sealed class OriginalState
    {
        public bool IsTrigger;
    }
    private static readonly ConditionalWeakTable<Collider2D, OriginalState> OriginalTriggers = new();

    private static void SetColliderTrigger(DropResGameObject drop, bool isTrigger)
    {
        if (!drop) return;
        var cap = drop.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            ApplyToCollider(cap, isTrigger);
        }
        var circ = drop.GetComponent<CircleCollider2D>();
        if (circ)
        {
            ApplyToCollider(circ, isTrigger);
        }
    }

    private static void RevertColliderTrigger(DropResGameObject drop)
    {
        if (!drop) return;
        var cap = drop.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            RestoreCollider(cap);
        }
        var circ = drop.GetComponent<CircleCollider2D>();
        if (circ)
        {
            RestoreCollider(circ);
        }
    }

    private static void ApplyToCollider(Collider2D col, bool isTrigger)
    {
        if (!OriginalTriggers.TryGetValue(col, out _))
        {
            OriginalTriggers.Add(col, new OriginalState { IsTrigger = col.isTrigger });
        }
        col.isTrigger = isTrigger;
    }

    private static void RestoreCollider(Collider2D col)
    {
        if (!OriginalTriggers.TryGetValue(col, out var orig)) return;
        col.isTrigger = orig.IsTrigger;
    }
}
