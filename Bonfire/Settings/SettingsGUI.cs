using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Extra settings GUI.
    /// </summary>
    public class SettingsGUI
    {
        public readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();

        public void DrawExtraSettingsButton()
        {
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");

            if (CustomCursor.Button("Extra Settings", GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                _pendingCrestUnlock = false;
                _pendingSettingsWindow = true;
            }

            GUILayout.EndVertical();
        }

        public void DrawExtraSettingsUI()
        {
            if (GameManager.instance.isPaused || !PlayerData.instance.atBench)
            {
                _pendingSettingsWindow = false;
                _pendingCrestUnlock = false;
                _pendingStatusReset = false;
            }

            if (!_pendingSettingsWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 550f, h = 600f;
            Rect modalRect = new Rect(
                BonfireGUI.Width / 2f - w / 2f, BonfireGUI.Height / 2f - h / 2f,
                w, h);
            CustomCursor.Window(10003, modalRect, DrawExtraSettingsModal, "Settings");
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
                float y = BonfireGUI.Height - screenPos.y - 20f;

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
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 550f, h = 210f;
            Rect modalRect = new Rect(
                BonfireGUI.Width / 2f - w / 2f, BonfireGUI.Height / 2f - h / 2f,
                w, h);
            CustomCursor.Window(10002, modalRect, DrawCrestUnlockConfirmModal, "Crest unlocking");
        }

        public void DrawPlayerStatusResetUI()
        {
            if (!_pendingStatusReset) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.red;

            float w = 600f, h = 400f;
            Rect modalRect = new Rect(
                BonfireGUI.Width / 2f - w / 2f, BonfireGUI.Height / 2f - h / 2f,
                w, h);
            CustomCursor.Window(10003, modalRect, DrawPlayerStatusResetConfirmModal, "Reset mod configurations");
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
            DrawToggleSettings();
            GUILayout.Space(10);

            DrawCrestUnlockButton();
            GUILayout.Space(15);

            DrawPlayerStatusResetButton();
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;

            if (CustomCursor.Button(
                    "Close window",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(35f)))
            {
                _pendingSettingsWindow = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void DrawToggleSettings()
        {
            var s = Bonfire.Instance.Status;

            GUI.enabled = GameManager.instance.inputHandler.gameController.IsAttached;
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");
            GUILayout.Label("Custom cursor (enabled if a GamePad device is detected)");

            GUILayout.Label($"Current speed: {s.CustomCursorSpeed}");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            if (CustomCursor.Button($"- (min. {CustomCursor.MinSpeed})", GUILayout.Width(120)))
                s.CustomCursorSpeed = Mathf.Max(50, s.CustomCursorSpeed - 10);

            GUILayout.Space(10);
            if (CustomCursor.Button($"+ (max. {CustomCursor.MaxSpeed})", GUILayout.Width(120)))
                s.CustomCursorSpeed = Mathf.Min(1000, s.CustomCursorSpeed + 10);

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            s.DisplayGameCursorWithGamepad = CustomCursor.Toggle(
                s.DisplayGameCursorWithGamepad,
                "Display both custom and default cursors");
            GUILayout.EndVertical();
            GUI.enabled = true;

            GUILayout.Space(10);


            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");
            GUILayout.Label("Enemy HP bars");

            s.EnemyHealthBarsEnabled = CustomCursor.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars (global hotkey is LeftControl + H)");

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = CustomCursor.Toggle(
                s.EnemyHpBarColorProgression,
                "Progressive HP bar values (green → yellow → red)");
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(10);


            GUILayout.BeginVertical("box");
            GUILayout.Label("Delivery Quests");

            s.DeliveryQuestsRemoveDamage = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveDamage,
                "Remove damage penalties from delivery quests.");

            s.DeliveryQuestsRemoveTimer = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveTimer,
                "Remove timers from delivery quests.");

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Other");

            s.RefillShardsAtBench = CustomCursor.Toggle(
                s.RefillShardsAtBench,
                "Refill shell shards to full at bench");
            GUILayout.Space(5);

            s.ReduceFleaGamesScoreLimit = CustomCursor.Toggle(
                s.ReduceFleaGamesScoreLimit,
                "Reduce Fleatopia game score requirements by 50% (visuals still show original score)");

            GUILayout.EndVertical();
        }

        private void DrawCrestUnlockButton()
        {
            if (Bonfire.Instance.Status.UnlockedAllCrests) return;

            if (CustomCursor.Button("Unlock All Crests (asks for confirmation)", GUILayout.Height(30f)))
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
                GUIStyles.CrestUnlockLabelStyle
            );
            GUILayout.Label("Are you sure?", GUIStyles.CrestUnlockLabelStyle);

            GUILayout.Space(15);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (CustomCursor.Button(
                    "Yes\nUnlock all crests",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingSettingsWindow = true;
                _pendingCrestUnlock = false;
                Bonfire.Instance.Status.UnlockedAllCrests = true;

                Bonfire.Log.LogDebug("[SettingsGUI] Unlocking all crests");
                foreach (var crest in Resources.FindObjectsOfTypeAll<ToolCrest>())
                {
                    Bonfire.Log.LogDebug("Crest name: " + crest.name);
                    if (!crest.IsUnlocked)
                        crest.Unlock();
                }

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(30);
            if (CustomCursor.Button(
                    "No\nLeave my crests alone",
                    GUIStyles.CrestUnlockButtonStyle,
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
            if (CustomCursor.Button("Reset mod config for this save slot (asks for confirmation)",
                GUILayout.Height(30f)))
            {
                _pendingSettingsWindow = false;
                _pendingStatusReset = true;
            }
        }

        private void DrawPlayerStatusResetConfirmModal(int windowId)
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "Resets your mod configurations e.g. stat levels and settings, for CURRENT SAVE SLOT.\n" +
                "\n" +
                "Configs are save slot specific and loaded from an external file.\n" +
                "Config data remains even if you start a new game on same save slot.\n" +
                "This feature overrides the config data for currently active slot so you don't " +
                "need to manually search and edit/delete the file outside the game.\n" +
                "\n" +
                "You should only use this feature if you're done with your current playthrough and " +
                "want to wipe all the progress for a fresh start.",
                GUIStyles.CrestUnlockLabelStyle
            );
            GUILayout.Space(10);
            GUILayout.Label(
                $"Reset mod configurations for save slot {GameManager.instance.profileID}?",
                GUIStyles.CrestUnlockLabelStyle);

            GUILayout.Space(25);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (CustomCursor.Button(
                    "Yes\nReset config data",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                _pendingStatusReset = false;
                _pendingSettingsWindow = true;
                Bonfire.Instance.Status = new PlayerStatus();

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(30);
            if (CustomCursor.Button(
                    "No\nKeep current data",
                    GUIStyles.CrestUnlockButtonStyle,
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