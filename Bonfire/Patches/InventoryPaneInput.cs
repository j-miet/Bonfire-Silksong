using HarmonyLib;

using BonfireSilksong.Teleport;

namespace BonfireSilksong.Patches
{
    internal class InventoryPaneInput_Patch
    {
        [HarmonyPatch(typeof(InventoryPaneInput), "Update")]
        class InventoryPaneInput_Update
        {
            static bool Prefix()
            {
                return !TeleportPatch.BlockedInputInFastTravelScreen();
            }
        }
    }
}