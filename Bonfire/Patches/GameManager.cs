using HarmonyLib;

using BonfireSilksong.Teleport;
using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class GameManager_Patch
    {
        [HarmonyPatch(typeof(GameManager), "EnterHero")]
        class GameManager_EnterHero
        {
            static void Prefix(GameManager __instance)
            {
                TeleportPatch.CheckMarkerValidity(__instance);

                if (BenchTeleport.IsBenchTeleporting && __instance.RespawningHero)
                {
                    // despawn cogfly object when using fast travel teleport. Otherwise each cogfly creates a duplicate 
                    // which is detached from player and just hovers at target scene after arrival. Gameplay-wise this 
                    // doesn't matter as cogflies will despawn on scene transitions, but this fixes the uninteded 
                    // 'visual bug'.
                    CogflySpawner.DespawnAllCogflies();
                }
            }
        }
    }
}