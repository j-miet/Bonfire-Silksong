using HarmonyLib;

namespace BonfireSilksong
{
    internal class ToolItemManager_Patch
    {
        // public static int GetToolStorageAmount(ToolItem tool)
        [HarmonyPatch(typeof(ToolItemManager), "GetToolStorageAmount")]
        class ToolItemManager_GetToolStorageAmount
        {
            static void Postfix(ToolItem tool, ref int __result)
            {
                if (ArtificeStat.CheckArtificeMilestone(1) && tool?.name == "Pimpilo")
                    __result *= 2;
            }
        }
    }
}