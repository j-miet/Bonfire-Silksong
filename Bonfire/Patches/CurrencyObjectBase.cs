using HarmonyLib;

namespace BonfireSilksong
{
    internal class CurrencyObjectBase_Patch
    {
        [HarmonyPatch(typeof(CurrencyObjectBase), "MagnetToolIsEquipped")]
        class CurrencyObjectBase_MagnetToolIsEquipped
        {
            static void Postfix(ref bool __result)
            {
                if (GlobalStat.CheckGlobalMilestone(GlobalMilestone.QOLTOOLS) &&
                    GlobalStat.GetMagnetiteBroochToolEffect())
                {
                    __result = true;
                }
            }
        }
    }
}