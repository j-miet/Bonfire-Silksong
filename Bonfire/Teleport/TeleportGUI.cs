using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Leveling;

namespace BonfireSilksong.Teleport
{
    /// <summary>
    /// Bench teleport GUI.
    /// </summary>
    public class TeleportGUI
    {
        public const float StyleButtonHeight = 30f;
        public const float CloseButtonHeight = 40f;
        public const float ListSpacer = 15f;

        public static float ListW => 350f;
        public static float ListH => BonfireGUI.Height * 0.7f;
        public static float ListX => 20f;
        public static float ListY => BonfireGUI.Height / 2f - ListH / 2;

        public static bool FastTravelDisplay { get; private set; } = false;

        public void CloseFastTravelMenu() => Bonfire.Instance.StartCoroutine(CloseFastTravelMenuRoutine());

        public void DrawBenchFastTravelButton()
        {
            GUI.backgroundColor = new Color(0.8f, 0.5f, 0.2f);
            GUILayout.BeginVertical("box");

            bool hasAnyMap = PlayerData.instance.HasAnyMap;
            bool hasBenchPins = PlayerData.instance.hasPinBench;

            GUI.enabled = BonfireGUI.NoWindowsPending && GlobalStat.CheckFastTravelCondition;
            if (CustomCursor.Button("Fast Travel", GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                CustomCursor.ResetCursorPosition();

                GUI.backgroundColor = Color.white;
                Bonfire.Log.LogDebug("[TeleportGUI] Fast travel menu open button was clicked");

                if (!hasAnyMap || !hasBenchPins)
                {
                    GUILayout.EndVertical();
                    return;
                }

                if (!UpdateCurrentBenchFSM())
                {
                    GUILayout.EndVertical();
                    return;
                }

                Bonfire.Instance.StartCoroutine(SendOpenMapFSM());

                BlockInventoryInputs();
            }
            GUI.enabled = true;

            GUI.backgroundColor = Color.white;

            GUILayout.EndVertical();
        }

        public IEnumerator DoTeleport()
        {
            if (_selectedPin != null)
            {
                CustomCursor.ResetCursorPosition();

                DeliveryQuestItem.TakeHit(100); // destroy all courier quest items

                BenchTeleport.PrepareTeleportLocation(_selectedPin);

                Bonfire.Instance.StartCoroutine(StartTeleport());

                yield return new WaitForSecondsRealtime(1f);
            }
            else if (_selectedScene != null)
            {
                CustomCursor.ResetCursorPosition();

                DeliveryQuestItem.TakeHit(100);

                FillTeleportData(_selectedScene);

                Bonfire.Instance.StartCoroutine(StartTeleport());

                yield return new WaitForSecondsRealtime(1f);
            }

            yield break;
        }

        public void DrawBenchFastTravelUI()
        {
            if (!FastTravelDisplay) return;

            // color filter on top of map to make map screen slightly darker
            //GUI.color = new Color(0, 0, 0, 0.2f);
            //GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            //GUI.color = Color.white;

            // bench list + dark, filled background to hide map elements underneath
            Rect listRect = new Rect(ListX, ListY, ListW, ListH);

            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 1f);
            GUI.DrawTexture(listRect, Texture2D.whiteTexture);
            GUI.color = old;

            CustomCursor.BeginArea(listRect);
            DrawBenchListPanel();
            CustomCursor.EndArea();

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

                CustomCursor.BeginArea(confirmRect);
                DrawTeleportConfirmPanel();
                CustomCursor.EndArea();
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

                CustomCursor.BeginArea(confirmRect);
                DrawTeleportConfirmPanelExtraLocations();
                CustomCursor.EndArea();
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

            // RS = Right Stick for gamepads.
            actions.RsUp.Enabled = false;
            actions.RsDown.Enabled = false;
            actions.RsLeft.Enabled = false;
            actions.RsRight.Enabled = false;

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

            actions.RsUp.Enabled = true;
            actions.RsDown.Enabled = true;
            actions.RsLeft.Enabled = true;
            actions.RsRight.Enabled = true;

            //actions.OpenInventory.Enabled = true;
            //actions.OpenInventoryMap.Enabled = true;
            //actions.QuickMap.Enabled = true;
        }

