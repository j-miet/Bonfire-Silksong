using HarmonyLib;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class SilkSpool_Patch
    {
        [HarmonyPatch(typeof(SilkSpool), "BindCost", MethodType.Getter)]
        class SilkSpool_BindCost
        {
            static void Postfix(ref float __result)
            {
                __result = IntelligenceStat.UpdateBindCostVisual(__result);
            }
        }
    }
}