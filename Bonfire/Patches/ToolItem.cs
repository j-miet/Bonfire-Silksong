using HarmonyLib;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class ToolItem_Patch
    {
        [HarmonyPatch(typeof(ToolItem), "IsEquipped", MethodType.Getter)]
        class ToolItem_IsEquipped
        {
            static void Postfix(ToolItem __instance, ref bool __result)
            {
                if (GlobalStat.GetCompassToolEffect(__instance) ||
                    GlobalStat.GetWreathOfPurityToolEffect(__instance))
                {
                    __result = true;
                }
            }
        }
    }
}