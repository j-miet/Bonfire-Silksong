using HarmonyLib;

namespace BonfireSilksong
{
    internal class HeroWispLantern_Patch
    {
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_SpawnWispsRoutine
        {
            static void Prefix()
            {
                WisdomLogic.WispLanternSilkOnSpawn();
            }
        }

        // private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        [HarmonyPatch(typeof(HeroWispLantern), "OnSceneLoaded")]
        class HeroWispLantern_OnSceneLoaded
        {
            static void Prefix(HeroWispLantern __instance)
            {
                WisdomLogic.ApplyWispLanternSpeedBuff(__instance);
            }
        }

        // private void ToggleWispLantern(bool isEquipped)
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_ToggleWispLantern
        {
            static bool Prefix()
            {
                if (WisdomLogic.BlockSummons)
                    return false;

                return true;
            }
        }
    }
}