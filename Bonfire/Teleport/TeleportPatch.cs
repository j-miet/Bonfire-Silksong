using System.Collections.Generic;
using System.Linq;

namespace BonfireSilksong.Teleport
{
    /// <summary>
    /// Bench teleport logic which needs to be injected into game code.
    /// </summary>
    public static class TeleportPatch
    {
        /// <summary>
        /// HeroController.FinishedEnteringScene (Postfix):
        /// Handle transition post-check: set spawn location to targeted bench and save game.
        /// </summary>
        public static void HandleArrival(HeroController instance)
        {
            if (!BenchTeleport.IsBenchTeleporting)
                return;

            Bonfire.Log.LogDebug("[TeleportPatch] Set spawn location");

            Bonfire.Log.LogDebug($" - gravity: {HeroController.instance.IsGravityApplied}");

            var markers = RespawnMarker.Markers
                .FirstOrDefault(r => r.gameObject != null &&
                                r.gameObject.activeInHierarchy &&
                                r.name == PlayerData.instance.respawnMarkerName);

            if (markers != null)
            {
                var benchFSM = FSMUtility.LocateFSM(markers.gameObject, "Bench Control");

                // properly update spawn location. This will also ensure the save slot displays image of correct 
                // main/side area
                benchFSM?.SetState("Set Spawn Location");
                benchFSM?.SendEventSafe("OVERRIDE");
                Bonfire.Log.LogDebug("- Updated spawn location");
            }
            else
            {
                Bonfire.Log.LogDebug("- Failed to update spawn location");
            }

            instance.proxyFSM.SendEventSafe("HeroCtrl-EnteringScene");

            GameManager.instance.SaveGame(null);

            BenchTeleport.IsBenchTeleporting = false;
        }

        /// <summary>
        /// GameManager.EnterHero (Prefix): Check if the bench location is valid for respawning after player is loaded 
        /// into scene. Settles for a fallback in case the value set by BenchTeleport turned out not to be a valid one.
        /// </summary>
        public static void CheckMarkerValidity(GameManager instance)
        {
            if (!BenchTeleport.IsBenchTeleporting || !instance.RespawningHero)
                return;

            string scene = instance.playerData.tempRespawnScene;
            string marker = instance.playerData.tempRespawnMarker;

            Bonfire.Log.LogDebug($"[TeleportPatch] EnterHero: scene={scene}, marker={marker}");

            List<RespawnMarker> markers = RespawnMarker.Markers
                .Where(r =>
                    r.gameObject != null &&
                    r.gameObject.scene.name != "DontDestroyOnLoad" &&
                    r.gameObject.activeInHierarchy)
                .ToList();

            // compare value fetched from static marker in TeleportToBench table to actual scene markers
            RespawnMarker found = markers.FirstOrDefault(r => r.name == marker);
            if (found != null)
            {
                instance.playerData.respawnMarkerName = marker;
                Bonfire.Log.LogDebug("Destination marker exists.");
                return;
            }

            Bonfire.Log.LogWarning("Marker missing, selecting fallback...");

            found = markers.FirstOrDefault(r => r.name.Contains("Bench")) ??
                markers.FirstOrDefault(r => r.name == "Death Respawn Marker") ??
                markers.FirstOrDefault();

            if (found != null)
            {
                instance.playerData.tempRespawnMarker = found.name;
                instance.playerData.respawnMarkerName = found.name;

                Bonfire.Log.LogDebug($"Fallback marker = {found.name}");
            }
        }

        /// <summary>
        /// InventoryPaneInput.Update (Prefix): block default input behavior in map screen.
        /// </summary>
        public static bool BlockedInputInFastTravelScreen()
        {
            if (TeleportGUI.FastTravelDisplay)
                return true;

            return false;
        }
    }
}