        // bench location data parser, updated data is inserted into BenchActiveLocationData.
        private void UpdateActiveLocationData()
        {
            Bonfire.Log.LogDebug("[TeleportGUI] Update bench location data for active benches");

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

                    // Bonfire.Log.LogDebug($"Scene: {s}, Marker: {bench.Value.marker}");

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
                        Bonfire.Log.LogDebug($"# Scene '{s}' duplicate check =>");
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

                            Bonfire.Log.LogDebug("  - No duplicates found.");
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
                " Start scene name: " + PlayerData.instance.respawnScene +
                " Start marker name: " + PlayerData.instance.respawnMarkerName);

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
                    Bonfire.Log.LogDebug("[TeleportGUI] Bench pin sprite -> " +
                        $"Material: {sr.sharedMaterial.name}, " +
                        "Shader: {sr.sharedMaterial.shader?.name}");
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

            Bonfire.Log.LogDebug($"=> Found {_benchPinScenes.Count} bench pins in total. Scene names:");

            foreach (var l in _benchActiveLocationData.Values)
            {
                foreach (var e in l)
                {
                    var scene = e.scene;
                    var pin = e.mapPin;
                    var origScene = _benchPinScenes[pin].scene;
                    Bonfire.Log.LogDebug($" - Original: {origScene} - Custom: {scene}");
                }
            }
        }

        // send FSM to open in-game map menu
        private IEnumerator SendOpenMapFSM()
        {
            _currentBenchFSM.SetState("Inv Map Open");

            // add a pause (even yield return null seemed to be sufficient based on testing), otherwise GUI overlay
            // seems to interfere with rendering of compass icon.
            yield return new WaitForSecondsRealtime(0.01f);

            FastTravelDisplay = true;
        }

        // close map screen via FSM event calls
        private IEnumerator CloseInventoryMap()
        {
            InventoryPaneList inventoryPaneList = null;
            InventoryPaneInput paneInput = null;

            try
            {
                inventoryPaneList = (InventoryPaneList)InventoryPaneListInstanceField.GetValue(null);
                if (inventoryPaneList == null) yield break;

                paneInput = inventoryPaneList.GetComponentInChildren<InventoryPaneInput>();
                if (paneInput == null) yield break;
            }
            catch (Exception e)
            {
                Bonfire.Log.LogError($"CloseInventoryMap init error: {e}");
                yield break;
            }

            // "UI CANCEL" steps once backwards to open the zoomed out map with only area names
            FSMUtility.SendEventToGameObject(paneInput.gameObject, "UI CANCEL");
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
            CustomCursor.ResetCursorPosition();

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

            if (Bonfire.Instance.Status.ResetMusicAfterTeleport)
                StopAllSceneMusic();
        }

