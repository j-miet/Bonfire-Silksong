using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Leveling;

namespace BonfireSilksong.Settings
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

            GUI.enabled = BonfireGUI.NoWindowsPending;
            if (CustomCursor.Button(_loc.Get("settings.title"), GUIStyles.ButtonStyle, GUILayout.Height(40f)))
            {
                PendingCrestUnlock = false;
                PendingSettingsWindow = true;
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
        }

        public void DrawExtraSettingsUI()
        {
            if (!PendingSettingsWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = SettingsWidth;
            float h = SettingsHeight;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;

            // main texture
            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            Rect rect = new Rect(x, y, w, h);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            // footer texture
            float bottomH = 60f;
            float bottomY = y + h - bottomH;

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Rect bottomRect = new Rect(x, bottomY, w, bottomH);
            GUI.DrawTexture(bottomRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // rendering (use GUI.Box(rect, GUIContent.none) instead of DrawTexture if styling is also needed)
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(
                new Rect(
                    SettingsOuterOffset,
                    SettingsOuterOffset,
                    rect.width - SettingsInnerOffset,
                    rect.height - bottomH - SettingsInnerOffset));

            DrawExtraSettingsContent();
            CustomCursor.EndArea();

            CustomCursor.BeginArea(new Rect(0, rect.height - bottomH, rect.width, bottomH));
            DrawSettingsFooter();
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

            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

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

            GUI.color = new Color(0.8f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(20, 20, rect.width - 40, rect.height - 40));
            GUILayout.Label(_loc.Get("settings.title"), GUIStyles.LabelStyle);
            DrawPlayerStatusResetContent();
            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }

        public void DrawEnemyHealthBars()
        {
            var s = Bonfire.Instance.Status;

            if (!s.EnemyHealthBarsEnabled) return;

            if (Camera.main == null) return;

            foreach (var kvp in EnemyHealth.EnemyMaxHp)
            {
                HealthManager hm = kvp.Key;
                if (hm == null) continue;

                Vector3 screenPos = Camera.main.WorldToScreenPoint(hm.transform.position);
                if (screenPos.z < 0) continue;

                float maxHp = kvp.Value;

                float currentHp = hm.hp;
                // some bosses can temporarily reset their hp value on phase transition e.g. Cogwork Dancers set this
                // to 99999f. This checks prevents hp bar visual to jump up when that happens.
                if (currentHp >= EnemyHealth.HpResetSentinel)
                    continue;

                if (currentHp == maxHp) continue; // full HP = hide bars for normal enemies

                // screenPos above is raw value which isn't scaled with GUI.Matrix -> manual conversion is needed.
                float screenX = screenPos.x * BonfireGUI.ScaleX;
                float screenY = screenPos.y * BonfireGUI.ScaleY;

                Color fillColor;
                float width, height;
                float x, y;

                float pct = Mathf.Clamp01(currentHp / maxHp);

                if (s.EnemyHpDisplayNumbers)
                {
                    width = 240f;
                    height = 30f;
                }
                else
                {
                    width = 120f;
                    height = 15f;
                }

                x = screenX - width / 2f;
                y = BonfireGUI.Height - screenY - 100f;

                Rect container = new Rect(x, y, width, height);
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(container, SolidTex());

                if (s.EnemyHpBarColorProgression)
                    fillColor = pct > 0.6f ? _progHigh : pct > 0.3f ? _progMedium : _progLow;
                else
                    fillColor = _enemyColor;

                GUI.color = fillColor;
                GUI.DrawTexture(new Rect(x, y, width * pct, height), SolidTex());

                if (s.EnemyHpDisplayNumbers)
                {
                    GUI.color = Color.white;
                    GUILayout.BeginArea(container);
                    GUILayout.BeginVertical();
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{currentHp} / {maxHp}", GUIStyles.HpBarStyle);
                    GUILayout.FlexibleSpace();
                    GUILayout.EndVertical();
                    GUILayout.EndArea();
                }

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
                GUIStyles.HpLowStyle == null ||
                GUIStyles.HpMediumStyle == null ||
                GUIStyles.HpHighStyle == null)
                return;

            _enemyColor = GUIStyles.EnemyHpStyle.normal.textColor;
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
        private const float CrestUnlockHeight = 700f;

        private static bool _hpBarColorsInitialized;
        private static Color _enemyColor;
        private static Color _progLow;
        private static Color _progMedium;
        private static Color _progHigh;
        private static Texture2D _tex;

        private readonly string[] _crestNames = {
            "Hunter_v3", "Wanderer", "Reaper", "Warrior", "Witch", "Toolmaster", "Spell" };
        private readonly Dictionary<string, ToolCrest> _crestDict = new Dictionary<string, ToolCrest>();
        private bool _allCrestsUnlocked = false;

        private Vector2 _settingsScrollPos;
        private readonly Localization _loc = Localization.Loc;

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
            _settingsScrollPos = CustomCursor.BeginScrollView(_settingsScrollPos);

            DrawToggleSettings();
            GUILayout.Space(15);

            DrawCrestUnlockButton();
            GUILayout.Space(15);

            DrawPlayerStatusResetButton();
            GUILayout.Space(30);

            CustomCursor.EndScrollView();

            GUI.backgroundColor = Color.white;
        }

        private void DrawSettingsFooter()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (CustomCursor.Button(
                    _loc.Get("settings.footer.title"),
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(35f)))
            {
                PendingSettingsWindow = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        private void DrawGeneralUI()
        {
            var s = Bonfire.Instance.Status;

            // adjust step size
            GUILayout.Label(_loc.Get("settings.general.stepsize.text"));
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

            GUILayout.Space(10);

            s.DebugTeleport = CustomCursor.Toggle(
                s.DebugTeleport,
                _loc.Get("settings.general.debugteleport.text"),
                GUIStyles.WordWrapToggleStyle,
                GUILayout.Width(SettingsWidth - SettingsOuterOffset - SettingsInnerOffset - 50)
            );

            s.DebugLogs = CustomCursor.Toggle(
                s.DebugLogs,
                _loc.Get("settings.general.debuglogs.text",
                    ("debuglogsmaxmessages", LoggingOverlay.MaxMessages),
                    ("debuglogsmaxmessagesextended", LoggingOverlay.MaxMessagesExtended)),
                GUIStyles.WordWrapToggleStyle,
                GUILayout.Width(SettingsWidth - SettingsOuterOffset - SettingsInnerOffset - 50));
        }

        private void DrawCursorSettings()
        {
            var s = Bonfire.Instance.Status;

            GUI.enabled = GameManager.instance.inputHandler.gameController.IsAttached;

            GUILayout.Space(10);
            GUILayout.Label(_loc.Get("settings.cursor.text"), GUIStyles.SettingsHeader2);

            GUILayout.Label(
                _loc.Get("settings.cursor.speed.text",
                    ("cursorminspeed", CustomCursor.MinSpeed),
                    ("cursormaxspeed", CustomCursor.MaxSpeed)));

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
                _loc.Get("settings.cursor.display.text"));

            GUI.enabled = true;
        }

        private void DrawOverlaySettings()
        {
            var s = Bonfire.Instance.Status;

            int widthMax = (int)(BonfireGUI.Width - DamageOverlay.Width - DamageOverlay.BoxOffset);
            int heightMax = (int)(BonfireGUI.Height - DamageOverlay.Height - DamageOverlay.BoxOffset);

            GUILayout.Space(10);
            GUILayout.Label(_loc.Get("settings.overlay.componenets.title"), GUIStyles.SettingsHeader2);

            GUILayout.BeginHorizontal(GUILayout.Width(520));
            s.EnemyHealthBarsEnabled = CustomCursor.Toggle(
                s.EnemyHealthBarsEnabled,
                "Display enemy health bars");

            GUILayout.Label("(");
            GUILayout.Label(_loc.Get("settings.overlay.enemyhpbars.barcolor.text"), GUIStyles.EnemyHpStyle);
            GUILayout.Label(">");
            GUILayout.Label(_loc.Get("settings.overlay.enemyhpbars.barcolor.hotkeytext") + ")");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(490));
            GUILayout.Space(15);

            GUI.enabled = s.EnemyHealthBarsEnabled;
            s.EnemyHpBarColorProgression = CustomCursor.Toggle(
                s.EnemyHpBarColorProgression,
                _loc.Get("settings.overlay.enemyhpbars.barprogression.text"));

            GUILayout.Label("(");
            GUILayout.Label(_loc.Get("settings.overlay.enemyhpbars.barprogression.highhptext"), GUIStyles.HpHighStyle);
            GUILayout.Label("→");
            GUILayout.Label(_loc.Get("settings.overlay.enemyhpbars.barprogression.mediumhptext"),
                GUIStyles.HpMediumStyle);
            GUILayout.Label("→");
            GUILayout.Label(_loc.Get("settings.overlay.enemyhpbars.barprogression.lowhptext"), GUIStyles.HpLowStyle);
            GUILayout.Label(")");

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(15);

            s.EnemyHpDisplayNumbers = CustomCursor.Toggle(
                s.EnemyHpDisplayNumbers,
                _loc.Get("settings.overlay.enemyhpbars.shownumbers.text"));

            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUI.enabled = true;

            s.ShowCustomRangeIndicator = CustomCursor.Toggle(
                s.ShowCustomRangeIndicator,
                _loc.Get("settings.overlay.showrangeindicator.text"));
            GUILayout.Space(10);
            s.ShowDamageOverlay = CustomCursor.Toggle(
                s.ShowDamageOverlay,
                _loc.Get("settings.overlay.showdamageoverlay.text"),
                GUIStyles.WordWrapToggleStyle,
                GUILayout.Width(SettingsWidth - SettingsOuterOffset - SettingsInnerOffset - 50));

            GUI.enabled = s.ShowDamageOverlay;
            GUILayout.BeginHorizontal(GUILayout.Width(450));
            GUILayout.Space(10);
            GUILayout.Label(_loc.Get("settings.overlay.damageoverlay.xpositiontext"));
            GUILayout.Label(_loc.Get("settings.overlay.damageoverlay.ypositiontext"));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(GUILayout.Width(450));
            GUILayout.Space(10);
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
                _loc.Get("settings.fasttravel.text"),
                GUIStyles.WordWrapToggleStyle,
                GUILayout.Width(SettingsWidth - SettingsOuterOffset - SettingsInnerOffset - 50));
        }

        private void DrawGameplaySettings()
        {
            var s = Bonfire.Instance.Status;

            GUILayout.Space(10);
            GUILayout.Label(_loc.Get("settings.gameplay.deliveryquests.title"), GUIStyles.SettingsHeader2);

            s.DeliveryQuestsRemoveDamage = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveDamage,
                _loc.Get("settings.gameplay.deliveryquests.removepenaltytext"));

            s.DeliveryQuestsRemoveTimer = CustomCursor.Toggle(
                s.DeliveryQuestsRemoveTimer,
                _loc.Get("settings.gameplay.deliveryquests.removetimertext"));

            GUILayout.Space(10);
            GUILayout.Label("Other", GUIStyles.SettingsHeader2);

            s.ReduceFleaGamesScoreLimit = CustomCursor.Toggle(
                s.ReduceFleaGamesScoreLimit,
                _loc.Get("settings.gameplay.other.reducefleascoretext"));

            s.EasierSprintmasterRaces = CustomCursor.Toggle(
                s.EasierSprintmasterRaces,
                _loc.Get("settings.gameplay.other.easiersprintmasterracetext"));
        }

        private void DrawToggleSettings()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label(_loc.Get("settings.togglesettings.general"), GUIStyles.SettingsHeader);
            DrawGeneralUI();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label(_loc.Get("settings.togglesettings.overlay"), GUIStyles.SettingsHeader);
            DrawCursorSettings();
            GUILayout.Space(10);
            DrawOverlaySettings();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label(_loc.Get("settings.togglesettings.teleport"), GUIStyles.SettingsHeader);
            DrawFastTravelSettings();
            GUILayout.EndVertical();

            GUILayout.Space(20);

            GUILayout.BeginVertical("box");
            GUILayout.Label(_loc.Get("settings.togglesettings.gameplay"), GUIStyles.SettingsHeader);
            DrawGameplaySettings();
            GUILayout.EndVertical();
        }

        private void DrawCrestUnlockButton()
        {
            if (CustomCursor.Button(_loc.Get("settings.crestunlockbutton.text"), GUILayout.Height(30f)))
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
                $"{(_allCrestsUnlocked ? _loc.Get("settings.crestunlockconfirm.allunlockedtext") : "")}",
                GUIStyles.CrestUnlockLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.hunterv2"), "Hunter_v3");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.wanderer"), "Wanderer");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.reaper"), "Reaper");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.beast"), "Warrior");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.witch"), "Witch");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.architect"), "Toolmaster");
            CrestUnlockButton(_loc.Get("settings.crestunlockconfirm.shaman"), "Spell");

            // GUILayout.Space(30);
            // CrestUnlockButton("Unlock All Crests", "All");
            GUILayout.Space(40);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string currentCrest = PlayerData.instance.CurrentCrestID;
            GUI.enabled = _crestNames.Contains(currentCrest);

            if (CustomCursor.Button(
                _loc.Get("settings.crestunlockconfirm.memoryslotunlock"),
                GUIStyles.CrestUnlockButtonStyle,
                GUILayout.Width(300f),
                GUILayout.Height(50f)))
            {
                UnlockAllCrestSlots(currentCrest);
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(40);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Color closeOld = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);

            if (CustomCursor.Button(
                _loc.Get("settings.crestunlockconfirm.closewindow"),
                GUIStyles.CrestUnlockButtonStyle,
                GUILayout.Width(150f),
                GUILayout.Height(50f)))
            {
                Bonfire.Instance.SaveSettings();
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
            if (CustomCursor.Button(_loc.Get("settings.playerstatusreset.resetbutton"),
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
                _loc.Get("settings.playerstatusreset.content.text"),
                GUIStyles.CrestUnlockLabelStyle
            );
            GUILayout.Space(10);
            GUILayout.Label(
                _loc.Get("settings.playerstatusreset.content.text2", ("saveprofileid", GameManager.instance.profileID)),
                GUIStyles.CrestUnlockLabelStyle);

            GUILayout.Space(25);
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();
            if (CustomCursor.Button(
                    _loc.Get("settings.playerstatusreset.content.confirmbutton"),
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(200f),
                    GUILayout.Height(50f)))
            {
                PendingStatusReset = false;
                PendingSettingsWindow = true;

                // order is important here: remove all stored rosary data so respec can properly reset the fields
                // without refunding currency.
                ResetPlayerStatus();
                LevelingSystem.Instance.Respec(ignoreCost: true);

                Bonfire.Instance.SaveSettings();
            }

            GUILayout.Space(30);
            if (CustomCursor.Button(
                    _loc.Get("settings.playerstatusreset.content.cancelbutton"),
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
                    $"{displayName} {(unlocked ? " " + _loc.Get("settings.crestunlockbutton.alreadyunlocked") : "")}",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(320f),
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
            GameManager.instance.SaveGame(null);
        }

        private void UnlockAllCrestSlots(string name)
        {
            if (!_crestDict.Keys.Contains(name)) return;

            var crest = _crestDict[name];
            if (!crest.IsUnlocked) return;

            ToolCrest.SlotInfo[] s = crest.Slots;

            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].IsLocked)
                {
                    s[i].IsLocked = false;
                    Bonfire.Log.LogDebug("Unlocked slot number " + i);
                }
            }

            Bonfire.Log.LogDebug($"Crest {name}: Unlocked all {s.Length} slots.");
        }

        private void ResetPlayerStatus()
        {
            var s = Bonfire.Instance.Status;
            var newStatus = new PlayerStatus()
            {
                LevelingInfoStyle = s.LevelingInfoStyle,
                BenchMenuStyle = s.BenchMenuStyle,

                DebugTeleport = s.DebugTeleport,
                DebugLogs = s.DebugLogs,
                AdjustButtonStep = s.AdjustButtonStep,

                CustomCursorSpeed = s.CustomCursorSpeed,
                DisplayGameCursorWithGamepad = s.DisplayGameCursorWithGamepad,

                ShowCustomRangeIndicator = s.ShowCustomRangeIndicator,
                ShowDamageOverlay = s.ShowDamageOverlay,
                DamageOverlayX = s.DamageOverlayX,
                DamageOverlayY = s.DamageOverlayY,

                ResetMusicAfterTeleport = s.ResetMusicAfterTeleport,

                EnemyHealthBarsEnabled = s.EnemyHealthBarsEnabled,
                EnemyHpBarColorProgression = s.EnemyHpBarColorProgression,
                EnemyHpDisplayNumbers = s.EnemyHpDisplayNumbers,
                DeliveryQuestsRemoveTimer = s.DeliveryQuestsRemoveTimer,
                DeliveryQuestsRemoveDamage = s.DeliveryQuestsRemoveDamage,
                ReduceFleaGamesScoreLimit = s.ReduceFleaGamesScoreLimit,
                EasierSprintmasterRaces = s.EasierSprintmasterRaces,
            };

            Bonfire.Instance.Status = newStatus;
        }
    }
}