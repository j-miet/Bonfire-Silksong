using System;
using System.Collections.Generic;
using System.Linq;

using GlobalEnums;
using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// GUI component drawing
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
                GUILogic.InInventory())
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
                GUILogic.ResetPendingLevelUp();
        }


        // custom order for area names, in-game fast travel bench list then displays them in same order
        // GUILogic._benchStaticLocations is the source of truth: if there isn't a valid map area under any of the
        // scenes, it will be ignored and falls to "Other" category instead.
        private readonly List<(string areaName, Color32 color)> _orderedBenchAreaNames = new List<(string, Color32)>
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

        private MapPin _selectedPin = null;
        private string _selectedFullName;
        private bool _fastTravelDisplay = false;
        private Vector2 _scrollPos;

        private bool _pendingCrestUnlock = false;

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

        private static Texture2D _tex;
        private static readonly Color32 _defaultMenuColor = Rgb(204, 191, 26);

        private void Awake()
        {
            Instance = this;
        }

        // -- fast travel --

        private void DrawBenchFastTravelButton()
        {
            GUI.backgroundColor = Color.blue;
            GUILayout.BeginVertical("box");

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("Fast travel menu", _buttonStyle, GUILayout.Height(35f)))
            {
                Bonfire.Log.LogDebug("Fast travel menu open button was clicked");
                if (!GUILogic.UpdateCurrentBenchFSM()) return;

                GUILogic.SendOpenMapFSM();
                _fastTravelDisplay = true;

                GUILogic.BlockInventoryInputs();

                StartCoroutine(GUILogic.DelayedPinUpdate());
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
            var menuStyle = Bonfire.Instance.Status.ToggleColoredTeleportMenuStyle;

            GUI.backgroundColor = Color.white;
            GUILayout.BeginHorizontal("box");
            if (GUILayout.Button($"Style: {(menuStyle ? "Colored" : "Basic")}", _buttonStyle, GUILayout.Height(30f)))
            {
                var s = Bonfire.Instance.Status;
                s.ToggleColoredTeleportMenuStyle = !s.ToggleColoredTeleportMenuStyle;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            GUILayout.Label("Bench locations", _labelStyle);
            GUILayout.Space(10);

            if (GUILogic.BenchPinScenes.Count == 0)
            {
                Bonfire.Log.LogDebug("Loading bench locations...");
                GUILogic.UpdateBenchPins();
            }
            else
            {
                _scrollPos = GUILayout.BeginScrollView(_scrollPos);

                foreach ((string mapArea, Color32 c) in _orderedBenchAreaNames)
                {
                    if (!GUILogic.BenchActiveLocationData.Keys.Contains(mapArea)) continue;

                    Color bgOld = GUI.backgroundColor;
                    Color textOld = GUI.color;

                    Color bg;
                    Color textLabel;
                    Color text;
                    if (menuStyle)
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
                    GUILayout.Label(mapArea, _benchListAreaLabelStyle);

                    GUILayout.Space(4);

                    foreach (var (customName, mapPin) in GUILogic.BenchActiveLocationData[mapArea])
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

                        //GUI.backgroundColor = isSelected ? new Color(0.8f, 0.4f, 0f) : Color.white;
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
            if (GUILayout.Button("Close teleport menu", _buttonStyle, GUILayout.Height(40f)))
            {
                _selectedPin = null;
                _fastTravelDisplay = false;
                GUILogic.BenchPinScenes.Clear();

                StartCoroutine(GUILogic.CloseInventoryMap());
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
                GUILogic.BenchPinScenes.Clear();

                StartCoroutine(GUILogic.CloseInventoryMap());
                BenchTeleport.TeleportToBench();
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // -- QoL

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

        // -- main UI --

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                    int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    $"{statName}: {displayValue}",
                    _buttonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(160f))
                && GUILogic.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, _labelStyle, GUILayout.Height(40f), GUILayout.Width(160f));
        }

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
                GUILogic.ResetPendingLevelUp();
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

        // -- styling --

        private static Color32 Rgb(byte r, byte g, byte b) => new Color32(r, g, b, 255);

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
        // technically responsibility of GUILogic, but this avoids cross-referencing between classes

        // these should prevent remaining inputs in bench fast travel menu that GUILogic.BlockInventoryInputs can't:
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