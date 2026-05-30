namespace Shared;

internal static class DungeonState
{
    // True while the player is inside a dungeon.
    internal static bool IsInDungeon
    {
        get
        {
            if (!MainGame.me || !MainGame.me.dungeon_root) return false;
            if (!MainGame.me.dungeon_root.dungeon_is_loaded_now) return false;

            // A teleport stone can leave dungeon_is_loaded_now true after the player is
            // back on the surface. Use current_zone instead - dungeons have no WorldZone.
            var pc = MainGame.me.player_component;
            return !pc || !pc.current_zone;
        }
    }
}
