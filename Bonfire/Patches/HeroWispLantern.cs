using HarmonyLib;

using BonfireSilksong.Leveling;
using System.Collections;

namespace BonfireSilksong.Patches
{
    internal class HeroWispLantern_Patch
    {
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_SpawnWispsRoutine
        {
            static void Prefix()
            {
                WisdomStat.WispLanternSilkOnSpawn();
            }
        }

        // private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        [HarmonyPatch(typeof(HeroWispLantern), "OnSceneLoaded")]
        class HeroWispLantern_OnSceneLoaded
        {
            static void Prefix(HeroWispLantern __instance)
            {
                WisdomStat.ApplyWispLanternSpeedBuff(__instance);
            }
        }

        // private void ToggleWispLantern(bool isEquipped)
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_ToggleWispLantern
        {
            static bool Prefix(ref IEnumerator __result)
            {
                if (WisdomStat.BlockSummons)
                {
                    __result = LevelingPatch.EmptyRoutine();
                    return false;
                }

                return true;
            }
        }
    }
}