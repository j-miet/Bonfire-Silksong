using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Bench fast travel API. Setup: add HandleArrival and CheckMarkerValidity methods from TeleportPatchHelpers
    /// under their respective Harmony patchers.
    ///
    /// Simple usage without extra checks: call PrepareTeleportLocation for target pin, then TeleportToBench.
    /// For pinless locations, manually update tempRespawnScene, tempRespawnMarker and tempRespawnType fields of
    /// PlayerData.instance, then call TeleportToBench.
    /// </summary>
    public class BenchTeleport
    {
        internal static bool IsBenchTeleporting;

        public static readonly FieldInfo ActivePinsField = typeof(MapPin)
            .GetField("_activePins", BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Initializes respawn data: scene, marker and type.
        /// </summary>
        public static bool PrepareTeleportLocation(MapPin closestPin)
        {
            if (closestPin == null || !CanTeleport())
                return false;

            GameMapScene parentScene = (GameMapScene)ParentSceneField.GetValue(closestPin);
            if (parentScene == null)
            {
                Bonfire.Log.LogError("parentScene is null!");
                return false;
            }

            var pd = PlayerData.instance;

            // respawnScene (declared below) corresponds to bundle file names in
            // "Hollow Knight Silksong_Data/StreamingAssets/aa/Standalone<os>/scenes_scenes_scenes" with each
            // word capitalized e.g. song_enclave => Song_Enclave
            // However parentScene.Name might not always correspond to main scene, instead it could be an 
            // identifier for a region/sub-area e.g. "Slab_16_bench" even when scene is "Slab_16". For this reason the 
            // code below attempts to retrieve actual scene name, but if it fails, resorts to a fallback.
            // Furthermore, some scenes might require overriding scene/respawn location/respawn type data manually.
            // In this case targetScene is very useful as it will always be the most unique descriptor available.
            string targetScene = parentScene.Name;

            Bonfire.Log.LogDebug($"[BenchTeleport] Teleport target scene: {targetScene}");

            Dictionary<string, SceneTeleportMap.SceneInfo> teleportMap = SceneTeleportMap.GetTeleportMap();
            if (teleportMap == null)
            {
                Bonfire.Log.LogError("SceneTeleportMap is null!");
                return false;
            }

            string respawnScene = null;
            string respawnMarker = null;
            int respawnType = 1;

            // find target scene for selected bench pin
            if (teleportMap.TryGetValue(targetScene, out var info))
            {
                respawnMarker = info.RespawnPoints.FirstOrDefault(p => p == "RestBench" || p == "TrapBench");

                if (!string.IsNullOrEmpty(respawnMarker) &&
                    info.TransitionGates.Any(t =>
                        t.StartsWith("left") ||
                        t.StartsWith("right") ||
                        t.StartsWith("top") ||
                        t.StartsWith("bot")))
                {
                    respawnScene = targetScene;
                }
            }

            // fallback
            if (string.IsNullOrEmpty(respawnScene))
            {
                foreach (var pair in teleportMap)
                {
                    if (targetScene.StartsWith(pair.Key) || pair.Key.StartsWith(targetScene))
                    {
                        if (pair.Value.TransitionGates.Any(t =>
                            t.StartsWith("left") ||
                            t.StartsWith("right") ||
                            t.StartsWith("top") ||
                            t.StartsWith("bot")))
                        {
                            respawnScene = pair.Key;
                        }

                        respawnMarker = pair.Value.RespawnPoints.FirstOrDefault(p => p.Contains("Bench"));
                    }
                }
            }

            // special cases i.e. scene/marker/type needs to be updated manually.
            switch (targetScene)
            {
                // slab room with two benches (where hornet gets her items back if captured): the lower bench
                case "Slab_16_bench":
                    respawnMarker = "RestBench (1)";
                    break;

                // Songclave bench in Act3: shrine is destroyed, update scene + marker outside
                case "Bellshrine_Enclave":
                    if (pd.blackThreadWorld)
                    {
                        respawnScene = "Song_Enclave";
                        respawnMarker = "RestBench";
                    }
                    break;

                // sinner's road Styx bench in Act3: keep blocked until the void masses are destroyed.
                // in Steel Soul mode, void masses don't exists at all so bench is always available
                case "Dust_11_right":
                    if (pd.blackThreadWorld &&
                        !pd.silkFarmAbyssCoresCleared &&
                        pd.permadeathMode == PermadeathModes.Off)
                    {
                        respawnScene = null;
                        respawnMarker = null;
                    }
                    break;

                // after visiting abyss with fixed diving bell, abyss fast travel becomes normal: at this point there
                // should no longer be anything that can break game progression so player can freely teleport in 
                // and out of abyss.
                // also change bench location from broken bell to fixed one
                case "Abyss_03_bell_fixed":
                    if (!Bonfire.Instance.Status.VisitedAbyssSecondTime)
                        Bonfire.Instance.Status.VisitedAbyssSecondTime = true;

                    respawnScene = "Room_Diving_Bell_Abyss_Fixed";
                    respawnMarker = "RestBench";
                    break;
            }

            if (!AbyssChecks(targetScene))
                return false;

            if (string.IsNullOrEmpty(respawnScene) ||
                string.IsNullOrEmpty(respawnMarker))
            {
                Bonfire.Log.LogError("Couldn't determine respawn destination.");
                return false;
            }

            Bonfire.Log.LogDebug($"Respawn Scene : {respawnScene}");
            Bonfire.Log.LogDebug($"Respawn Marker: {respawnMarker}");
            Bonfire.Log.LogDebug($"Respawn Type  : {respawnType}");

            // these define the fast travel outcome: 
            // - bench scene, 
            // - respawn location within scene
            // - spawn type -> usual values are either 0 = lay on ground, or 1 = sit down)
            pd.tempRespawnScene = respawnScene;
            pd.tempRespawnMarker = respawnMarker;
            pd.tempRespawnType = respawnType;

            return true;
        }

        /// <summary>
        /// Additional checks for target scenes in The Abyss to prevent progress softlocks.
        /// </summary>
        public static bool AbyssChecks(string targetScene)
        {
            // 1. first time visit and trying to teleport out is prevented. Must complete the ascent normally. However
            // can tp between abyss benches normally.
            if (PlayerData.instance.respawnScene.Contains("Abyss_")
                && !targetScene.Contains("Abyss_")
                && !PlayerData.instance.completedAbyssAscent)
                return false;

            // 2. finished ascent and trying to teleport to abyss without using the fixed diving bell is prevented
            if (targetScene.Contains("Abyss_")
                && !Bonfire.Instance.Status.VisitedAbyssSecondTime)
                return false;

            return true;
        }

        /// <summary>
        /// Force game into respawn state and simulate fast travel. Requires PrepareTeleportLocation
        /// to initialize PlayerData respawn fields.
        /// </summary>
        public static void TeleportToBench()
        {
            if (!CanTeleport()) return;

            GameManager gm = GameManager.instance;

            IsBenchTeleporting = true;
            NeedFirstFadeInField.SetValue(gm, true);
            gm.hero_ctrl.cState.dead = true;

            Bonfire.Log.LogDebug("[BenchTeleport] Calling ReadyForRespawn to teleport");

            gm.ReadyForRespawn(false);
        }

        /// <summary>
        /// Get bench pin position in map coordinates.
        /// </summary>
        public static Vector2 GetBenchPinPositionInMapSpace(MapPin pin)
        {
            // In Unity, one could use the transform.parent reparenting trick by temporarily changing parent under
            // different object, which triggers recalculation of coordinates respect to that parent transform then 
            // switch back. This allows easy updates of coordinates from one basis to another. However this is not a 
            // viable strategy unless objects with persistent state or external hooks attached to it that could trigger 
            // side effects. 
            // MapPin is indeed such object: it's placed inside a hierarchial parent structure with multiple 
            // layers where changing its transform could seriously mess the map rending process.
            // In conclusion: loop-based position search is safer to active objects with parent chains, temporary 
            // object are simpler with direct transform reparenting. MapPin fits to former, it uses a parent chain
            // which behaves somewhat like this: pin position -> bench pos -> scene pos -> map pos. Changing pin under
            // map would then recalculate anything below it, very likely to break stuff and lead to a crash.

            Vector3 position = pin.transform.localPosition;
            Transform parent = pin.transform.parent;

            // private field MapMarkerMenu.gameMapObject is the map, gameMapObject.name returns "Game_Map_Hornet(Clone)"
            // which is the loop termination condition. 
            // Loop iterates upward (child -> parent): always updating the pin position relative to 
            // the parents local coordinate basis then moving onto parent of that parent.
            while (parent != null && !parent.gameObject.name.StartsWith("Game_Map_Hornet"))
            {
                position = Vector3.Scale(position, parent.localScale) + parent.localPosition;
                parent = parent.parent;
            }

            return new Vector2(position.x, position.y);
        }


        private static readonly FieldInfo ParentSceneField = typeof(MapPin)
            .GetField("parentScene", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo NeedFirstFadeInField = typeof(GameManager)
            .GetField("needFirstFadeIn", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool CanTeleport()
        {
            if (Bonfire.Instance == null || !Bonfire.Instance.Status.UnlockedFastTravel)
                return false;

            if (GameManager.instance == null || GameManager.instance.IsMemoryScene())
                return false;

            return PlayerData.instance.atBench;
        }
    }
}