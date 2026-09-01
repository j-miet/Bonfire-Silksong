using HarmonyLib;

namespace BonfireSilksong
{
    internal static class ClockworkHatchling_Patch
    {
        [HarmonyPatch(typeof(ClockworkHatchling), "HitLanded")]
        class ClockworkHatchling_HitLanded
        {
            static void Prefix(ClockworkHatchling __instance)
            {
                WisdomStat.UpdateCogflyCurrentHP(__instance);
            }
        }

        [HarmonyPatch(typeof(ClockworkHatchling), "StartAttackCooldown")]
        class ClockworkHatchling_StartAttackCooldown
        {
            static void Postfix(ClockworkHatchling __instance)
            {
                WisdomStat.UpdateCogflyAttackCooldown(__instance);
            }
        }
    }
}