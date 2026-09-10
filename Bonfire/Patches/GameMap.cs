using HarmonyLib;

using BonfireSilksong.Teleport;

namespace BonfireSilksong.Patches
{
    internal class GameMap_Patch
    {
        [HarmonyPatch(typeof(GameMap), "Update")]
        class GameMap_Update
        {
            static bool Prefix()
            {
                return !TeleportPatch.InFastTravelDisplayScreen();
            }
        }
    }
}