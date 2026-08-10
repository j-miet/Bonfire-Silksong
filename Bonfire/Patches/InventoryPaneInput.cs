using HarmonyLib;

namespace BonfireSilksong
{
    internal class InventoryPaneInput_Patch
    {
        [HarmonyPatch(typeof(InventoryPaneInput), "Update")]
        class InventoryPaneInput_Update
        {
            static bool Prefix()
            {
                return !TeleportPatchHelpers.InFastTravelDisplayScreen();
            }
        }
    }
}