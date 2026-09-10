using HarmonyLib;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
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