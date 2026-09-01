using HarmonyLib;

namespace BonfireSilksong
{
    internal class HeroNailImbuement_Patch
    {
        [HarmonyPatch(typeof(HeroNailImbuement), "Update")]
        class HeroNailImbuement_Update
        {
            static void Prefix(HeroNailImbuement __instance)
            {
                StrengthStat.RefreshFlintslateBuffDuration(__instance);
            }
        }
    }
}