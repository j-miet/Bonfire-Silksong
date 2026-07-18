using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Bench user interface, fast travel menu and enemy hp bar rendering
    /// </summary>
    public class BonfireGUI : MonoBehaviour
    {
        public readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();

        public static BonfireGUI Instance;

        public void OnGUI()
        {
            if (GameManager.instance == null && PlayerData.instance == null) return;

            EnsureFonts();

            if (_fastTravelDisplay)
            {
                Bonfire.Instance.ShowCursor();
                DrawBenchFastTravelUI();
            }

            // then check if game state is playing, paused or in inventory
            // if none of them is true, avoid running to prevent lag in main menu
            if (!(GameManager.instance.GameState == GameState.PLAYING ||
                  GameManager.instance.GameState == GameState.PAUSED) ||
                InInventory())
                return;

            if (PlayerData.instance.atBench && !GameManager.instance.isPaused)
            {
                GUI.enabled = true;
                EnsureStyles();

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.color = Color.white;

                DrawBenchUI();
            }

            DrawEnemyHealthBars();  // hp bar display must not be tied to bench check

            if (!PlayerData.instance.atBench)
                ResetPendingLevelUp();
        }


        private bool _pendingCrestUnlock = false;

        private MapPin _selectedPin = null;
        private string _selectedFullName;
        private bool _fastTravelDisplay = false;
        private PlayMakerFSM _currentBenchFSM;
        private Vector2 _scrollPos;

        // MapPin -> <location scene, location bench marker>
        // Used in: UpdateBenchPins stores data by using MapPin object, this ensures each key being unique. 
        // For each pin, the scene and marker name are also stored. This is because in UpdateActiveLocationData,
        // pin and scene are used for categorizing bench locations by area (see _benchActiveLocationData below), but
        // in rare cases, different pins could get same scene names from BenchTeleport.PrepareTeleportLocation thus
        // requiring an extra secondary identifier i.e. the marker name.
        private readonly Dictionary<MapPin, Tuple<string, string>> _benchPinScenes =
            new Dictionary<MapPin, Tuple<string, string>>();

        // Map area -> List of all benches in that area as <location scene, location map pin>
        // Used in: UpdateActiveLocationData divides all available benches under main areas. Then DrawBenchListPanel
        // creates the GUI list by iterating over each area and it's bench locations
        private readonly Dictionary<string, List<Tuple<string, MapPin>>> _benchActiveLocationData =
            new Dictionary<string, List<Tuple<string, MapPin>>>();

        // custom scene names for teleport locations (left = game's internal default, right = custom name)
        // scene names are ones after BenchTeleport.PrepareToTeleport has been applied!
        // if no key-value pair is found for specific scene, keeps using the default.
        //
        // TODO: add better names for key locations; leave rest either as defaults or something like "Bench 1"
        private readonly Dictionary<string, string> _benchStaticLocations =
            new Dictionary<string, string>
            {
                {"Tut_03", "Church"},
            };

        // custom order for area names, in-game map list GUI follows same order.
        // adding/removing entries is also possible, just make sure to update UpdateActiveLocationData as well.
        private readonly List<string> _orderedBenchAreaNames = new List<string>
        {
            "Bone Bottom",
            "Weavenest Atla",
            "The Marrow",
            "Wormways",
            "Deep Docks",
            "Hunter's March",
            "Far Fields",
            "Greymoor",
            "Wisp Thicket",
            "Sinner's Road",
            "Bilewater",
            "Putrified Ducts",
            "Bellhart",
            "Shellwood",
            "Blasted Steps",
            "Sands of Karak",
            "The Slab",
            "Mount Fay",
            "The Citadel",
            "Underworks",
            "Arborium",
            "The Cradle",
            "Verdania",
            "The Abyss",
            "Other"
        };

        private Font _trajanBold;
        private Font _trajanNormal;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _crestUnlockLabelStyle;
        private GUIStyle _crestUnlockButtonStyle;
        private GUIStyle _fastTravelUnlockLabelStyle;
        private GUIStyle _benchListAreaLabelStyle;
        private GUIStyle _benchListButtonStyle;
        private GUIStyle _benchListButtonSelectedStyle;
        private GUIStyle _fastTravelConfirmHeaderStyle;
        private GUIStyle _fastTravelConfirmDestinationStyle;

        private static readonly FieldInfo InventoryPaneListInstanceField = typeof(InventoryPaneList)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

        private static Texture2D _tex;

        private void Awake()
        {
            Instance = this;
        }

        // -- bench fast travel -- //

        private IEnumerator DelayedPinUpdate()
        {
            yield return new WaitForSeconds(1f);
            UpdateBenchPins();
        }

        // prevent most keyboard inputs in bench menu map screen, rest are handled with patched Update methods.
        private void BlockInventoryInputs()
        {
            var actions = GameManager.instance.inputHandler.inputActions;
            actions.PaneLeft.Enabled = false;
            actions.PaneRight.Enabled = false;
            actions.OpenInventory.Enabled = false;
            actions.OpenInventoryMap.Enabled = false;
            actions.OpenInventoryJournal.Enabled = false;
            actions.OpenInventoryTools.Enabled = false;
            actions.OpenInventoryQuests.Enabled = false;
            actions.QuickMap.Enabled = false;
        }

        private void RestoreInventoryInputs()
        {
            var actions = GameManager.instance.inputHandler.inputActions;
            actions.PaneLeft.Enabled = true;
            actions.PaneRight.Enabled = true;
            actions.OpenInventory.Enabled = true;
            actions.OpenInventoryMap.Enabled = true;
            actions.OpenInventoryJournal.Enabled = true;
            actions.OpenInventoryTools.Enabled = true;
            actions.OpenInventoryQuests.Enabled = true;
            actions.QuickMap.Enabled = true;
        }

        private void UpdateActiveLocationData()
        {
            foreach (KeyValuePair<MapPin, Tuple<string, string>> bench in _benchPinScenes)
            {
                try
                {
                    string b = bench.Value.Item1; // bench scene; Item2 is the be bench marker
                    var p = bench.Key; // map pin

                    // bench custom name; only used for naming teleport locations, either by itself
                    string customName;
                    string areaName;

                    // use custom name if available; otherwise default to internal scene name e.g. "Tut_03"
                    if (_benchStaticLocations.TryGetValue(b, out string location))
                        customName = location;
                    else
                        customName = b;

                    // get map area name
                    // <IMPORTANT> scene names are from BenchTeleport.PrepareToTeleport:
                    // ---> if respawnScene is manually updated, use that value instead! <---
                    //
                    // TODO: could simplify this a bit using StartsWith statements
                    // e.g. b.StartsWith("Bone_"), b.StartsWith("Bone_East_") etc.
                    if (b == "Tut_03" || b == "Tut_04" || b == "Mosstown_02c")
                        areaName = "Bone Bottom";
                    else if (b == "Weave_07")
                        areaName = "Weavenest Atla";
                    else if (b == "Bone_01c" || b == "Bone_04" || b == "Bone_10" || b == "Bone_12" || b == "Bellshrine")
                        areaName = "The Marrow";
                    else if (b == "Crawl_08")
                        areaName = "Wormways";
                    else if (b == "Dock_01" || b == "Dock_06_Church" || b == "Dock_10" || b == "Bellshrine_05"
                            || b == "Room_Forge" || b == "Room_Diving_Bell")
                        areaName = "Deep Docks";
                    else if (b == "Ant_17")
                        areaName = "Hunter's March";
                    else if (b == "Bone_East_10_Room" || b == "Bone_East_15" || b == "Bone_East_27" || b == "Bellway_03"
                            || b == "Bone_East_Umbrella" || b == "Bone_East_Weavehome" || b == "Sprintmaster_Cave")
                        areaName = "Far Fields";
                    else if (b == "Bellshrine_02" || b == "Halfway_01")
                        areaName = "Greymoor";
                    else if (b == "Dust_10" || b == "Dust_11")
                        areaName = "Sinner's Road";
                    else if (b == "Wisp_04")
                        areaName = "Wisp Thicket";
                    else if (b == "Shadow_08" || b == "Shadow_18" || b == "Bellway_Shadow" || b == "Organ_01")
                        areaName = "Bilewater";
                    else if (b == "Aqueduct_05" || b == "Bellway_Aqueduct" || b == "Room_Huntress")
                        areaName = "Putrified Ducts";
                    else if (b == "Belltown")
                        areaName = "Bellhart";
                    else if (b == "Bellshrine_03" || b == "Shellwood_01b" || b == "Shellwood_08c" || b == "Mosstown_03")
                        areaName = "Shellwood";
                    else if (b == "Bellway_08" || b == "Coral_02" || b == "Room_Pinstress")
                        areaName = "Blasted Steps";
                    else if (b == "Coral_Tower_01" || b == "Bellshrine_Coral")
                        areaName = "Sands of Karak";
                    else if (b == "Slab_06" || b == "Slab_16" || b == "Slab_20")
                    {
                        // rare case requiring a secondary identifier via marker name
                        if (b == "Slab_16" && bench.Value.Item2 == "RestBench (1)")
                        {
                            customName = "Slab_16_bench";

                            if (_benchStaticLocations.TryGetValue(customName, out string locationUpdated))
                                customName = locationUpdated;
                        }

                        areaName = "The Slab";
                    }
                    else if (b == "Peak_07" || b == "Peak_12" || b == "Bellway_Peak")
                        areaName = "Mount Fay";
                    else if (b == "Song_10" || b == "Song_18" || b == "Hang_01" || b == "Hang_06b" || b == "Under_07b"
                            || b == "Bellway_City" || b == "Bellshrine_Enclave" || b == "Song_Enclave"
                            || b == "Library_08" || b == "Library_10" || b == "Cog_Bench" || b == "Ward_01")
                        areaName = "The Citadel";
                    else if (b == "Under_01b" || b == "Under_08" || b == "Under_17")
                        areaName = "Underworks";
                    else if (b == "Arborium_04")
                        areaName = "Arborium";
                    else if (b == "Tube_Hub")
                        areaName = "The Cradle";
                    else if (b == "Clover_20")
                        areaName = "Verdania";
                    else if (b == "Abyss_09" || b == "Abyss_12" || b == "Room_Diving_Bell_Abyss"
                            || b == "Room_Diving_Bell_Abyss_Fixed")
                        areaName = "The Abyss";
                    else
                        areaName = "Other";

                    if (!_benchActiveLocationData.ContainsKey(areaName))
                        _benchActiveLocationData.Add(areaName, new List<Tuple<string, MapPin>>());

                    _benchActiveLocationData[areaName].Add(new Tuple<string, MapPin>(customName, p));
                }
                catch (Exception e)
                {
                    Bonfire.Log.LogInfo($"[UpdateActiveLocationData]: {e}");
                }
            }
        }

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
                {
                    Bonfire.Log.LogError("Error while preparing teleport locations");
                    _benchPinScenes.Clear();
                    return;
                }

                string sceneName = PlayerData.instance.tempRespawnScene;
                string markerName = PlayerData.instance.tempRespawnMarker;
                _benchPinScenes.Add(pin, new Tuple<string, string>(sceneName, markerName));
            }

            _benchActiveLocationData.Clear();
            UpdateActiveLocationData(); // must only call this after filling _benchPinScenes!

            Bonfire.Log.LogDebug($"Found {_benchPinScenes.Count} bench pins");
            Bonfire.Log.LogDebug("GUI: bench pin respawn scenes:");
            foreach (var p in _benchPinScenes)
                Bonfire.Log.LogDebug(p.Value);
        }

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

        private void DrawBenchFastTravelButton()
        {
            GUI.backgroundColor = Color.blue;
            GUILayout.BeginVertical("box");

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Fast travel menu", _buttonStyle, GUILayout.Height(35f)))
            {
                Bonfire.Log.LogDebug("Fast travel menu open button was clicked");
                if (!UpdateCurrentBenchFSM()) return;

                _currentBenchFSM.SetState("Open Map");
                _currentBenchFSM.SendEvent("INV MAP");
                _fastTravelDisplay = true;

                BlockInventoryInputs();

                StartCoroutine(DelayedPinUpdate());
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawBenchFastTravelUI()
        {
            if (!_fastTravelDisplay) return;

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
                DrawConfirmPanel();
                GUILayout.EndArea();
            }
        }

        private void DrawBenchListPanel()
        {
            GUILayout.Label("Bench locations", _labelStyle);
            GUILayout.Space(10);

            if (_benchPinScenes.Count == 0)
            {
                GUILayout.Label("Loading bench locations...");
                GUILayout.Space(10);
                UpdateBenchPins();
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                foreach (string mapArea in _orderedBenchAreaNames)
                {
                    if (!_benchActiveLocationData.Keys.Contains(mapArea)) continue;

                    GUILayout.Label(mapArea, _benchListAreaLabelStyle);
                    GUILayout.Space(4);

                    foreach (var (customName, mapPin) in _benchActiveLocationData[mapArea])
                    {
                        bool isSelected = _selectedPin == mapPin;

                        GUI.backgroundColor = isSelected ? new Color(0.8f, 0.4f, 0f) : Color.white;
                        GUIStyle buttonStyle = isSelected ? _benchListButtonSelectedStyle : _benchListButtonStyle;

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
                    }
                }

                GUILayout.EndScrollView();
                GUI.backgroundColor = Color.white;
            }

            GUILayout.Space(10);

            GUI.backgroundColor = Color.red;
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button("Close teleport menu", _buttonStyle, GUILayout.Height(40f)))
            {
                _selectedPin = null;
                _fastTravelDisplay = false;
                _benchPinScenes.Clear();

                StartCoroutine(CloseInventoryMap());
            }
            GUILayout.EndHorizontal();
        }

        private void DrawConfirmPanel()
        {
            GUILayout.Space(8);
            GUILayout.Label("LOCATION", _fastTravelConfirmHeaderStyle);
            GUILayout.Label(_selectedFullName, _fastTravelConfirmDestinationStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.5f);
            GUI.contentColor = Color.white;
            if (GUILayout.Button("Travel", _buttonStyle, GUILayout.Width(200f), GUILayout.Height(30f)))
            {
                BenchTeleport.PrepareTeleportLocation(_selectedPin);

                _selectedPin = null;
                _fastTravelDisplay = false;
                _benchPinScenes.Clear();

                StartCoroutine(CloseInventoryMap());
                BenchTeleport.TeleportToBench();
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // -- main UI --

        private void DrawBenchUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            int availableRelicLevels = ls.CachedRelicsObtained - s.ConsumedRelicLevels
                                     + s.RespecRelicLevels - s.PendingRelicLevels;
            bool gotFreeLevel = availableRelicLevels > 0;

            int rosaryLevels = s.TotalRosaryLevels + s.PendingRosaryLevels;
            int rosariesToLvlUp = (int)(Math.Pow(rosaryLevels, 2.0) + (10 * rosaryLevels) + 50.0);

            int currentLvl = s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                        + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                        + s.PendingRelicLevels
                        + s.PendingRosaryLevels
                        + 1; // base level is 1

            // preview strings
            string totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            string totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            string totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string totalArt = (s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            string totalLck = (s.LuckStat + ls.LuckIncrease).ToString();

            string needleDamage = ls.NeedleDamage(s.StrengthStat + ls.StrengthIncrease).ToString();

            string attackSpeed = ls.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            string critDamage = ls.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100).ToString();

            string spellDamage = ls.SpellDamage(100, s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string bindCost = ls.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();

            string toolDamage = ls.ToolDamage(100, s.ArtificeStat + ls.ArtificeIncrease).ToString();
            string toolRegenInterval = "-";
            if (s.ArtificeStat + ls.ArtificeIncrease > 0)
            {
                int toolRegen = ls.ToolRegenInterval(s.ArtificeStat + ls.ArtificeIncrease);
                toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            string extraMasks = ls.ExtraMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string blockChance = s.ResilienceStat + ls.ResilienceIncrease > 0
                ? ls.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0")
                : "0";

            string extraSilk = ls.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            string silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 0)
            {
                int silkRegen = ls.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }

            string critChance = ls.CritChance(s.LuckStat + ls.LuckIncrease).ToString();
            string rosaryIncrease = (5 * (s.LuckStat + ls.LuckIncrease)).ToString();

            // apply button label
            string applyText;
            if (s.PendingRelicLevels > 0 && s.PendingRosaries > 0)
            {
                applyText = $"Apply ({s.PendingRosaries} rosaries and {s.PendingRelicLevels} relics)";
            }
            else if (s.PendingRelicLevels > 0)
            {
                applyText = $"Apply ({s.PendingRelicLevels} relic levels)";
            }
            else if (gotFreeLevel)
            {
                applyText = $"{availableRelicLevels} Free Levels available!";
            }
            else
            {
                applyText = $"Apply ({s.PendingRosaries} rosaries)";
            }

            // layout
            GUILayout.BeginArea(new Rect(20f, Screen.height / 4.25f, 530f, 750f));

            DrawBenchFastTravelButton();

            GUILayout.Label("Level Up", _labelStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButton("Strength", totalStr, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Dexterity", totalDex, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Intelligence", totalInt, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Artifice", totalArt, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Resilience", totalRes, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Wisdom", totalWsdm, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Luck", totalLck, gotFreeLevel, s, rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("Needle Damage: " + needleDamage);
            DrawStatLabel("Slash Speed: " + attackSpeed);
            DrawStatLabel("Silk Skill Damage: " + spellDamage + "%");
            DrawStatLabel("Tool Damage: " + toolDamage + "%");
            DrawStatLabel("Extra Lifeblood Masks: " + extraMasks);
            DrawStatLabel("Silk Gained On Hit: " + extraSilk);
            DrawStatLabel("Crit Chance: " + critChance + "%");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("");   // spacer to align with column 1
            DrawStatLabel("Crit Damage: " + critDamage + "%");
            DrawStatLabel("Binding Cost: " + bindCost);
            DrawStatLabel("Tool Regen interval: " + toolRegenInterval);
            DrawStatLabel("Block Chance: " + blockChance + "%");
            DrawStatLabel("Silk Regen interval: " + silkRegenInterval);
            DrawStatLabel("Rosary + Shard Increase: " + rosaryIncrease + "%");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Label("Current Level: " + currentLvl, _labelStyle);
            GUILayout.Label("Rosaries to Level Up: " + rosariesToLvlUp, _labelStyle);

            // apply and cancel
            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(applyText, _buttonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("Cancel", _buttonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            // respec
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    _buttonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(522f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawShellShardSettingsUI();
            DrawEnemyHPToggleUI();
            DrawCrestUnlockUI();

            FastTravelUnlockNotification();

            GUILayout.EndArea();
        }

        // ui panel for shell shard replenishment
        private void DrawShellShardSettingsUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Shell shards replenishment");

            GUI.backgroundColor = Color.white;
            s.InfiniteShellShardsEnabled = GUILayout.Toggle(s.InfiniteShellShardsEnabled, "Infinite shards");

            if (!s.InfiniteShellShardsEnabled)
                s.InfiniteShardsBenchOnly = false;

            GUI.enabled = s.InfiniteShellShardsEnabled;
            s.InfiniteShardsBenchOnly = GUILayout.Toggle(
                s.InfiniteShardsBenchOnly,
                "Shard refill to full only at benches");
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        // toggle button to enable/disable enemy HP bars
        private void DrawEnemyHPToggleUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Enemy HP bars");

            GUI.backgroundColor = Color.white;
            s.EnemyHealthBarsEnabled = GUILayout.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars (global hotkey is LeftControl + H)");

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = GUILayout.Toggle(
                s.EnemyHpBarColorProgression,
                "Progressive HP bar values (green → yellow → red)");
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        // draws floating health bars above tracked enemies
        private void DrawEnemyHealthBars()
        {
            var s = Bonfire.Instance.Status;

            if (!s.EnemyHealthBarsEnabled) return;

            foreach (var kvp in EnemyMaxHp)
            {
                HealthManager hm = kvp.Key;
                if (hm == null) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(hm.transform.position);
                if (screenPos.z < 0) continue;

                float maxHp = kvp.Value;
                float currentHp = hm.hp;
                bool isBoss = EnemyIsBoss.TryGetValue(hm, out bool b) && b;

                if (currentHp == maxHp && !isBoss) continue; // full HP = hide bars for normal enemies

                float pct = Mathf.Clamp01(currentHp / maxHp);
                float width = isBoss ? 300f : 120f;
                float height = isBoss ? 20f : 10f;
                float x = screenPos.x - width / 2f;
                float y = Screen.height - screenPos.y - 20f;

                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(x, y, width, height), SolidTex());

                Color fillColor;
                if (s.EnemyHpBarColorProgression)
                    fillColor = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;
                else
                    fillColor = isBoss ? new Color(1f, 0.5f, 0f) : Color.red;

                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(x, y, width * pct, height), SolidTex());

                GUI.color = Color.white;
            }
        }

        // crest unlock button
        private void DrawCrestUnlockUI()
        {
            if (Bonfire.Instance.Status.UnlockedAllCrests) return;

            GUILayout.Space(10);
            if (GUILayout.Button("Unlock All Crests (asks for confirmation)"))
                _pendingCrestUnlock = true;

            if (_pendingCrestUnlock)
            {
                GUI.color = new Color(0, 0, 0, 0.8f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;

                float w = 550f, h = 275f;
                Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
                GUI.ModalWindow(9999, modalRect, DrawConfirmModal, "Crest unlocking");
            }
        }

        // crest unlock modal window
        private void DrawConfirmModal(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "Unlocking all crests is permanent and cannot be undone\n",
                _crestUnlockLabelStyle
            );
            GUILayout.Label("Are you sure?", _crestUnlockLabelStyle);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Yes\nUnlock all crests",
                    _crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingCrestUnlock = false;
                Bonfire.Instance.Status.UnlockedAllCrests = true;

                foreach (var crest in Resources.FindObjectsOfTypeAll<ToolCrest>())
                {
                    Bonfire.Log.LogDebug("Unlocked Crest name: " + crest.name); // "Warrior" = beast crest
                    crest.Unlock();
                }

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(10);
            if (GUILayout.Button(
                    "No\nLeave my crests alone",
                    _crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingCrestUnlock = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(25);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Remove crest unlocking\n" + "(This will permanently hide the \"Unlock All Crests\" button)",
                    _crestUnlockButtonStyle,
                    GUILayout.Width(520f),
                    GUILayout.Height(55f)))
            {
                _pendingCrestUnlock = false;
                Bonfire.Instance.Status.UnlockedAllCrests = true;

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // bench fast travel unlock check
        private void FastTravelUnlockNotification()
        {
            if (Bonfire.Instance.Status.UnlockedFastTravel) return;

            if (PlayerData.instance.defeatedCogworkDancers)
            {
                GUI.color = new Color(0, 0, 0, 0.8f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.yellow;

                float w = 600f, h = 280f;
                Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
                GUI.ModalWindow(10000, modalRect, DrawFastTravelUnlockModalPopup, "Bench fast travel unlocked!");
            }
        }

        // fast travel unlock: GUI.ModalWindow callback for actual popup window
        private void DrawFastTravelUnlockModalPopup(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "You have defeated the Cogwork Dancers boss and " +
                "therefore have unlocked the bench fast travel feature!",
                _crestUnlockLabelStyle
            );

            GUILayout.Space(5);
            GUILayout.Label(
                "To fast travel:\n" +
                "- Open the map while sitting on any bench.\n" +
                "- Open the pin marker menu\n" +
                "- Move the marker on top of the bench icon you wish to travel to, then place the pin.\n",
                _fastTravelUnlockLabelStyle
            );

            GUILayout.Space(20);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Close this popup",
                    _crestUnlockButtonStyle,
                    GUILayout.Width(150f),
                    GUILayout.Height(50f)))
            {
                Bonfire.Instance.Status.UnlockedFastTravel = true;
                Bonfire.Instance.SaveSettings();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                    int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}: {displayValue}",
                    _buttonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(160f))
                && CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, _labelStyle, GUILayout.Height(40f), GUILayout.Width(160f));
        }

        private bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
        }

        // resets all pending (unapplied) stat allocations
        private void ResetPendingLevelUp()
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

        private void EnsureFonts()
        {
            if (_trajanBold != null && _trajanNormal != null) return;

            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font == null) continue;
                if (font.name == "TrajanPro-Bold") _trajanBold = font;
                if (font.name == "TrajanPro-Regular") _trajanNormal = font;
            }
        }

        private void EnsureStyles()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = _trajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (_crestUnlockLabelStyle == null)
            {
                _crestUnlockLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanBold,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }

            if (_crestUnlockButtonStyle == null)
            {
                _crestUnlockButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = _trajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_fastTravelUnlockLabelStyle == null)
            {
                _fastTravelUnlockLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanBold,
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };
            }

            if (_benchListAreaLabelStyle == null)
            {
                _benchListAreaLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanBold,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.8f, 0.75f, 0.1f) }
                };
            }

            if (_benchListButtonStyle == null)
            {
                _benchListButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = _trajanBold,
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5),
                };
            }

            if (_benchListButtonSelectedStyle == null)
            {
                _benchListButtonSelectedStyle = new GUIStyle(GUI.skin.button)
                {
                    font = _trajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                };
                _benchListButtonSelectedStyle.normal.background = Texture2D.whiteTexture;
            }

            if (_fastTravelConfirmHeaderStyle == null)
                _fastTravelConfirmHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanNormal,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.7f, 0.3f) }
                };

            if (_fastTravelConfirmDestinationStyle == null)
                _fastTravelConfirmDestinationStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanBold,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
        }

        private static bool InInventory()
        {
            GameObject go = GameObject.FindGameObjectWithTag("Inventory Top");
            if (go == null) return false;

            PlayMakerFSM fsm = FSMUtility.LocateFSM(go, "Inventory Control");
            if (fsm == null) return false;

            FsmBool open = fsm.FsmVariables.GetFsmBool("Open");
            return open != null && open.Value;
        }

        private static Texture2D SolidTex()
        {
            if (_tex == null)
            {
                _tex = new Texture2D(1, 1);
                _tex.SetPixel(0, 0, Color.white);
                _tex.Apply();
            }
            return _tex;
        }

        // -- patched methods -- 

        // these should prevent remaining inputs in bench fast travel menu that BlockInventoryInputs can't:
        // ESC/inventory pane navigation
        [HarmonyPatch(typeof(InventoryPaneInput), "Update")]
        class InventoryPaneInput_Update
        {
            static bool Prefix()
            {
                if (Instance != null && Instance._fastTravelDisplay)
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
                if (Instance != null && Instance._fastTravelDisplay)
                    return false;
                return true;
            }
        }
    }
}