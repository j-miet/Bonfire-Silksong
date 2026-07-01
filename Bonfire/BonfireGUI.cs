using System;
using System.Collections.Generic;

using BepInEx.Configuration;
using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    public class BonfireGUI : MonoBehaviour
    {
        public static BonfireGUI Instance;

        public readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();

        public void OnGUI()
        {
            if (GameManager.instance == null) return;

            if (!(GameManager.instance.GameState == GameState.PLAYING ||
                  GameManager.instance.GameState == GameState.PAUSED) ||
                InInventory())
                return;

            EnsureFonts();

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


        private ConfigEntry<KeyboardShortcut> EnemyHpBarToggleKey;
        private bool enemyHPBarsEnabled = false;

        private Font _trajanBold;
        private Font _trajanNormal;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        private static Texture2D _tex;

        private void Awake()
        {
            Instance = this;
            EnemyHpBarToggleKey = Bonfire.Instance.Config.Bind(
                "Hotkeys",
                "Enemy HP bar Toggle",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Toggle enemy HP bars on/off"
                );
        }

        private void Update()
        {
            // enemy hp bar toggle via hotkey
            if (EnemyHpBarToggleKey.Value.IsDown())
            {
                enemyHPBarsEnabled = !enemyHPBarsEnabled;
                Bonfire.Instance.Status.EnemyHealthBarsEnabled = enemyHPBarsEnabled;
            }
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

        private void DrawRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, SolidTex());
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

            int currentLvl = s.StrengthStat + s.DexterityStat + s.LuckStat
                          + s.ResilienceStat + s.WisdomStat + s.IntelligenceStat
                          + s.PendingRelicLevels
                          + s.PendingRosaryLevels
                          - 5;

            // preview strings
            string totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            string totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            string totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            string totalLck = (s.LuckStat + ls.LuckIncrease).ToString();

            string needleDamage = ls.NeedleDamage(s.StrengthStat + ls.StrengthIncrease).ToString();

            string attackSpeed = ls.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            string critDamage = ls.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100).ToString();

            string spellDamage = ls.SpellDamage(100, s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            string bindCost = ls.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();

            string extraMasks = ls.ExtraMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            string blockChance = s.ResilienceStat + ls.ResilienceIncrease > 1
                ? ls.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0")
                : "0";

            string extraSilk = ls.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            string silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 1)
            {
                int regen = ls.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                silkRegenInterval = regen > 0 ? regen.ToString() + " s" : "-";
            }

            string critChance = ls.CritChance(s.LuckStat + ls.LuckIncrease).ToString();
            string rosaryIncrease = (5 * (s.LuckStat + ls.LuckIncrease - 1)).ToString();

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
            GUILayout.BeginArea(new Rect(20f, Screen.height / 4.0f, 530f, 700f));
            GUILayout.Label("Level Up", _labelStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButton("Strength", totalStr, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Dexterity", totalDex, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Intelligence", totalInt, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Resilience", totalRes, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Wisdom", totalWsdm, gotFreeLevel, s, rosariesToLvlUp);
            DrawStatButton("Luck", totalLck, gotFreeLevel, s, rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("Needle Damage: " + needleDamage);
            DrawStatLabel("Slash Speed: " + attackSpeed);
            DrawStatLabel("Silk Skill Damage: " + spellDamage + "%");
            DrawStatLabel("Extra Lifeblood Masks: " + extraMasks);
            DrawStatLabel("Silk Gained On Hit: " + extraSilk);
            DrawStatLabel("Crit Chance: " + critChance + "%");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("");   // spacer to align with column 1
            DrawStatLabel("Crit Damage: " + critDamage + "%");
            DrawStatLabel("Binding Cost: " + bindCost);
            DrawStatLabel("Block Chance: " + blockChance + "%");
            DrawStatLabel("Silk Regen interval: " + silkRegenInterval);
            DrawStatLabel("Rosary + Shard Increase: " + rosaryIncrease + "%");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Label(new GUIContent("Current Level: " + currentLvl), _labelStyle);
            GUILayout.Label(new GUIContent("Rosaries to Level Up: " + rosariesToLvlUp), _labelStyle);

            // apply and cancel
            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(new GUIContent(applyText), _buttonStyle, GUILayout.Height(40f), GUILayout.Width(258f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (GUILayout.Button(new GUIContent("Cancel"), _buttonStyle,
                    GUILayout.Height(40f), GUILayout.Width(258f))
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
                    new GUIContent($"Respec ({s.RespecCost}  Silkeater)"),
                    _buttonStyle,
                    GUILayout.Height(40f), GUILayout.Width(522f))
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            DrawShellShardSettingsUI();
            DrawEnemyHPToggleUI();

            GUILayout.EndArea();
        }

        // ui panel for shell shard replenishment
        private void DrawShellShardSettingsUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Shell shard settings");

            GUI.backgroundColor = Color.white;
            s.InfiniteShellShardsEnabled = GUILayout.Toggle(s.InfiniteShellShardsEnabled, "Infinite shards");

            if (!s.InfiniteShellShardsEnabled)
                s.InfiniteShardsBenchOnly = false;

            GUI.enabled = s.InfiniteShellShardsEnabled;
            s.InfiniteShardsBenchOnly = GUILayout.Toggle(s.InfiniteShardsBenchOnly, "Replenish shards only at bench");
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        // toggle button to enable/disable enemy HP bars
        private void DrawEnemyHPToggleUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");

            GUI.backgroundColor = Color.white;
            s.EnemyHealthBarsEnabled = GUILayout.Toggle(s.EnemyHealthBarsEnabled, "Enemy Health Bars");

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = GUILayout.Toggle(s.EnemyHpBarColorProgression,
                "Toggle HP progression (green -> yellow -> red)");
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

                DrawRect(new Rect(x, y, width, height), new Color(0f, 0f, 0f, 0.7f));

                Color fillColor;
                if (s.EnemyHpBarColorProgression)
                    fillColor = pct > 0.6f ? Color.green : pct > 0.3f ? Color.yellow : Color.red;
                else
                    fillColor = isBoss ? new Color(1f, 0.5f, 0f) : Color.red;

                DrawRect(new Rect(x, y, width * pct, height), fillColor);

                GUI.color = Color.white;
            }
        }

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                    int rosariesToLvlUp)
        {
            if (GUILayout.Button(
                    new GUIContent($"{statName}: {displayValue}"),
                    _buttonStyle,
                    GUILayout.Height(40f), GUILayout.Width(160f))
                && CanLevelUp(gotFreeLevel, rosariesToLvlUp) && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(new GUIContent(text), _labelStyle,
                GUILayout.Height(40f), GUILayout.Width(160f));
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
            ls.WisdomIncrease = 0;
            ls.ResilienceIncrease = 0;
            ls.IntelligenceIncrease = 0;
            ls.LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;
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
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15
                };
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = _trajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter
                };
            }
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
    }
}
