using HarmonyLib;

namespace BonfireSilksong
{
    internal class SilkSpool_Patch
    {
        [HarmonyPatch(typeof(SilkSpool), "BindCost", MethodType.Getter)]
        class SilkSpool_BindCost_Getter
        {
            static void Postfix(ref float __result)
            {
                __result = IntelligenceStat.UpdateBindCostVisual(__result);
            }
        }
    }
}