using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GlobalEnums;
using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Bench fast travel system.
    /// Most of the core functionality is from "BonfireTeleport" mod by kassent. This has then been further simplified/
    /// modified to fit BonfireSilksong's needs.
    /// Doesn't include all the logic so this is more like a lite version. Nonetheless it's their hard work that has 
    /// made allowed this feature to be added.
    /// BonfireTeleport Nexusmods page: https://www.nexusmods.com/hollowknightsilksong/mods/156
    /// </summary>
    public class BenchTeleport : MonoBehaviour
    {
        internal static bool IsBenchTeleporting;

        private static readonly float ClickThreshold = Mathf.Sqrt(0.2f);

        private static readonly FieldInfo ActivePinsField = typeof(MapPin)
            .GetField("_activePins", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly FieldInfo ParentSceneField = typeof(MapPin)
            .GetField("parentScene", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo NeedFirstFadeInField = typeof(GameManager).
            GetField("needFirstFadeIn", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo GameMapObjectField = typeof(MapMarkerMenu)
            .GetField("gameMapObject", BindingFlags.Instance | BindingFlags.NonPublic);

        private static void TeleportToBench(MapPin closestPin)
        {
            if (closestPin == null)
                return;

            GameMapScene parentScene = (GameMapScene)ParentSceneField.GetValue(closestPin);
            if (parentScene == null)
            {
                Bonfire.Log.LogError("parentScene is null!");
                return;
            }

            string targetScene = parentScene.Name;

            Bonfire.Log.LogDebug($"Teleport target scene: {targetScene}");

            Dictionary<string, SceneTeleportMap.SceneInfo> teleportMap = SceneTeleportMap.GetTeleportMap();
            if (teleportMap == null)
            {
                Bonfire.Log.LogError("SceneTeleportMap is null!");
                return;
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

                        respawnMarker = pair.Value.RespawnPoints
                            .FirstOrDefault(p => p.Contains("Bench"));
                    }
                }
            }

            // special cases
            if (targetScene == "Slab_16_bench")
                respawnMarker = "RestBench (1)";

            if (targetScene == "Ant_17")
                respawnType = PlayerData.instance.antBenchTrapDefused ? 1 : 0;

            if (targetScene == "Abyss_03_bell_broken")
            {
                respawnScene = "Room_Diving_Bell_Abyss";
                respawnMarker = "RestBench";
            }

            if (targetScene == "Abyss_03_bell_fixed")
            {
                respawnScene = "Room_Diving_Bell_Abyss_Fixed";
                respawnMarker = "RestBench";
            }

            if (targetScene == "Bellshrine_Enclave" &&
                PlayerData.instance.blackThreadWorld)
            {
                respawnScene = "Song_Enclave";
                respawnMarker = "RestBench";
            }

            if (targetScene == "Dust_11_right")
            {
                var pd = PlayerData.instance;

                if (pd.blackThreadWorld &&
                    !pd.silkFarmAbyssCoresCleared &&
                    pd.permadeathMode == PermadeathModes.Off)
                {
                    respawnScene = null;
                    respawnMarker = null;
                }
            }

            if (string.IsNullOrEmpty(respawnScene) ||
                string.IsNullOrEmpty(respawnMarker))
            {
                Bonfire.Log.LogError("Couldn't determine respawn destination.");
                return;
            }

            Bonfire.Log.LogDebug($"Respawn Scene : {respawnScene}");
            Bonfire.Log.LogDebug($"Respawn Marker: {respawnMarker}");
            Bonfire.Log.LogDebug($"Respawn Type  : {respawnType}");

            // these define the respawn outcome: scene, bench, type
            PlayerData.instance.tempRespawnScene = respawnScene;
            PlayerData.instance.tempRespawnMarker = respawnMarker;
            PlayerData.instance.tempRespawnType = respawnType;

            GameManager gm = GameManager.instance;

            IsBenchTeleporting = true;
            NeedFirstFadeInField.SetValue(gm, true);
            gm.hero_ctrl.cState.dead = true;

            Bonfire.Log.LogDebug("Calling ReadyForRespawn...");

            gm.ReadyForRespawn(false);
        }

        private static bool CanTeleport()
        {
            if (Bonfire.Instance == null || !Bonfire.Instance.Status.UnlockedFastTravel)
                return false;

            if (GameManager.instance == null)
                return false;

            if (GameManager.instance.IsMemoryScene())
                return false;

            return PlayerData.instance.atBench;
        }

        // get bench pin position in map coordinates
        // 
        // technically the same gameMapObject trick could be done here what GetClickPositionInMapSpace uses.
        // however pin position getter is called once for each bench pin available vs click only once per pin location.
        // Also pin location is a temporary object, which mean it
        // - doesn't trigger side effects (just a cursor position tracker, no hooks are attached to it)
        // - cause permanent issues should an error be thrown during processing it
        // Finally: performance costs for triggering transformation calculations repeatedly when reparenting could be
        // an issue: loop iteration is much cheaper for larger object count
        private static Vector2 GetBenchPinPosition(MapPin pin)
        {
            Vector3 position = pin.transform.localPosition;
            Transform parent = pin.transform.parent;

            while (parent != null && !parent.gameObject.name.StartsWith("Game_Map_Hornet"))
            {
                position = Vector3.Scale(position, parent.localScale) + parent.localPosition;
                parent = parent.parent;
            }

            return new Vector2(position.x, position.y);
        }

        // get custom pin's placement position in map coordinates
        private static Vector2 GetPinPositionInMapSpace(MapMarkerMenu menu)
        {
            GameObject gameMapObject = (GameObject)GameMapObjectField.GetValue(menu); // map
            GameObject placementBox = menu.placementBox; // pin location

            // reads placementBox's position in map space:
            // - temporarily reparent placementBox under gameMapObject: Unity will preserve object's world position and 
            // automatically recalculates the localPosition of assigned transform.parent. Because it now gets passed
            // gameMapObject, it will instead calculate the pin box position relative to gameMapObject's local space,
            // that is, the map space 
            // - get position and produce return value vector
            // - restoring its original parent afterwards to revert chances i.e. force to again recalculate the 
            // position, returning back to original values
            // Why is this required: pin positions from GetBenchPosition are expressed relative to Game_Map_Hornet, 
            // while placementBox lives under a different parent. The localPosition is always expressed relative to
            // its parent transform. Thus here this means that particular parent of placementBox, whatever it is.
            Transform originalParent = placementBox.transform.parent;

            placementBox.transform.parent = gameMapObject.transform;
            Vector3 localPosition = placementBox.transform.localPosition;
            Vector2 result = new Vector2(localPosition.x, localPosition.y);
            placementBox.transform.parent = originalParent;

            return result;
        }

        /* patched methods */

        // handle transition post-check
        [HarmonyPatch(typeof(HeroController), "FinishedEnteringScene")]
        class HeroController_FinishedEnteringScene
        {
            static void Postfix(HeroController __instance)
            {
                if (!IsBenchTeleporting)
                    return;

                IsBenchTeleporting = false;

                __instance.proxyFSM.SendEvent("HeroCtrl-EnteringScene");

                Bonfire.Log.LogDebug("Sent HeroCtrl-EnteringScene");
            }
        }

        // check if the bench location is valid for respawning after the scene has loaded
        // settles for a fallback in case the value set by TeleportToBench turned out not to be a valid bench marker for
        // target scene
        [HarmonyPatch(typeof(GameManager), "EnterHero")]
        class GameManager_EnterHero
        {
            static void Prefix(GameManager __instance)
            {
                if (!IsBenchTeleporting)
                    return;

                if (!__instance.RespawningHero)
                    return;

                string scene = __instance.playerData.tempRespawnScene;
                string marker = __instance.playerData.tempRespawnMarker;

                Bonfire.Log.LogDebug($"EnterHero: scene={scene}, marker={marker}");

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
                    Bonfire.Log.LogDebug("Destination marker exists.");
                    return;
                }

                Bonfire.Log.LogWarning("Marker missing, selecting fallback...");

                found = markers.FirstOrDefault(r => r.name.Contains("Bench")) ??
                    markers.FirstOrDefault(r => r.name == "Death Respawn Marker") ??
                    markers.FirstOrDefault();

                if (found != null)
                {
                    __instance.playerData.tempRespawnMarker = found.name;

                    Bonfire.Log.LogDebug($"Fallback marker = {found.name}");
                }
            }
        }

        // override map pin placement function by fast travel teleport when player uses pin very close/on top of a 
        // bench icon
        [HarmonyPatch(typeof(MapMarkerMenu), "PlaceMarker")]
        private class MapMarkerMenu_PlaceMarker
        {
            static bool Prefix(MapMarkerMenu __instance)
            {
                try
                {
                    if (__instance == null)
                        return true;

                    if (!CanTeleport())
                        return true;

                    GameObject placementCursor = __instance.placementCursor;

                    if (placementCursor == null)
                    {
                        Bonfire.Log.LogError("placementCursor is null!");
                        return true;
                    }

                    Vector2 placedPinPos = GetPinPositionInMapSpace(__instance);

                    Bonfire.Log.LogDebug($"Pin cursor position (map space): {placedPinPos}");

                    List<MapPin> activePins = (List<MapPin>)ActivePinsField.GetValue(null);
                    if (activePins == null)
                    {
                        Bonfire.Log.LogError("MapPin._activePins is null!");
                        return true;
                    }

                    float closestDistance = float.MaxValue;
                    MapPin closestPin = null;

                    foreach (MapPin pin in activePins)
                    {
                        if (pin == null ||
                            !pin.IsActive ||
                            !pin.gameObject.activeSelf ||
                            !pin.gameObject.activeInHierarchy)
                            continue;

                        SpriteRenderer sr = pin.GetComponent<SpriteRenderer>();

                        if (sr?.sprite?.name != "pin_bench") // get only bench pins
                            continue;

                        Vector2 benchPos = GetBenchPinPosition(pin);
                        float distance = Vector2.Distance(placedPinPos, benchPos);

                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestPin = pin;
                        }
                    }

                    // only trigger teleport if the click actually landed directly/very near a bench pin.
                    if (closestPin == null || closestDistance >= ClickThreshold)
                    {
                        Bonfire.Log.LogDebug(
                            $"No bench pin is close enough to trigger traveling: closest distance = {closestDistance}");

                        return true;
                    }

                    Bonfire.Log.LogDebug($"Closest bench: {closestPin.name}, distance = {closestDistance}");

                    // close marker menu before teleporting, otherwise it stays open and requires a manual close into
                    // reopen from the player
                    __instance.Close();

                    TeleportToBench(closestPin);

                    return false; // cancel the actual pin placement with 'false'
                }
                catch (Exception ex)
                {
                    Bonfire.Log.LogError(ex);
                    return true;
                }
            }
        }
    }
}
