using System.Collections.Generic;

using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace Bonfire
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

            DrawEnemyHealthBars();  // shown outside the bench check
        }

        private Font _trajanBold;
        private Font _trajanNormal;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        private static Texture2D _tex;
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

        // internal Unity methods for startup and GUI updates
        private void Awake()
        {
            Instance = this;
        }

        // bench ui

        private void DrawBenchUI()
        {
            GUILayout.BeginArea(new Rect(20f, Screen.height / 4.0f, 530f, 700f));

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

            if (!s.EnemyHealthBarsEnabled)
                s.EnemyHpBarColorProgression = false;

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = GUILayout.Toggle(s.EnemyHpBarColorProgression,
                "Toggle HP progression (green -> yellow -> red)");
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        /// draws floating health bars above tracked enemies
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
