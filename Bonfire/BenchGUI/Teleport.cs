using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    // bench teleport
    public class Teleport
    {
        // MapPin -> (location scene, location bench marker) tuple
        // Used in: UpdateBenchPins stores data by using MapPin object, this ensures each key being unique. 
        // For each pin, the scene and marker name are also stored. This is because in UpdateActiveLocationData,
        // pin and scene are used for categorizing bench locations by area (see _benchActiveLocationData below), but
        // in rare cases, different pins could get same scene names from BenchTeleport.PrepareTeleportLocation thus
        // requiring an extra secondary identifier i.e. the marker name.
        private readonly Dictionary<MapPin, (string scene, string marker)> _benchPinScenes =
            new Dictionary<MapPin, (string, string)>();

        // Map area -> List of all benches in that area as (location scene, location map pin) tuples
        // Used in: UpdateActiveLocationData divides all available benches under main areas. Then DrawBenchListPanel
        // creates the GUI list by iterating over each area and it's bench locations
        private readonly Dictionary<string, List<(string scene, MapPin mapPin)>> _benchActiveLocationData
            = new Dictionary<string, List<(string, MapPin)>>();

        private readonly FieldInfo InventoryPaneListInstanceField = typeof(InventoryPaneList)
           .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private bool _isCustomNameStyle = false;
        private PlayMakerFSM _currentBenchFSM;
        private MapPin _selectedPin = null;
        private string _selectedFullName;
        private Vector2 _scrollPos;

        public bool FastTravelDisplay { get; private set; } = false;

        private static Color32 Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);
        private static readonly Color32 _defaultMenuColor = Rgb(204, 191, 26);

        // custom order for area names, in-game fast travel bench list then displays them in same order
        // _benchStaticLocations is the source of truth: if there isn't a valid map area under any of the
        // scenes, it will be ignored and falls to "Other" category instead.
        private static readonly List<(string areaName, Color32 color)> _orderedBenchAreaNames
            = new List<(string, Color32)>
        {
            ("Mosslands", Rgb(117, 160, 103)),
            ("Weavenest Atla", Rgb(109, 116, 151)),
            ("The Marrow", Rgb(120, 122, 120)),
            ("Wormways", Rgb(119, 106, 88)),
            ("Deep Docks", Rgb(192, 131, 66)),
            ("Hunter's March", Rgb(168, 94, 77)),
            ("Far Fields", Rgb(126, 137, 71)),
            ("Greymoor", Rgb(90, 123, 123)),
            ("Wisp Thicket", Rgb(68, 126, 81)),
            ("Bellhart", Rgb(238, 192, 128)),
            ("Sinner's Road", Rgb(187, 140, 118)),
            ("Bilewater", Rgb(157, 153, 73)),
            ("Putrified Ducts", Rgb(86, 120, 135)),
            ("Shellwood", Rgb(145, 151, 111)),
            ("Blasted Steps", Rgb(159, 132, 92)),
            ("Sands of Karak", Rgb(227, 134, 151)),
            ("The Slab", Rgb(122, 128, 138)),
            ("Mount Fay", Rgb(80, 108, 151)),
            ("The Citadel", Rgb(209, 194, 127)),
            ("Underworks", Rgb(99, 103, 109)),
            ("Memorium", Rgb(192, 178, 159)),
            ("The Cradle", Rgb(100, 106, 142)),
            ("Verdania", Rgb(96, 173, 120)),
            ("The Abyss", Rgb(94, 95, 94)),
            ("Other", _defaultMenuColor), // default option for uncategorized benches, never remove this
        };

        // Custom scene names for teleport locations
        // Key is the raw scene value from BenchTeleport.PrepareToTeleport
        //   => make sure to use final scene value for manually handled special cases!
        // Value is a tuple (custom name, map area). Custom name is optional: using empty string "" reverts back to
        // raw scene name. Map area is used in categorizing bench scenes.
        // If no key-value pair is found for specific scene, scene defaults to raw name and map area becomes "Other"
        //
        // Some benches don't have pins; these are filled manually
        private static readonly Dictionary<string, (string customName, string areaName)> _benchStaticLocations
            = new Dictionary<string, (string, string)>
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
                {"Bellshrine_Enclave", ("The First Shrine", "The Citadel")},
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

        private static readonly Dictionary<int, string> BenchMenuStyle = new Dictionary<int, string>
        {
            {0, "Basic + Default names"},
            {1, "Basic + Custom names"},
            {2, "Colored + Default names"},
            {3, "Colored + Custom names"}
        };

        // prevent most keyboard inputs in bench menu map screen, the rest require prefix patching via
        // InventoryPaneInput.Update.
        private void BlockInventoryInputs()
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

        // reverts keyboard input blocking.
        private void RestoreInventoryInputs()
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

        // bench location data parser, updated data is inserted into BenchActiveLocationData.
        private void UpdateActiveLocationData()
        {
            Bonfire.Log.LogDebug("== Bench location data ==");

            string customName;
            string areaName;

            foreach (KeyValuePair<MapPin, (string scene, string marker)> bench in _benchPinScenes)
            {
                try
                {
                    string s = bench.Value.scene;
                    string m = bench.Value.marker;
                    if (s == null || m == null) continue;

                    var p = bench.Key;

                    Bonfire.Log.LogDebug("Scene: " + s);
                    Bonfire.Log.LogDebug("Marker: " + bench.Value.marker);

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
                        && _benchActiveLocationData.ContainsKey(areaName))
                    {
                        Bonfire.Log.LogDebug($"Scene '{s}' duplicate check =>");
                        bool alreadyFound = false;

                        foreach (var (scene, _) in _benchActiveLocationData[areaName])
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

                    if (!_isCustomNameStyle)
                        customName = s;

                    if (!_benchActiveLocationData.ContainsKey(areaName))
                        _benchActiveLocationData.Add(areaName, new List<(string, MapPin)>());

                    _benchActiveLocationData[areaName].Add((customName, p));
                }
                catch (Exception e)
                {
                    Bonfire.Log.LogError($"[UpdateActiveLocationData]: {e}");
                }
            }
        }

        // replace tracked FSM with one from current bench location.
        private bool UpdateCurrentBenchFSM()
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

        // update BenchPinScenes and BenchActiveLocationData
        private void UpdateBenchPins()
        {
            _benchPinScenes.Clear();
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
                _benchPinScenes.Add(pin, (sceneName, markerName));
            }

            _benchActiveLocationData.Clear();
            UpdateActiveLocationData(); // must only call this after filling _benchPinScenes!

            Bonfire.Log.LogDebug($"Found {_benchPinScenes.Count} bench pins");
            Bonfire.Log.LogDebug("GUI: bench pin respawn scenes:");
            foreach (var p in _benchPinScenes)
                Bonfire.Log.LogDebug(p.Value);
        }

        // call UpdateBenchPins with a small delay. This allows
        private IEnumerator DelayedPinUpdate()
        {
            yield return new WaitForSeconds(1f);
            UpdateBenchPins();
        }

        // send FSM to open in-game map menu
        private void SendOpenMapFSM()
        {
            _currentBenchFSM.SetState("Open Map");
            _currentBenchFSM.SendEvent("INV MAP");
        }

        // close map screen via FSM event calls
        private IEnumerator CloseInventoryMap()
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
            yield return new WaitForSecondsRealtime(0.5f);
            FSMUtility.SendEventToGameObject(panelInput.gameObject, "UI CANCEL");
            yield return new WaitForSecondsRealtime(0.5f);

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

        public void CloseFastTravelMenu()
        {
            _selectedPin = null;
            FastTravelDisplay = false;

            _benchPinScenes.Clear();
            _benchActiveLocationData.Clear();

            Bonfire.Instance.StartCoroutine(CloseInventoryMap());
        }

        private void ChangeBenchMenuStyle()
        {
            int s = Bonfire.Instance.Status.BenchMenuStyle + 1;
            if (s >= 4 || s < 0)
                s = 0;

            Bonfire.Instance.Status.BenchMenuStyle = s;
        }

        public void DrawBenchFastTravelButton()
        {
            if (!Bonfire.Instance.Status.UnlockedFastTravel) return;

            GUI.backgroundColor = new Color(0.8f, 0.5f, 0.2f);
            GUILayout.BeginVertical("box");

            bool hasAnyMap = PlayerData.instance.HasAnyMap;
            bool hasBenchPins = PlayerData.instance.hasPinBench;
            string text = "Fast travel";

            if (hasAnyMap && hasBenchPins) { }
            else if (hasAnyMap) text += "\n(Locked, need Bench Pins from Shakra)";
            else if (hasBenchPins) text += "\n(Locked, need a map from Shakra)";
            else text += "\n(Locked, need a map + Bench Pins from Shakra)";

            if (GUILayout.Button(text, Styles._buttonStyle, GUILayout.Height(40f)))
            {
                GUI.backgroundColor = Color.white;
                Bonfire.Log.LogDebug("Fast travel menu open button was clicked");

                if (!hasAnyMap || !hasBenchPins) return;

                if (!UpdateCurrentBenchFSM()) return;

                SendOpenMapFSM();
                FastTravelDisplay = true;

                BlockInventoryInputs();

                BonfireGUI.Instance.StartCoroutine(DelayedPinUpdate());
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
        }

        public void DrawBenchFastTravelUI()
        {
            if (!FastTravelDisplay) return;

            // add color filter on top of map
            GUI.color = new Color(0, 0, 0, 0.2f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // bench list + dark, filled background to hide map elements underneath
            float listW = 350f, listH = Screen.height * 0.7f;
            Rect listRect = new Rect(20f, Screen.height / 2f - listH / 2f, listW, listH);

            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 1f);
            GUI.DrawTexture(listRect, Texture2D.whiteTexture);
            GUI.color = old;

            GUILayout.BeginArea(listRect);
            DrawBenchListPanel();
            GUILayout.EndArea();

            // if a bench is clicked: confirmation panel with dark background and border
            if (_selectedPin != null)
            {
                float confirmW = 400f, confirmH = 110f;
                float confirmX = Screen.width / 2f - confirmW / 2f;
                float confirmY = Screen.height - confirmH - 20f;
                Rect confirmRect = new Rect(confirmX, confirmY, confirmW, confirmH);

                old = GUI.color;
                GUI.color = new Color(0.8f, 0.7f, 0.3f, 0.9f);
                GUI.DrawTexture(
                    new Rect(
                        confirmX - 2, confirmY - 2,
                        confirmW + 4, confirmH + 4),
                        Texture2D.whiteTexture);

                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                GUI.DrawTexture(confirmRect, Texture2D.whiteTexture);
                GUI.color = old;

                GUILayout.BeginArea(confirmRect);
                DrawTeleportConfirmPanel();
                GUILayout.EndArea();
            }
        }

        private void DrawBenchListPanel()
        {
            int menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
            string styleText = BenchMenuStyle[menuStyleIndex];

            bool coloredStyle = menuStyleIndex == 2 || menuStyleIndex == 3;
            bool customNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
            _isCustomNameStyle = customNameStyle;

            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button($"Style: {styleText}", Styles._buttonStyle, GUILayout.Height(30f)))
            {
                ChangeBenchMenuStyle();

                menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
                _isCustomNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
                UpdateBenchPins();

                return;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.Label("Bench locations", Styles._labelStyle);
            GUILayout.Space(10);

            if (_benchPinScenes.Count == 0)
            {
                Bonfire.Log.LogDebug("Loading bench locations...");
                UpdateBenchPins();
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                foreach ((string mapArea, Color32 c) in _orderedBenchAreaNames)
                {
                    if (!_benchActiveLocationData.Keys.Contains(mapArea)) continue;

                    Color bgOld = GUI.backgroundColor;
                    Color textOld = GUI.color;

                    Color bg;
                    Color textLabel;
                    Color text;

                    if (coloredStyle)
                    {
                        bg = (Color)c * 1.5f;
                        textLabel = c;
                        text = Color.Lerp(c, Color.white, 0.9f);
                    }
                    else
                    {
                        bg = bgOld;
                        textLabel = _defaultMenuColor;
                        text = textOld;
                    }

                    GUI.backgroundColor = bg;
                    GUI.color = textLabel;
                    GUILayout.Label(mapArea, Styles._benchListAreaLabelStyle);

                    GUILayout.Space(4);

                    foreach (var (customName, mapPin) in _benchActiveLocationData[mapArea])
                    {
                        bool isSelected = _selectedPin == mapPin;

                        if (isSelected)
                        {
                            GUI.color = Color.white;
                            GUI.backgroundColor = new Color(0.8f, 0.4f, 0f);
                        }
                        else
                        {
                            GUI.color = text;
                        }

                        GUIStyle buttonStyle = isSelected ?
                            Styles._benchListButtonSelectedStyle : Styles._benchListButtonStyle;

                        if (GUILayout.Button(customName, buttonStyle, GUILayout.Height(30f)))
                        {
                            _selectedPin = mapPin;
                            _selectedFullName = mapArea + ": " + customName;

                            // pan map camera on top of this bench if possible; otherwise close as possible
                            var gameMap = GameManager.instance.gameMap;
                            Vector2 benchPos = BenchTeleport.GetBenchPinPositionInMapSpace(mapPin);
                            Vector2 scaledPos = benchPos * gameMap.transform.localScale.x;
                            gameMap.UpdateMapPosition(-scaledPos);
                        }

                        GUILayout.Space(2);

                        GUI.backgroundColor = bg;
                        GUI.color = text;
                    }

                    GUI.backgroundColor = bgOld;
                    GUI.color = textOld;
                }

                GUILayout.EndScrollView();
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(15);

            GUI.backgroundColor = Color.red;
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Close teleport menu", Styles._buttonStyle, GUILayout.Height(40f)))
            {
                CloseFastTravelMenu();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTeleportConfirmPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("LOCATION", Styles._fastTravelConfirmHeaderStyle);
            GUILayout.Label(_selectedFullName, Styles._fastTravelConfirmDestinationStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.5f);
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Travel", Styles._buttonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                BenchTeleport.PrepareTeleportLocation(_selectedPin);

                CloseFastTravelMenu();

                BenchTeleport.TeleportToBench();
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // bench fast travel unlock check
        public void FastTravelUnlockNotification()
        {
            if (Bonfire.Instance.Status.UnlockedFastTravel) return;

            if (PlayerData.instance.defeatedBellBeast)
            {
                GUI.color = new Color(0, 0, 0, 0.8f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.yellow;

                float w = 600f, h = 300f;
                Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
                GUI.ModalWindow(10000, modalRect, DrawFastTravelUnlockModalPopup, "Bench fast travel unlocked!");
            }
        }

        private void DrawFastTravelUnlockModalPopup(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "You have freed the Bell Beast and " +
                "unlocked the bench fast travel system!",
                Styles._crestUnlockLabelStyle
            );

            GUILayout.Space(5);
            GUILayout.Label(
                "To access travel menu: you need to own a map + Bench Pins item from Shakra.\n" +
                "To fast travel:\n" +
                "\n" +
                "- Sit down on any bench and click the new \"Fast Travel\" button.\n" +
                "- Select any bench from the list\n" +
                "- Then click the \"Travel\" button bottom-middle to teleport.\n",
                Styles._fastTravelUnlockLabelStyle
            );

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Close",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(100f),
                    GUILayout.Height(40f)))
            {
                Bonfire.Instance.Status.UnlockedFastTravel = true;
                Bonfire.Instance.SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // -- patched methods -- 

        // these should prevent inputs which BlockInventoryInputs can't block in bench fast travel menu:
        // ESC/inventory pane navigation
        [HarmonyPatch(typeof(InventoryPaneInput), "Update")]
        class InventoryPaneInput_Update
        {
            static bool Prefix()
            {
                if (BonfireGUI.Teleport != null && BonfireGUI.Teleport.FastTravelDisplay)
                    return false;
                return true;
            }
        }

        // map panning
        [HarmonyPatch(typeof(GameMap), "Update")]
        class GameMap_Update
        {
            static bool Prefix()
            {
                if (BonfireGUI.Teleport != null && BonfireGUI.Teleport.FastTravelDisplay)
                    return false;
                return true;
            }
        }
    }
}