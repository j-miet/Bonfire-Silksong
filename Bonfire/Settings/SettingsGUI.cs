using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Extra settings GUI.
    /// </summary>
    public class SettingsGUI
    {
        public static bool PendingSettingsWindow = false;
        public static bool PendingCrestUnlock = false;
        public static bool PendingStatusReset = false;

        public void DrawExtraSettingsButton()
        {
            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");

            if (CustomCursor.Button("Extra Settings", GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                PendingCrestUnlock = false;
                PendingSettingsWindow = true;
            }

            GUILayout.EndVertical();
        }

        public void DrawExtraSettingsUI()
        {
            if (!PendingSettingsWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 590f, h = 670f;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;
            Rect rect = new Rect(x, y, w, h);

            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.3f, 0.3f, 0.3f, 3f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Box(rect, GUIContent.none);
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(10, 10, rect.width - 20, rect.height - 20));
            _settingsScrollPos = GUILayout.BeginScrollView(_settingsScrollPos);

            GUILayout.Label("Settings", GUIStyles.LabelStyle);
            DrawExtraSettingsContent();

            GUILayout.EndScrollView();

            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }

        public void DrawCrestUnlockUI()
        {
            if (!PendingCrestUnlock) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 550f, h = 210f;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;
            Rect rect = new Rect(x, y, w, h);

            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Box(rect, GUIContent.none);
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(20, 20, rect.width - 40, rect.height - 40));

            GUILayout.Label("Crest Unlocking", GUIStyles.LabelStyle);
            DrawCrestUnlockConfirmContent();

            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }

        public void DrawPlayerStatusResetUI()
        {
            if (!PendingStatusReset) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 600f, h = 400f;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;
            Rect rect = new Rect(x, y, w, h);

            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            GUI.color = new Color(0.8f, 0.1f, 0.1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Box(rect, GUIContent.none);
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(20, 20, rect.width - 40, rect.height - 40));

            GUILayout.Label("Reset mod configurations", GUIStyles.LabelStyle);
            DrawPlayerStatusResetContent();

            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }

        public void DrawEnemyHealthBars()
        {
            var s = Bonfire.Instance.Status;

            if (!s.EnemyHealthBarsEnabled) return;

            foreach (var kvp in EnemyHealth.EnemyMaxHp)
            {
                HealthManager hm = kvp.Key;
                if (hm == null) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(hm.transform.position);
                if (screenPos.z < 0) continue;

                float maxHp = kvp.Value;

                // extra check + update for enemies whose HP might get updated after initial data check 
                // (e.g. Act3 void enemies)
                if (hm.hp > maxHp)
                {
                    EnemyHealth.EnemyMaxHp[hm] = hm.hp;
                    maxHp = hm.hp;
                }

                float currentHp = hm.hp;
                bool isBoss = EnemyHealth.EnemyIsBoss.TryGetValue(hm, out bool b) && b;

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
                    fillColor = pct > 0.6f ? _progHigh : pct > 0.3f ? _progMedium : _progLow;
                else
                    fillColor = isBoss ? _bossColor : _enemyColor;

                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(x, y, width * pct, height), SolidTex());

                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Initializes hp bar custom colors for settings window.
        /// </summary>
        public void EnsureHpBarColors()
        {
            // UnityEngine.Color is struct: it's a value type 'color == null' checks are always false, unlike with
            // UnityEngine.GUIStyle. Better to first check GUIStyles null values and only then update 
            // colors.

            if (_hpBarColorsInitialized)
                return;

            if (GUIStyles.EnemyHpStyle == null ||
                GUIStyles.BossHpStyle == null ||
                GUIStyles.HpLowStyle == null ||
                GUIStyles.HpMediumStyle == null ||
                GUIStyles.HpHighStyle == null)
                return;

            _enemyColor = GUIStyles.EnemyHpStyle.normal.textColor;
            _bossColor = GUIStyles.BossHpStyle.normal.textColor;
            _progLow = GUIStyles.HpLowStyle.normal.textColor;
            _progMedium = GUIStyles.HpMediumStyle.normal.textColor;
            _progHigh = GUIStyles.HpHighStyle.normal.textColor;

            _hpBarColorsInitialized = true;
        }


        private static bool _hpBarColorsInitialized;
        private static Color _bossColor;
        private static Color _enemyColor;
        private static Color _progLow;
        private static Color _progMedium;
        private static Color _progHigh;
        private static Texture2D _tex;

        private Vector2 _settingsScrollPos;

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

        private void DrawExtraSettingsContent()
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
                PendingSettingsWindow = false;
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

            GUILayout.Label(
                $"Cursor speed [min. {CustomCursor.MinSpeed}, max. {CustomCursor.MaxSpeed}]");
            GUILayout.Space(10);

            GUILayout.BeginHorizontal(GUILayout.Width(160));
            if (CustomCursor.Button($"-", GUILayout.Width(60)))
                s.CustomCursorSpeed = Mathf.Max(50, s.CustomCursorSpeed - 10);

            GUILayout.Space(20);
            GUILayout.Label($"{s.CustomCursorSpeed}");
            GUILayout.Space(20);

            if (CustomCursor.Button($"+", GUILayout.Width(60)))
                s.CustomCursorSpeed = Mathf.Min(1000, s.CustomCursorSpeed + 10);

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            s.DisplayGameCursorWithGamepad = CustomCursor.Toggle(
                s.DisplayGameCursorWithGamepad,
                "Display both custom and default cursor");
            GUILayout.EndVertical();
            GUI.enabled = true;

            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Custom");

            s.ShowCustomRangeIndicator = CustomCursor.Toggle(
                s.ShowCustomRangeIndicator,
                "Display damage range indicator in Taunt/Reflect.");

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUI.backgroundColor = Color.white;
            GUILayout.BeginVertical("box");
            GUILayout.Label("Enemy HP bars");

            GUILayout.BeginHorizontal(GUILayout.Width(480));
            s.EnemyHealthBarsEnabled = CustomCursor.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars");

            GUILayout.Label("(");
            GUILayout.Label("enemy", GUIStyles.EnemyHpStyle);
            GUILayout.Label("/");
            GUILayout.Label("boss", GUIStyles.BossHpStyle);
            GUILayout.Label("→");
            GUILayout.Label("default hotkey is LeftControl + H)");
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(GUILayout.Width(370));
            GUILayout.Space(15);

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = CustomCursor.Toggle(
                s.EnemyHpBarColorProgression,
                "Progressive HP bar values");

            GUILayout.Label("(");
            GUILayout.Label("high", GUIStyles.HpHighStyle);
            GUILayout.Label("→");
            GUILayout.Label("medium", GUIStyles.HpMediumStyle);
            GUILayout.Label("→");
            GUILayout.Label("low", GUIStyles.HpLowStyle);
            GUILayout.Label(")");
            GUI.enabled = true;

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Delivery Quests");

            s.DeliveryQuestsRemoveDamage = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveDamage,
                "Remove damage penalty");

            s.DeliveryQuestsRemoveTimer = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveTimer,
                "Remove timer penalty");

            GUILayout.EndVertical();
            GUILayout.Space(10);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Other");

            s.AlwaysHaveCompassTool = CustomCursor.Toggle(
                s.AlwaysHaveCompassTool,
                "Compass on minimap");

            s.RefillShardsAtBench = CustomCursor.Toggle(
                s.RefillShardsAtBench,
                "Refill shell shards to full at bench");

            s.ReduceFleaGamesScoreLimit = CustomCursor.Toggle(
                s.ReduceFleaGamesScoreLimit,
                "Reduce Fleatopia game score requirements by 50% (scoreboard still shows base values)");

            s.EasierSprintmasterRaces = CustomCursor.Toggle(
                s.EasierSprintmasterRaces,
                "Reduce Sprintmaster Swift's movement speed in races by 50%.");

            GUILayout.EndVertical();
        }

        private void DrawCrestUnlockButton()
        {
            if (Bonfire.Instance.Status.UnlockedAllCrests) return;

            if (CustomCursor.Button("Unlock All Crests (asks for confirmation)", GUILayout.Height(30f)))
            {
                PendingSettingsWindow = false;
                PendingCrestUnlock = true;
            }
        }

        private void DrawCrestUnlockConfirmContent()
        {
            GUILayout.Space(10);
            GUILayout.Label(
                "Unlocking all crests is permanent and cannot be undone.\n",
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
                PendingSettingsWindow = true;
                PendingCrestUnlock = false;
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
                PendingSettingsWindow = true;
                PendingCrestUnlock = false;
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
                PendingSettingsWindow = false;
                PendingStatusReset = true;
            }
        }

        private void DrawPlayerStatusResetContent()
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
                PendingStatusReset = false;
                PendingSettingsWindow = true;
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
                PendingStatusReset = false;
                PendingSettingsWindow = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}