using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// GUI logic + helpers
    /// </summary>
    public static class GUILogic
    {
        // MapPin -> (location scene, location bench marker) tuple
        // Used in: UpdateBenchPins stores data by using MapPin object, this ensures each key being unique. 
        // For each pin, the scene and marker name are also stored. This is because in UpdateActiveLocationData,
        // pin and scene are used for categorizing bench locations by area (see _benchActiveLocationData below), but
        // in rare cases, different pins could get same scene names from BenchTeleport.PrepareTeleportLocation thus
        // requiring an extra secondary identifier i.e. the marker name.
        public static Dictionary<MapPin, (string scene, string marker)> BenchPinScenes { get; private set; } =
            new Dictionary<MapPin, (string, string)>();

        // Map area -> List of all benches in that area as (location scene, location map pin) tuples
        // Used in: UpdateActiveLocationData divides all available benches under main areas. Then DrawBenchListPanel
        // creates the GUI list by iterating over each area and it's bench locations
        public static Dictionary<string, List<(string scene, MapPin mapPin)>> BenchActiveLocationData
        { get; private set; } = new Dictionary<string, List<(string, MapPin)>>();

        /// <summary>
        /// Call UpdateBenchPins with a small delay.
        /// </summary>
        public static IEnumerator DelayedPinUpdate()
        {
            yield return new WaitForSeconds(1f);
            UpdateBenchPins();
        }

        /// <summary>
        /// Prevent most keyboard inputs in bench menu map screen, the rest require prefix patching of
        /// InventoryPaneInput.Update.
        /// </summary>
        public static void BlockInventoryInputs()
        {
            var actions = GameManager.instance.inputHandler.inputActions;
            actions.PaneLeft.Enabled = false;
            actions.PaneRight.Enabled = false;
            actions.OpenInventoryJournal.Enabled = false;
            actions.OpenInventoryTools.Enabled = false;
            actions.OpenInventoryQuests.Enabled = false;

            // allow these through: patched InventoryPaneInput.Update will prevent any inputs from doing their
            // normal behavior while map is open. However it still allows them to register which allows BonfireGUI
            // to check if they were pressed and perform actions
            //
            //actions.OpenInventory.Enabled = false;
            //actions.OpenInventoryMap.Enabled = false;
            //actions.QuickMap.Enabled = false;
        }

        /// <summary>
        /// Reverts keyboard input blocking.
        /// </summary>
        public static void RestoreInventoryInputs()
        {
            var actions = GameManager.instance.inputHandler.inputActions;
            actions.PaneLeft.Enabled = true;
            actions.PaneRight.Enabled = true;
            actions.OpenInventoryJournal.Enabled = true;
            actions.OpenInventoryTools.Enabled = true;
            actions.OpenInventoryQuests.Enabled = true;

            //actions.OpenInventory.Enabled = true;
            //actions.OpenInventoryMap.Enabled = true;
            //actions.QuickMap.Enabled = true;
        }

        /// <summary>
        /// Bench location data parser, updated data is inserted into BenchActiveLocationData.
        /// </summary>
        public static void UpdateActiveLocationData()
        {
            Bonfire.Log.LogDebug("== Bench location data ==");
            foreach (KeyValuePair<MapPin, (string scene, string marker)> bench in BenchPinScenes)
            {
                try
                {
                    string s = bench.Value.scene;
                    string m = bench.Value.marker;
                    if (s == null || m == null) continue;

                    var p = bench.Key;

                    Bonfire.Log.LogDebug("Scene: " + s);
                    Bonfire.Log.LogDebug("Marker: " + bench.Value.marker);

                    string customName;
                    string areaName;

                    // get map area name:

                    // main logic
                    if (_benchStaticLocations.ContainsKey(s))
                    {
                        var value = _benchStaticLocations[s];
                        var name = value.customName;

                        // leaving custom name empty defaults to internal scene name e.g. "Tut_03"
                        customName = name != "" ? name : s;
                        areaName = value.areaName;
                    }
                    else
                    {
                        customName = s;
                        areaName = "Other";
                    }

                    // special cases where scene name is same and marker name must be used for unique identification
                    if (s == "Slab_16" && bench.Value.marker == "RestBench (1)")
                    {
                        customName = "Slab_16_bench";
                        if (_benchStaticLocations.ContainsKey(customName))
                            customName = _benchStaticLocations[customName].customName;
                    }

                    // some scenes have duplicate markers for same bench location. This prevents adding more than one
                    if ((s == "Tube_Hub" || s == "Bone_10")
                        && bench.Value.marker == "RestBench"
                        && BenchActiveLocationData.ContainsKey(areaName))
                    {
                        Bonfire.Log.LogDebug($"Scene '{s}' duplicate check =>");
                        bool alreadyFound = false;

                        foreach (var (scene, _) in BenchActiveLocationData[areaName])
                        {
                            // compare to customName as that's what is stored. This of course will be either original
                            // value or custom name, depending if latter has been defined in _benchStaticLocations
                            if (string.Equals(scene, customName))
                            {
                                Bonfire.Log.LogDebug("Found duplicate!");
                                alreadyFound = true;
                                break;
                            }
                        }

                        if (alreadyFound) continue;
                    }

                    if (!BenchActiveLocationData.ContainsKey(areaName))
                        BenchActiveLocationData.Add(areaName, new List<(string, MapPin)>());

                    BenchActiveLocationData[areaName].Add((customName, p));
                }
                catch (Exception e)
                {
                    Bonfire.Log.LogError($"[UpdateActiveLocationData]: {e}");
                }
            }
        }

        /// <summary>
        /// Replace tracked FSM with one from current bench location.
        /// </summary>
        public static bool UpdateCurrentBenchFSM()
        {
            var markers = RespawnMarker.Markers
                    .FirstOrDefault(r => r.gameObject != null &&
                                    r.gameObject.activeInHierarchy &&
                                    r.name == PlayerData.instance.respawnMarkerName);

            Bonfire.Log.LogDebug("Start scene name: " + PlayerData.instance.respawnScene);
            Bonfire.Log.LogDebug("Start marker name: " + PlayerData.instance.respawnMarkerName);

            if (markers == null) return false;

            var benchFSM = FSMUtility.LocateFSM(markers.gameObject, "Bench Control");
            if (benchFSM == null) return false;

            _currentBenchFSM = benchFSM;
            Bonfire.Log.LogDebug("Updated Bench FSM");

            return true;
        }

        /// <summary>
        /// Update BenchPinScenes and BenchActiveLocationData
        /// </summary>
        public static void UpdateBenchPins()
        {
            BenchPinScenes.Clear();
            var gameMap = GameManager.instance?.gameMap;
            if (gameMap == null) return;

            List<MapPin> activePins = (List<MapPin>)BenchTeleport.ActivePinsField.GetValue(null);
            if (activePins == null) return;

            foreach (var pin in activePins)
            {
                if (pin == null
                    || !pin.IsActive
                    || !pin.gameObject.activeSelf
                    || !pin.gameObject.activeInHierarchy)
                    continue;

                var sr = pin.GetComponent<SpriteRenderer>();
                if (sr?.sprite?.name != "pin_bench") continue;

                if (!BenchTeleport.PrepareTeleportLocation(pin))
                    continue;

                string sceneName = PlayerData.instance.tempRespawnScene;
                string markerName = PlayerData.instance.tempRespawnMarker;
                BenchPinScenes.Add(pin, (sceneName, markerName));
            }

            BenchActiveLocationData.Clear();
            UpdateActiveLocationData(); // must only call this after filling _benchPinScenes!

            Bonfire.Log.LogDebug($"Found {BenchPinScenes.Count} bench pins");
            Bonfire.Log.LogDebug("GUI: bench pin respawn scenes:");
            foreach (var p in BenchPinScenes)
                Bonfire.Log.LogDebug(p.Value);
        }

        /// <summary>
        /// Open in-game map screen
        /// </summary>
        public static void SendOpenMapFSM()
        {
            _currentBenchFSM.SetState("Open Map");
            _currentBenchFSM.SendEvent("INV MAP");
        }

        /// <summary>
        /// Close map screen via FSM event calls
        /// </summary>
        public static IEnumerator CloseInventoryMap()
        {
            InventoryPaneList inventoryPaneList = null;
            InventoryPaneInput panelInput = null;

            try
            {
                inventoryPaneList = (InventoryPaneList)InventoryPaneListInstanceField.GetValue(null);
                if (inventoryPaneList == null) yield break;

                panelInput = inventoryPaneList.GetComponentInChildren<InventoryPaneInput>();
                if (panelInput == null) yield break;
            }
            catch (Exception e)
            {
                Bonfire.Log.LogError($"CloseInventoryMap init error: {e}");
                yield break;
            }

            FSMUtility.SendEventToGameObject(panelInput.gameObject, "UI CANCEL");
            FSMUtility.SendEventToGameObject(panelInput.gameObject, "UI CANCEL");
            yield return new WaitForSecondsRealtime(0.25f);

            try
            {
                if (inventoryPaneList != null)
                    inventoryPaneList.CloseBlocked = false;

                if (GameManager.instance?.inventoryFSM != null)
                    GameManager.instance.inventoryFSM.SendEventSafe("BACK");
            }
            catch (Exception e)
            {
                Bonfire.Log.LogError($"CloseInventoryMap close error: {e}");
            }

            yield return new WaitUntil(() =>
                GameManager.instance == null ||
                GameManager.instance.inventoryFSM == null ||
                GameManager.instance.inventoryFSM.ActiveStateName == "Closed");

            try
            {
                RestoreInventoryInputs();
                if (_currentBenchFSM != null && _currentBenchFSM.gameObject != null)
                    _currentBenchFSM.SendEvent("FINISHED");
            }
            catch (Exception e)
            {
                Bonfire.Log.LogError($"CloseInventoryMap restore error: {e}");
            }
        }

        /// <summary>
        /// Check if player is able to level up.
        /// </summary>
        public static bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
        }

        /// <summary>
        /// Reset all pending (unapplied) stat allocations.
        /// </summary>
        public static void ResetPendingLevelUp()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.StrengthIncrease = 0;
            ls.DexterityIncrease = 0;
            ls.IntelligenceIncrease = 0;
            ls.ArtificeIncrease = 0;
            ls.WisdomIncrease = 0;
            ls.ResilienceIncrease = 0;
            ls.LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;

            Bonfire.Instance.UpdateBindCostFSM();
        }

        /// <summary>
        /// Check if game is in inventory screen.
        /// </summary>
        public static bool InInventory()
        {
            GameObject go = GameObject.FindGameObjectWithTag("Inventory Top");
            if (go == null) return false;

            PlayMakerFSM fsm = FSMUtility.LocateFSM(go, "Inventory Control");
            if (fsm == null) return false;

            FsmBool open = fsm.FsmVariables.GetFsmBool("Open");
            return open != null && open.Value;
        }


        // Custom scene names for teleport locations
        // Key is the raw scene value from BenchTeleport.PrepareToTeleport
        //   => make sure to use final scene value for manually handled special cases!
        // Value is a tuple (custom name, map area). Custom name is optional: using empty string "" reverts back to
        // raw scene name. Map area is used in categorizing bench scenes.
        // If no key-value pair is found for specific scene, scene defaults to raw name and map area becomes "Other"
        private static readonly Dictionary<string, (string customName, string areaName)> _benchStaticLocations =
            new Dictionary<string, (string, string)>
            {
                {"Tut_03", ("Ruined Chapel", "Mosslands")},
                {"Tut_04", ("Ruined Chapel Interior", "Mosslands")},
                {"Bonetown", ("Bone Bottom", "Mosslands")},
                {"Mosstown_02c", ("Mosshome", "Mosslands")},

                {"Weave_07", ("Rest", "Weavenest Atla")},

                {"Bone_01c", ("Entrance", "The Marrow")},
                {"Bone_04", ("Crossing", "The Marrow")},
                {"Bone_10", ("Enclave", "The Marrow")},
                {"Bone_12", ("Cells", "The Marrow")},
                {"Bellshrine", ("Bellshrine", "The Marrow")},

                {"Crawl_08", ("Alchemist", "Wormways")},

                {"Dock_01", ("Entrance West", "Deep Docks")},
                {"Dock_06_Church", ("Ascent Top", "Deep Docks")},
                {"Dock_10", ("Sauna", "Deep Docks")},
                {"Bellshrine_05", ("Bellshrine", "Deep Docks")},
                {"Room_Forge", ("Forge", "Deep Docks")},
                {"Room_Diving_Bell", ("Diving Bell", "Deep Docks")},

                {"Ant_17", ("Rest", "Hunter's March")},

                {"Bone_East_10_Room", ("Pilgrim's Rest", "Far Fields")},
                {"Bone_East_15", ("Entrance West", "Far Fields")},
                {"Bone_East_27", ("Hideout", "Far Fields")},
                {"Bellway_03", ("Bellway", "Far Fields")},
                {"Bone_East_Umbrella", ("Seamstress", "Far Fields")},
                {"Bone_East_Weavehome", ("Weavenest Cindril", "Far Fields")},
                {"Sprintmaster_Cave", ("Sprintmaster Cave", "Far Fields")},

                {"Bellshrine_02", ("Bellshrine", "Greymoor")},
                {"Halfway_01", ("Halfway Home", "Greymoor")},
                {"Greymoor_02", ("Camp", "Greymoor")},
                {"Greymoor_08", ("Flea Caravan", "Greymoor")},

                {"Dust_10", ("Rest", "Sinner's Road")},
                {"Dust_11", ("Styx", "Sinner's Road")},

                {"Wisp_04", ("Rest", "Wisp Thicket")},

                {"Shadow_08", ("Hidden", "Bilewater")},
                {"Shadow_18", ("Bilehaven", "Bilewater")},
                {"Bellway_Shadow", ("Bellway", "Bilewater")},
                {"Organ_01", ("Exhaust Organ", "Bilewater")},
                {"Shadow_15", ("Trap", "Bilewater")},

                {"Aqueduct_05", ("Fleatopia", "Putrified Ducts")},
                {"Bellway_Aqueduct", ("Bellway", "Putrified Ducts")},
                {"Room_Huntress", ("Huntress", "Putrified Ducts")},

                {"Belltown", ("Town", "Bellhart")},

                {"Bellshrine_03", ("Bellshrine", "Shellwood")},
                {"Shellwood_01b", ("Room Center", "Shellwood")},
                {"Shellwood_08c", ("Entrance West", "Shellwood")},
                {"Mosstown_03", ("Entrance South", "Shellwood")},

                {"Bellway_08", ("Bellway", "Blasted Steps")},
                {"Coral_02", ("Entrance", "Blasted Steps")},
                {"Room_Pinstress", ("Pinstress", "Blasted Steps")},
                {"Coral_Judge_Arena", ("Flea Caravan", "Blasted Steps")},

                {"Coral_Tower_01", ("Coral Tower", "Sands of Karak")},
                {"Bellshrine_Coral", ("Room East", "Sands of Karak")},

                {"Slab_06", ("Bellway", "The Slab")},
                {"Slab_16", ("Room Northwest Alcove", "The Slab")},
                {"Slab_16_bench", ("Room Northwest", "The Slab")},
                {"Slab_20", ("Room East", "The Slab")},

                {"Peak_02", ("Entrance", "Mount Fay")},
                {"Peak_07", ("East Hidden", "Mount Fay")},
                {"Peak_12", ("Top Hidden", "Mount Fay")},
                {"Bellway_Peak", ("Shelter", "Mount Fay")},

                {"Song_10", ("Citadel Spa", "The Citadel")},
                {"Song_18", ("Room Center", "The Citadel")},
                {"Hang_01", ("Room West", "The Citadel")},
                {"Under_07b", ("Entrance Underworks", "The Citadel")},
                {"Bellway_City", ("Bellway", "The Citadel")},
                {"Song_Enclave", ("Songclave", "The Citadel")},
                {"Library_08", ("Vaultkeeper", "The Citadel")},
                {"Library_10", ("Vaults South", "The Citadel")},
                {"Cog_Bench", ("Cogwork Core", "The Citadel")},
                {"Ward_01", ("Whiteward", "The Citadel")},
                {"Hang_06b", ("High Halls", "The Citadel")},

                {"Under_01b", ("Entrance West", "Underworks")},
                {"Under_08", ("Confessional", "Underworks")},
                {"Under_17", ("The Cauldron", "Underworks")},

                {"Arborium_04", ("Rest", "Memorium")},

                {"Tube_Hub", ("Ventrica", "The Cradle")},

                {"Clover_20", ("Rest", "Verdania")},

                {"Abyss_09", ("Ascent", "The Abyss")},
                {"Abyss_12", ("Lake of Void", "The Abyss")},
                {"Room_Diving_Bell_Abyss", ("Diving Bell", "The Abyss")},
                {"Room_Diving_Bell_Abyss_Fixed", ("Diving Bell", "The Abyss")},
            };

        private static PlayMakerFSM _currentBenchFSM;

        private static readonly FieldInfo InventoryPaneListInstanceField = typeof(InventoryPaneList)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
    }
}