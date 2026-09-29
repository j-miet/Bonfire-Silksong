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
                // Bonfire.Log.LogDebug($"[GameManager.EnterHero] " +
                //     $"Dead : {HeroController.instance.cState.dead}, Respawning : {__instance.RespawningHero}");

                TeleportPatch.CheckMarkerValidity(__instance);

                if (__instance.RespawningHero &&
                    (BenchTeleport.IsBenchTeleporting || HeroController.instance.cState.dead))
                {
                    // despawn cogfly object when using fast travel teleport, or when player respawn after dying.
                    // Otherwise each cogfly creates a duplicate which is detached from player and just hovers at 
                    // target scene after arrival. Gameplay-wise this doesn't matter as cogflies will despawn on scene 
                    // transitions, but this fixes the uninteded 'visual bug'.
                    CogflySpawner.DespawnAllCogflies();
                }
            }
        }
    }
}