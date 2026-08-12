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

        [HarmonyPatch(typeof(HeroWispLantern), "OnSceneLoaded")]
        class HeroWispLantern_OnSceneLoaded
        {
            static void Prefix(HeroWispLantern __instance)
            {
                WisdomLogic.WispLanternBuffSpeed(__instance);
            }
        }
    }
}