using System.Collections.Generic;
using System.Linq;
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
            if (_crestDict.Count == 0)
                InitCrestDictionary();

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

            float w = SettingsWidth, h = SettingsHeight;
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

            CustomCursor.BeginArea(
                new Rect(
                    SettingsOuterOffset,
                    SettingsOuterOffset,
                    rect.width - SettingsInnerOffset,
                    rect.height - SettingsInnerOffset));

            _settingsScrollPos = CustomCursor.BeginScrollView(_settingsScrollPos);

            DrawExtraSettingsContent();

            CustomCursor.EndScrollView();

            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }

        public void DrawCrestUnlockUI()
        {
            if (!PendingCrestUnlock) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = CrestUnlockWidth, h = CrestUnlockHeight;
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

            float w = StatusResetWidth, h = StatusResetHeight;
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


        private const float SettingsWidth = 590f;
        private const float SettingsHeight = 675f;
        private const float SettingsOuterOffset = 10f;
        private const float SettingsInnerOffset = 2 * SettingsOuterOffset;
        private const float SettingsAdjustButtonWidth = 60f;
        private const float SettingsAdjustButtonHeight = 30f;

        private const float StatusResetWidth = 600f;
        private const float StatusResetHeight = 530f;

        private const float CrestUnlockWidth = 420f;
        private const float CrestUnlockHeight = 610f;

        private readonly string[] _crestNames = { "Wanderer", "Reaper", "Warrior", "Witch", "Toolmaster", "Spell" };
        private readonly Dictionary<string, ToolCrest> _crestDict = new Dictionary<string, ToolCrest>();
        private bool _allCrestsUnlocked = false;

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
            GUILayout.Space(15);

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

        private void DrawAdjustUI()
        {
            var s = Bonfire.Instance.Status;

            // adjust step size
            GUILayout.Label($"Adjust button step size (how much -/+ buttons changes the target value; range 5-100)");
            GUILayout.BeginHorizontal(GUILayout.Width(160));
            if (CustomCursor.Button(
                    $"-",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.AdjustButtonStep = (int)Mathf.Max(DamageOverlay.BoxOffset, s.AdjustButtonStep - 5);
            }

            GUILayout.Space(10);
            GUILayout.Label(
                $"{s.AdjustButtonStep}",
                GUIStyles.LabelCenteredStyle,
                GUILayout.Width(30),
                GUILayout.Height(SettingsAdjustButtonHeight));
            GUILayout.Space(10);

            if (CustomCursor.Button(
                    $"+",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.AdjustButtonStep = Mathf.Min(100, s.AdjustButtonStep + 5);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawCursorSettings()
        {
            var s = Bonfire.Instance.Status;

            GUI.enabled = GameManager.instance.inputHandler.gameController.IsAttached;

            GUILayout.Space(10);
            GUILayout.Label("Custom cursor (enabled if a GamePad device is detected)", GUIStyles.SettingsHeader2);

            GUILayout.Label(
                $"Cursor speed (range {CustomCursor.MinSpeed} - {CustomCursor.MaxSpeed}, " +
                "will also scale scroll speed)");

            GUILayout.BeginHorizontal(GUILayout.Width(160));
            if (CustomCursor.Button(
                    $"-",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.CustomCursorSpeed = Mathf.Max(50, s.CustomCursorSpeed - s.AdjustButtonStep);
            }

            GUILayout.Space(10);
            GUILayout.Label(
                $"{s.CustomCursorSpeed}",
                GUIStyles.LabelCenteredStyle,
                GUILayout.Width(30),
                GUILayout.Height(SettingsAdjustButtonHeight));
            GUILayout.Space(10);

            if (CustomCursor.Button(
                    $"+",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.CustomCursorSpeed = Mathf.Min(1000, s.CustomCursorSpeed + s.AdjustButtonStep);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            s.DisplayGameCursorWithGamepad = CustomCursor.Toggle(
                s.DisplayGameCursorWithGamepad,
                "Display both custom and default cursor");

            GUI.enabled = true;
        }

        private void DrawOverlaySettings()
        {
            var s = Bonfire.Instance.Status;

            int widthMax = (int)(BonfireGUI.Width - DamageOverlay.Width - DamageOverlay.BoxOffset);
            int heightMax = (int)(BonfireGUI.Height - DamageOverlay.Height - DamageOverlay.BoxOffset);

            GUILayout.Space(10);
            GUILayout.Label("Components", GUIStyles.SettingsHeader2);

            GUILayout.BeginHorizontal(GUILayout.Width(520));
            s.EnemyHealthBarsEnabled = CustomCursor.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars");

            GUILayout.Label("(");
            GUILayout.Label("enemy", GUIStyles.EnemyHpStyle);
            GUILayout.Label("/");
            GUILayout.Label("boss", GUIStyles.BossHpStyle);
            GUILayout.Label("→");
            GUILayout.Label("has default hotkey LeftControl + H)");
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

            s.ShowCustomRangeIndicator = CustomCursor.Toggle(
                s.ShowCustomRangeIndicator,
                "Display damage range indicator in Taunt/Reflect.");
            s.ShowDamageOverlay = CustomCursor.Toggle(
                s.ShowDamageOverlay,
                "Display info from latest enemy damaging instance (has default hotkey LeftControl + L).");

            GUI.enabled = s.ShowDamageOverlay;
            GUILayout.BeginHorizontal(GUILayout.Width(450));
            GUILayout.Label($"X-Position");
            GUILayout.Label($"Y-Position");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(450));
            GUILayout.BeginHorizontal();
            if (CustomCursor.Button(
                    $"-",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.DamageOverlayX = (int)Mathf.Max(DamageOverlay.BoxOffset, s.DamageOverlayX - s.AdjustButtonStep);
            }

            GUILayout.Space(10);
            GUILayout.Label(
                $"{s.DamageOverlayX}",
                GUIStyles.LabelCenteredStyle,
                GUILayout.Width(30),
                GUILayout.Height(SettingsAdjustButtonHeight));
            GUILayout.Space(10);

            if (CustomCursor.Button(
                    $"+",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.DamageOverlayX = Mathf.Min(widthMax, s.DamageOverlayX + s.AdjustButtonStep);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (CustomCursor.Button(
                    $"-",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.DamageOverlayY = (int)Mathf.Max(DamageOverlay.BoxOffset, s.DamageOverlayY - s.AdjustButtonStep);
            }

            GUILayout.Space(10);
            GUILayout.Label(
                $"{s.DamageOverlayY}",
                GUIStyles.LabelCenteredStyle,
                GUILayout.Width(30),
                GUILayout.Height(SettingsAdjustButtonHeight));
            GUILayout.Space(10);

            if (CustomCursor.Button(
                    $"+",
                    GUILayout.Width(SettingsAdjustButtonWidth),
                    GUILayout.Height(SettingsAdjustButtonHeight)))
            {
                s.DamageOverlayY = Mathf.Min(heightMax, s.DamageOverlayY + s.AdjustButtonStep);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        private void DrawFastTravelSettings()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.Space(10);
            s.ResetMusicAfterTeleport = CustomCursor.Toggle(
                s.ResetMusicAfterTeleport,
                "Resets music after bench fast travel\n" +
                "\n" +
                "Normally music/atmosphere carries over to target scene, but then fades out and gets replaced with " +
                "the music from new scene. After fast travel, previous sound effects can linger much longer than " +
                "they should. With this enabled, existing music track cuts off immediately and the new one starts to " +
                "play as soon as scene transition is completed.",
                GUIStyles.WordWrapToggleStyle,
                GUILayout.Width(SettingsWidth - SettingsOuterOffset - SettingsInnerOffset - 50));
        }

        private void DrawGameplaySettings()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.Space(10);
            GUILayout.Label("Delivery Quests", GUIStyles.SettingsHeader2);

            s.DeliveryQuestsRemoveDamage = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveDamage,
                "Remove damage penalty");

            s.DeliveryQuestsRemoveTimer = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveTimer,
                "Remove timer penalty");

            GUILayout.Space(10);
            GUILayout.Label("Other", GUIStyles.SettingsHeader2);

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
        }

        private void DrawToggleSettings()
        {
            var s = Bonfire.Instance.Status;

            // adjust step size
            GUILayout.BeginVertical("box");
            GUILayout.Label("General", GUIStyles.SettingsHeader);
            DrawAdjustUI();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Overlay", GUIStyles.SettingsHeader);
            DrawCursorSettings();
            GUILayout.Space(10);
            DrawOverlaySettings();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Bench Fast Travel", GUIStyles.SettingsHeader);
            DrawFastTravelSettings();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label("Gameplay", GUIStyles.SettingsHeader);
            DrawGameplaySettings();
            GUILayout.EndVertical();
        }

        private void DrawCrestUnlockButton()
        {
            if (CustomCursor.Button("Crest Unlocking", GUILayout.Height(30f)))
            {
                PendingSettingsWindow = false;
                PendingCrestUnlock = true;
            }
        }

        private void DrawCrestUnlockConfirmContent()
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                $"{(_allCrestsUnlocked ? "All crests already unlocked" : "")}",
                GUIStyles.CrestUnlockLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            GUILayout.Space(10);
            CrestUnlockButton("Wanderer", "Wanderer");
            CrestUnlockButton("Reaper", "Reaper");
            CrestUnlockButton("Beast", "Warrior");
            CrestUnlockButton("Witch", "Witch");
            CrestUnlockButton("Architect", "Toolmaster");
            CrestUnlockButton("Shaman", "Spell");

            GUILayout.Space(30);
            CrestUnlockButton("Unlock All Crests", "All");
            GUILayout.Space(30);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Color closeOld = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            if (CustomCursor.Button(
                "Close Window",
                GUIStyles.CrestUnlockButtonStyle,
                GUILayout.Width(150f),
                GUILayout.Height(50f)))
            {
                PendingSettingsWindow = true;
                PendingCrestUnlock = false;
            }
            GUI.backgroundColor = closeOld;

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
                "Resets your mod configurations for CURRENT SAVE SLOT which includes: \n" +
                "- Leveling data -\n" +
                "- Bench fast travel unlock + safety checks -\n" +
                "(to prevent softlocks e.g. with Act 3 Abyss travel on new saves)\n" +
                "\n" +
                "<-- This will not reset your 'Extra Settings' values! -->\n" +
                "\n" +
                "Configs are save slot specific and loaded from an external file.\n" +
                "Config data remains even if you start a new game on same save slot.\n" +
                "This feature overrides only the config data for currently active slot so you don't " +
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

                ResetPlayerStatus();

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

        private void InitCrestDictionary()
        {
            var toolcrests = Resources.FindObjectsOfTypeAll<ToolCrest>();
            if (toolcrests == null) return;

            foreach (var crest in toolcrests)
            {
                if (_crestNames.Contains(crest.name))
                    _crestDict[crest.name] = crest;
            }

            Bonfire.Log.LogDebug("[SettingsGUI] Crest dictionary initialized.");

            foreach (var c in _crestDict.Values)
            {
                if (!c.IsUnlocked)
                    return;
            }

            _allCrestsUnlocked = true;

            Bonfire.Log.LogDebug("All crests already unlocked.");
        }

        private void CrestUnlockButton(string displayName, string crestName)
        {
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (crestName == "All")
            {
                GUI.enabled = !_allCrestsUnlocked;

                Color bgOld = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.6f, 0.6f, 1f);

                if (CustomCursor.Button(
                    $"{displayName}",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(250f),
                    GUILayout.Height(40f)))
                {
                    UnlockCrest("All");
                }
                GUI.backgroundColor = bgOld;

                GUI.enabled = true;
            }
            else
            {
                bool unlocked = _crestDict[crestName].IsUnlocked;
                GUI.enabled = !unlocked;

                if (CustomCursor.Button(
                    $"{displayName} {(unlocked ? " (Unlocked)" : "")}",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(250f),
                    GUILayout.Height(40f)))
                {
                    UnlockCrest(crestName);
                }
                GUI.enabled = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private void UnlockCrest(string name)
        {
            if (name == "All")
            {
                foreach (var c in _crestDict.Values)
                {
                    if (c.IsUnlocked) continue;

                    c.Unlock();

                    Bonfire.Log.LogDebug("Crest Unlocked: " + name);
                }

                _allCrestsUnlocked = true;
            }
            else if (_crestDict.Keys.Contains(name))
            {
                _crestDict[name].Unlock();

                Bonfire.Log.LogDebug("Crest Unlocked: " + name);
            }

            Bonfire.Instance.SaveSettings();
        }

        private void ResetPlayerStatus()
        {
            var s = Bonfire.Instance.Status;
            var newStatus = new PlayerStatus()
            {
                LevelingInfoStyle = s.LevelingInfoStyle,
                BenchMenuStyle = s.BenchMenuStyle,

                ResetMusicAfterTeleport = s.ResetMusicAfterTeleport,

                CustomCursorSpeed = s.CustomCursorSpeed,
                DisplayGameCursorWithGamepad = s.DisplayGameCursorWithGamepad,

                ShowCustomRangeIndicator = s.ShowCustomRangeIndicator,
                ShowDamageOverlay = s.ShowDamageOverlay,
                DamageOverlayX = s.DamageOverlayX,
                DamageOverlayY = s.DamageOverlayY,
                AdjustButtonStep = s.AdjustButtonStep,

                EnemyHealthBarsEnabled = s.EnemyHealthBarsEnabled,
                EnemyHpBarColorProgression = s.EnemyHpBarColorProgression,
                DeliveryQuestsRemoveTimer = s.DeliveryQuestsRemoveTimer,
                DeliveryQuestsRemoveDamage = s.DeliveryQuestsRemoveDamage,
                AlwaysHaveCompassTool = s.AlwaysHaveCompassTool,
                RefillShardsAtBench = s.RefillShardsAtBench,
                ReduceFleaGamesScoreLimit = s.ReduceFleaGamesScoreLimit,
                EasierSprintmasterRaces = s.EasierSprintmasterRaces,
            };

            Bonfire.Instance.Status = newStatus;
        }
    }
}