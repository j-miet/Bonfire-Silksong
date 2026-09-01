using HarmonyLib;

namespace BonfireSilksong
{
    internal class GameManager_Patch
    {
        [HarmonyPatch(typeof(GameManager), "EnterHero")]
        class GameManager_EnterHero
        {
            static void Prefix(GameManager __instance)
            {
                TeleportPatch.CheckMarkerValidity(__instance);
            }
        }
    }
}