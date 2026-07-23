using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Extra settings menu.
    /// </summary>
    public class Settings
    {
        public readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();

        public void DrawExtraSettingsButton()
        {
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");

            if (GUILayout.Button("Extra Settings", Styles._buttonStyle, GUILayout.Height(40f)))
            {
                _pendingCrestUnlock = false;
                _pendingSettingsWindow = true;
            }

            GUILayout.EndVertical();
        }

        public void DrawExtraSettingsUI()
        {
            if (!_pendingSettingsWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 550f, h = 420f;
            Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
            GUI.ModalWindow(10003, modalRect, DrawExtraSettingsModal, "Settings");
        }

        public void DrawEnemyHealthBars()
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

        public void DrawCrestUnlockUI()
        {
            if (!_pendingCrestUnlock) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 550f, h = 210f;
            Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
            GUI.ModalWindow(10002, modalRect, DrawCrestUnlockConfirmModal, "Crest unlocking");
        }

        public void DrawPlayerStatusResetUI()
        {
            if (!_pendingStatusReset) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.red;

            float w = 600f, h = 400f;
            Rect modalRect = new Rect(Screen.width / 2f - w / 2f, Screen.height / 2f - h / 2f, w, h);
            GUI.ModalWindow(10003, modalRect, DrawPlayerStatusResetConfirmModal, "Reset mod configurations");
        }


        private bool _pendingCrestUnlock = false;
        private bool _pendingSettingsWindow = false;
        private bool _pendingStatusReset = false;

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

        private void DrawExtraSettingsModal(int windowId)
        {
            DrawShellShardSettingsUI();
            GUILayout.Space(10);

            DrawEnemyHPToggleUI();
            GUILayout.Space(10);

            DrawOtherSettingsUI();
            GUILayout.Space(10);

            DrawCrestUnlockButton();
            GUILayout.Space(15);

            DrawPlayerStatusResetButton();
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button(
                    "Close window",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(35f)))
            {
                _pendingSettingsWindow = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawShellShardSettingsUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Shell shard replenishment");

            GUI.backgroundColor = Color.white;
            s.InfiniteShellShardsEnabled = GUILayout.Toggle(s.InfiniteShellShardsEnabled, "Infinite shards");

            if (!s.InfiniteShellShardsEnabled)
                s.InfiniteShardsBenchOnly = false;

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUI.enabled = s.InfiniteShellShardsEnabled;
            s.InfiniteShardsBenchOnly = GUILayout.Toggle(
                s.InfiniteShardsBenchOnly,
                "Shard refill to full only at benches");
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawEnemyHPToggleUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");
            GUILayout.Label("Enemy HP bars");

            GUI.backgroundColor = Color.white;
            s.EnemyHealthBarsEnabled = GUILayout.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars (global hotkey is LeftControl + H)");

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = GUILayout.Toggle(
                s.EnemyHpBarColorProgression,
                "Progressive HP bar values (green → yellow → red)");
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // other uncategorized features
        private void DrawOtherSettingsUI()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box");

            GUI.backgroundColor = Color.white;
            s.ReduceFleaGamesScoreLimit = GUILayout.Toggle(
                s.ReduceFleaGamesScoreLimit,
                "Reduce Fleatopia game score requirements by 50% (visuals still show original score)");

            GUILayout.EndVertical();
        }

        private void DrawCrestUnlockButton()
        {
            if (Bonfire.Instance.Status.UnlockedAllCrests) return;

            if (GUILayout.Button("Unlock All Crests (asks for confirmation)", GUILayout.Height(30f)))
            {
                _pendingSettingsWindow = false;
                _pendingCrestUnlock = true;
            }
        }

        private void DrawCrestUnlockConfirmModal(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "Unlocking all crests is permanent and cannot be undone\n",
                Styles._crestUnlockLabelStyle
            );
            GUILayout.Label("Are you sure?", Styles._crestUnlockLabelStyle);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Yes\nUnlock all crests",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingSettingsWindow = true;
                _pendingCrestUnlock = false;
                Bonfire.Instance.Status.UnlockedAllCrests = true;

                foreach (var crest in Resources.FindObjectsOfTypeAll<ToolCrest>())
                {
                    Bonfire.Log.LogDebug("Unlocked Crest name: " + crest.name); // "Warrior" = beast crest
                    crest.Unlock();
                }

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(30);
            if (GUILayout.Button(
                    "No\nLeave my crests alone",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingSettingsWindow = true;
                _pendingCrestUnlock = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawPlayerStatusResetButton()
        {
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Reset mod config (asks for confirmation)", GUILayout.Height(30f)))
            {
                _pendingSettingsWindow = false;
                _pendingStatusReset = true;
            }
        }

        private void DrawPlayerStatusResetConfirmModal(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "Resets your mod configurations e.g. stat levels, for CURRENT SAVE SLOT.\n" +
                "\n" +
                "Configs are save slot specific and loaded from an external file.\n" +
                "Config data remains even if you start a new game on same save slot.\n" +
                "This feature overrides the config data for currently active slot so you don't " +
                "need to manually search and delete the file outside the game.\n" +
                "\n" +
                "Using this will notably wipe all level progression and make you lose any rosaries " +
                "spent on leveling. You should only use it at the beginning of a new playthrough " +
                "if you want a fresh start.",
                Styles._crestUnlockLabelStyle
            );
            GUILayout.Space(10);
            GUILayout.Label(
                "Do you want to reset config for currently active save slot?",
                Styles._crestUnlockLabelStyle);

            GUILayout.Space(25);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(
                    "Yes\nReset config data",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingStatusReset = false;
                _pendingSettingsWindow = true;
                Bonfire.Instance.Status = new PlayerStatus();

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(30);
            if (GUILayout.Button(
                    "No\nKeep current data",
                    Styles._crestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingStatusReset = false;
                _pendingSettingsWindow = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}