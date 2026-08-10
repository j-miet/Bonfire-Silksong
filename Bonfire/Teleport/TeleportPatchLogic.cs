using System.Collections.Generic;
using System.Linq;

namespace BonfireSilksong
{
    public static class TeleportPatchLogic
    {
        /// <summary>
        /// HeroController.FinishedEnteringScene (Postfix):
        /// Handle transition post-check: set spawn location to targeted bench, update location and save game.
        /// </summary>
        public static void HandleArrival(HeroController instance)
        {
            if (!BenchTeleport.IsBenchTeleporting)
                return;

            BenchTeleport.IsBenchTeleporting = false;

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
                Bonfire.Log.LogDebug("[BenchTeleport] Set spawn location");
            }

            instance.proxyFSM.SendEventSafe("HeroCtrl-EnteringScene");

            // teleporting will update blue hp too early so call update again after a short delay for refresh state
            Bonfire.Instance.StartCoroutine(LevelingSystem.CallDelayedUpdateBlueHP());

            GameManager.instance.SaveGame(null);
        }

        /// <summary>
        /// GameManager.EnterHero (Prefix):
        /// Check if the bench location is valid for respawning after player is loaded into scene.
        /// Settles for a fallback in case the value set by BenchTeleport turned out not to be a valid bench marker for
        /// target scene.
        /// </summary>
        public static void CheckMarkerValidity(GameManager instance)
        {
            if (!BenchTeleport.IsBenchTeleporting)
                return;

            if (!instance.RespawningHero)
                return;

            string scene = instance.playerData.tempRespawnScene;
            string marker = instance.playerData.tempRespawnMarker;

            Bonfire.Log.LogDebug($"[BenchTeleport] EnterHero: scene={scene}, marker={marker}");

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
        /// InventoryPaneInput.Update (Prefix): block ESC/inventory pane navigation when fast travel menu is open.
        /// GameMap.Update (Prefix): block map panning when fast travel menu is open.
        /// </summary>
        public static bool InFastTravelDisplayScreen()
        {
            if (BonfireGUI.Teleport != null && BonfireGUI.Teleport.FastTravelDisplay)
                return true;

            return false;
        }
    }
}