        // force all music and atmosphere to stop: when teleporting from certain scenes, the music from that scene
        // still remains on target scene.
        private static void StopAllSceneMusic()
        {
            Bonfire.Log.LogDebug("[TeleportGUI] Stop scene music");
            var gm = GameManager.instance;

            gm.AudioManager.StopAndClearMusic();
            gm.AudioManager.StopAndClearAtmos();

            Transform music = gm.AudioManager.transform?.Find("Music");
            if (music == null)
            {
                Bonfire.Log.LogDebug("Music object not found");
                return;
            }

            PlayMakerFSM fsmRestArea = music.Find("RestArea")?.GetComponent<PlayMakerFSM>();
            if (fsmRestArea != null)
            {
                fsmRestArea.Fsm.Start();
                fsmRestArea.SendEventSafe("REST AREA MUSIC STOP FAST");
                Bonfire.Log.LogDebug("Rest area audio source stopped");
            }

            PlayMakerFSM fsmFleaCaravan = music.Find("FleaCaravan")?.GetComponent<PlayMakerFSM>();
            if (fsmFleaCaravan != null)
            {
                fsmFleaCaravan.Fsm.Start();
                fsmFleaCaravan.SendEventSafe("FLEA MUSIC STOP FAST");
                Bonfire.Log.LogDebug("Flea caravan audio source stopped");
            }
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
            int menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
            string styleText = TeleportData.BenchMenuStyle[menuStyleIndex];

            bool coloredStyle = menuStyleIndex == 2 || menuStyleIndex == 3;
            bool customNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
            _isCustomNameStyle = customNameStyle;

            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal("box");
            if (CustomCursor.Button($"Style: {styleText}", GUIStyles.ButtonStyle, GUILayout.Height(StyleButtonHeight)))
            {
                ChangeBenchMenuStyle();

                menuStyleIndex = Bonfire.Instance.Status.BenchMenuStyle;
                _isCustomNameStyle = menuStyleIndex == 1 || menuStyleIndex == 3;
                UpdateBenchPins();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(ListSpacer);

            GUILayout.Label("Bench locations", GUIStyles.LabelStyle, GUILayout.Height(StyleButtonHeight));
            GUILayout.Space(ListSpacer);

            if (_benchPinScenes.Count == 0)
            {
                Bonfire.Log.LogDebug("Loading bench locations...");
                UpdateBenchPins();
            }
            else
            {
                _scrollPos = CustomCursor.BeginScrollViewTeleportMenu(_scrollPos);

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
                        foreach (var (customName, mapPin) in _benchActiveLocationData[mapArea])
                        {
                            bool isSelected = _selectedPin == mapPin;

                            if (isSelected)
                            {
                                GUI.color = Color.white;
                                GUI.backgroundColor = new Color(0.8f, 0.4f, 0f);
                                _selectedFullName = mapArea + ": " + customName;
                            }
                            else
                            {
                                GUI.color = text;
                            }

                            GUIStyle buttonStyle = isSelected ?
                                GUIStyles.BenchListButtonSelectedStyle : GUIStyles.BenchListButtonStyle;

                            if (CustomCursor.Button(customName, buttonStyle, GUILayout.Height(30f)))
                            {
                                _selectedPin = mapPin;
                                _selectedScene = null;

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

                CustomCursor.EndScrollView();
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(ListSpacer);

            GUI.backgroundColor = Color.red;
            GUILayout.BeginHorizontal("box");
            if (CustomCursor.Button("Close teleport menu", GUIStyles.ButtonStyle, GUILayout.Height(CloseButtonHeight)))
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
            if (CustomCursor.Button("Travel", GUIStyles.ButtonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                Bonfire.Instance.StartCoroutine(DoTeleport());
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // extra bench locations logic. For benches without map pins e.g. Bellhome in Bellhart.

        private void DrawSpecialLocationsForArea(string areaName, Color background, Color text)
        {
            foreach (var loc in TeleportData.SpecialLocations)
            {
                if (loc.AreaName != areaName || !loc.IsUnlocked()) continue;

                string customName = _isCustomNameStyle ? loc.CustomName : loc.DefaultName;
                bool isSelected = _selectedScene == loc.Scene;

                if (isSelected)
                {
                    GUI.color = Color.white;
                    GUI.backgroundColor = new Color(0.8f, 0.4f, 0f);
                    _selectedFullName = areaName + ": " + customName;
                }
                else
                {
                    GUI.color = text;
                }

                GUIStyle buttonStyle = isSelected ?
                    GUIStyles.BenchListButtonSelectedStyle : GUIStyles.BenchListButtonStyle;

                // special locations are highlighted with a custom symbol
                if (CustomCursor.Button(customName + " ★", buttonStyle, GUILayout.Height(30f)))
                {
                    _selectedPin = null;
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
            if (CustomCursor.Button("Travel", GUIStyles.ButtonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                Bonfire.Instance.StartCoroutine(DoTeleport());
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}