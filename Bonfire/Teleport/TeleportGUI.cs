using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Bench teleport GUI.
    /// </summary>
    public class TeleportGUI
    {
        public bool FastTravelDisplay { get; private set; } = false;

        public void CloseFastTravelMenu() => Bonfire.Instance.StartCoroutine(CloseFastTravelMenuRoutine());

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

            if (GUILayout.Button(text, GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                GUI.backgroundColor = Color.white;
                Bonfire.Log.LogDebug("[TeleportGUI] Fast travel menu open button was clicked");

                if (!hasAnyMap || !hasBenchPins) return;

                if (!UpdateCurrentBenchFSM()) return;

                SendOpenMapFSM();
                FastTravelDisplay = true;

                BlockInventoryInputs();
            }

            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
        }

        public void DrawBenchFastTravelUI()
        {
            if (!FastTravelDisplay) return;

            // add color filter on top of map
            GUI.color = new Color(0, 0, 0, 0.2f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // bench list + dark, filled background to hide map elements underneath
            float listW = 350f, listH = BonfireGUI.Height * 0.7f;
            Rect listRect = new Rect(20f, BonfireGUI.Height / 2f - listH / 2f, listW, listH);

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
                float confirmX = BonfireGUI.Width / 2f - confirmW / 2f;
                float confirmY = BonfireGUI.Height - confirmH - 20f;
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
            // for benches which don't have a pin location on map (same code, just different draw method called)
            else if (_selectedScene != null)
            {
                float confirmW = 400f, confirmH = 110f;
                float confirmX = BonfireGUI.Width / 2f - confirmW / 2f;
                float confirmY = BonfireGUI.Height - confirmH - 20f;
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
                DrawTeleportConfirmPanelExtraLocations();
                GUILayout.EndArea();
            }
        }

        public void FastTravelUnlockNotification()
        {
            if (Bonfire.Instance.Status.UnlockedFastTravel) return;

            if (PlayerData.instance.defeatedBellBeast)
            {
                GUI.color = new Color(0, 0, 0, 0.8f);
                GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
                GUI.color = Color.yellow;

                float w = 600f, h = 300f;
                Rect modalRect = new Rect(BonfireGUI.Width / 2f - w / 2f, BonfireGUI.Height / 2f - h / 2f, w, h);
                GUI.ModalWindow(10000, modalRect, DrawFastTravelUnlockModalPopup, "Bench fast travel unlocked!");
            }
        }


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
        private string _selectedScene = null;
        private string _selectedFullName;
        private Vector2 _scrollPos;

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
            Bonfire.Log.LogDebug("[TeleportGUI] Bench location data for active benches");

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
                    if (TeleportData.BenchStaticLocations.ContainsKey(s))
                    {
                        var value = TeleportData.BenchStaticLocations[s];
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
                        if (TeleportData.BenchStaticLocations.ContainsKey(customName))
                            customName = TeleportData.BenchStaticLocations[customName].customName;
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
                            // value or custom name, depending if the latter has been defined in BenchStaticLocations
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

            Bonfire.Log.LogDebug(
                "[TeleportGUI] Updating Bench FSM" +
                "\nStart scene name: " + PlayerData.instance.respawnScene +
                "\nStart marker name: " + PlayerData.instance.respawnMarkerName);

            if (markers == null) return false;

            var benchFSM = FSMUtility.LocateFSM(markers.gameObject, "Bench Control");
            if (benchFSM == null) return false;

            _currentBenchFSM = benchFSM;
            Bonfire.Log.LogDebug("Updated Bench FSM");

            return true;
        }

        // find all bench pins, update BenchPinScenes and BenchActiveLocationData
        private void UpdateBenchPins()
        {
            Bonfire.Log.LogDebug("[TeleportGUI] Updating bench pins");
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

                // don't include locked benches (these have darker map pin color)
                if (sr.sharedMaterial != null)
                {
                    Bonfire.Log.LogDebug($"Material: {sr.sharedMaterial.name}");
                    Bonfire.Log.LogDebug($"Shader:   {sr.sharedMaterial.shader?.name}");
                    if (sr.sharedMaterial.name.StartsWith("Inactive")) continue;
                }

                if (!BenchTeleport.PrepareTeleportLocation(pin))
                    continue;

                string sceneName = PlayerData.instance.tempRespawnScene;
                string markerName = PlayerData.instance.tempRespawnMarker;
                _benchPinScenes.Add(pin, (sceneName, markerName));
            }

            _benchActiveLocationData.Clear();
            UpdateActiveLocationData(); // must only call this after filling _benchPinScenes!

            Bonfire.Log.LogDebug($"Found {_benchPinScenes.Count} bench pins");
            Bonfire.Log.LogDebug("All bench pin respawn scenes:");
            foreach (var p in _benchPinScenes)
                Bonfire.Log.LogDebug(p.Value);
        }

        // send FSM to open in-game map menu
        private void SendOpenMapFSM()
        {
            _currentBenchFSM.SetState("Inv Map Open");
            _currentBenchFSM.SendEventSafe("FINISHED");
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

            // "UI CANCEL" steps once backwards to open the zoomed out map with only area names
            FSMUtility.SendEventToGameObject(panelInput.gameObject, "UI CANCEL");
            yield return new WaitForSecondsRealtime(0.25f);

            inventoryPaneList.CloseBlocked = false;

            // "BACK" closes the map menu itself
            // It's important to use "UI CANCEL" to step out first as it will not affect close state. And then only
            // use "BACK" as the final closing step and thus avoid any inventory state issues where game thinks it
            // might still be open.
            GameManager.instance.inventoryFSM.SendEventSafe("BACK");

            yield return new WaitUntil(() =>
                GameManager.instance == null ||
                GameManager.instance.inventoryFSM == null ||
                GameManager.instance.inventoryFSM.ActiveStateName == "Closed");

            RestoreInventoryInputs();
        }

        // clean up variables and close inventory. This method is used in two ways: 
        // 1. for public close menu interface (CloseFastTravelMenu)
        // 2. internally when both closing the map menu and teleporting must be done under same call, but closing must 
        // finish first so that teleporting sequence doesn't break menu functionality (StartTeleport)
        private IEnumerator CloseFastTravelMenuRoutine()
        {
            _selectedPin = null;
            _selectedScene = null;
            FastTravelDisplay = false;

            _benchPinScenes.Clear();
            _benchActiveLocationData.Clear();

            yield return CloseInventoryMap();
        }

        // first close inventory and only then perform fast travel
        private IEnumerator StartTeleport()
        {
            yield return Bonfire.Instance.StartCoroutine(CloseFastTravelMenuRoutine());
            BenchTeleport.TeleportToBench();
        }

        private void ChangeBenchMenuStyle()
        {
            int s = Bonfire.Instance.Status.BenchMenuStyle + 1;
            if (s >= 4 || s < 0)
                s = 0;

            Bonfire.Instance.Status.BenchMenuStyle = s;
        }

        private void DrawBenchListPanel()
        {
            Bonfire.Log.LogDebug("[TeleportGUI] Drawing bench list");
            int menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
            string styleText = TeleportData.BenchMenuStyle[menuStyleIndex];

            bool coloredStyle = menuStyleIndex == 2 || menuStyleIndex == 3;
            bool customNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
            _isCustomNameStyle = customNameStyle;

            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button($"Style: {styleText}", GUIStyles.ButtonStyle, GUILayout.Height(30f)))
            {
                ChangeBenchMenuStyle();

                menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
                _isCustomNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
                UpdateBenchPins();

                return;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.Label("Bench locations", GUIStyles.LabelStyle);
            GUILayout.Space(10);

            if (_benchPinScenes.Count == 0)
            {
                Bonfire.Log.LogDebug("Loading bench locations...");
                UpdateBenchPins();
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                foreach ((string mapArea, Color32 c) in TeleportData.OrderedBenchAreaNames)
                {
                    bool hasPinLocations = _benchActiveLocationData.ContainsKey(mapArea);
                    bool hasSpecialLocations = TeleportData.SpecialLocations.Any(l =>
                        l.AreaName == mapArea &&
                        l.IsUnlocked() &&
                        BenchTeleport.AbyssChecks(l.Scene));

                    if (!hasPinLocations && !hasSpecialLocations) continue;

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
                        textLabel = TeleportData.DefaultMenuColor;
                        text = textOld;
                    }

                    GUI.backgroundColor = bg;
                    GUI.color = textLabel;
                    GUILayout.Label(mapArea, GUIStyles.BenchListAreaLabelStyle);

                    GUILayout.Space(4);

                    if (hasPinLocations)
                    {
                        Bonfire.Log.LogDebug("Draw active bench locations");
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
                                GUIStyles.BenchListButtonSelectedStyle : GUIStyles.BenchListButtonStyle;

                            if (GUILayout.Button(customName, buttonStyle, GUILayout.Height(30f)))
                            {
                                _selectedPin = mapPin;
                                _selectedScene = null;
                                _selectedFullName = mapArea + ": " + customName;

                                var gameMap = GameManager.instance.gameMap;
                                Vector2 benchPos = BenchTeleport.GetBenchPinPositionInMapSpace(mapPin);
                                Vector2 scaledPos = benchPos * gameMap.transform.localScale.x;
                                gameMap.UpdateMapPosition(-scaledPos);
                            }

                            GUILayout.Space(2);

                            GUI.backgroundColor = bg;
                            GUI.color = text;
                        }
                    }

                    DrawSpecialLocationsForArea(mapArea, bg, text);

                    GUI.backgroundColor = bgOld;
                    GUI.color = textOld;
                }

                GUILayout.EndScrollView();
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(15);

            GUI.backgroundColor = Color.red;
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Close teleport menu", GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                CloseFastTravelMenu();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTeleportConfirmPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("LOCATION", GUIStyles.FastTravelConfirmHeaderStyle);
            GUILayout.Label(_selectedFullName, GUIStyles.FastTravelConfirmDestinationStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.5f);
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Travel", GUIStyles.ButtonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                BenchTeleport.PrepareTeleportLocation(_selectedPin);

                Bonfire.Instance.StartCoroutine(StartTeleport());
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // extra bench locations logic. For benches without map pins e.g. Bellhome in Bellhart.

        private void DrawSpecialLocationsForArea(string areaName, Color background, Color text)
        {
            Bonfire.Log.LogDebug("Draw static bench locations");
            foreach (var loc in TeleportData.SpecialLocations)
            {
                if (loc.AreaName != areaName || !loc.IsUnlocked()) continue;

                string customName = _isCustomNameStyle ? loc.CustomName : loc.DefaultName;
                bool isSelected = _selectedScene == loc.Scene;

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
                    GUIStyles.BenchListButtonSelectedStyle : GUIStyles.BenchListButtonStyle;

                // special locations are highlighted with a custom symbol
                if (GUILayout.Button(customName + " ★", buttonStyle, GUILayout.Height(30f)))
                {
                    _selectedPin = null;
                    _selectedFullName = areaName + ": " + customName;
                    _selectedScene = loc.Scene;
                }

                GUILayout.Space(2);

                GUI.backgroundColor = background;
                GUI.color = text;
            }
        }

        private void FillTeleportData(string sceneName)
        {
            var loc = TeleportData.SpecialLocations.FirstOrDefault(l => l.Scene == sceneName);
            if (loc == null) return;

            PlayerData.instance.tempRespawnScene = loc.Scene;
            PlayerData.instance.tempRespawnMarker = loc.Marker;
            PlayerData.instance.tempRespawnType = loc.RespawnType;
        }

        private void DrawTeleportConfirmPanelExtraLocations()
        {
            GUILayout.Space(8);
            GUILayout.Label("LOCATION", GUIStyles.FastTravelConfirmHeaderStyle);
            GUILayout.Label(_selectedFullName, GUIStyles.FastTravelConfirmDestinationStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.5f);
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Travel", GUIStyles.ButtonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                FillTeleportData(_selectedScene);

                Bonfire.Instance.StartCoroutine(StartTeleport());
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawFastTravelUnlockModalPopup(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "You have freed the Bell Beast and " +
                "unlocked the bench fast travel system!",
                GUIStyles.CrestUnlockLabelStyle
            );

            GUILayout.Space(5);
            GUILayout.Label(
                "To access travel menu: you need to own a map + Bench Pins item from Shakra.\n" +
                "To fast travel:\n" +
                "\n" +
                "- Sit down on any bench and click the new \"Fast Travel\" button.\n" +
                "- Select any bench from the list\n" +
                "- Then click the \"Travel\" button bottom-middle to teleport.\n",
                GUIStyles.FastTravelUnlockLabelStyle
            );

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Close",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(100f),
                    GUILayout.Height(40f)))
            {
                Bonfire.Instance.Status.UnlockedFastTravel = true;
                Bonfire.Instance.SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

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