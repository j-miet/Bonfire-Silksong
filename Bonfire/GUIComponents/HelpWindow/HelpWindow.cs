using UnityEngine;

namespace BonfireSilksong.GUIComponents
{
    /// <summary>
    /// Displays help/info overlay.
    /// </summary>
    public class HelpWindow
    {
        public static bool PendingHelpWindow = false;

        public static void Display()
        {
            if (!PendingHelpWindow) return;

            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(0, 0, BonfireGUI.Width, BonfireGUI.Height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = HelpWindowWidth;
            float h = HelpWindowHeight;
            float x = BonfireGUI.Width / 2f - w / 2f;
            float y = BonfireGUI.Height / 2f - h / 2f;

            // main texture
            Rect borderRect = new Rect(x - 4, y - 4, w + 8, h + 8);
            GUI.color = Color.gray;
            GUI.DrawTexture(borderRect, Texture2D.whiteTexture);

            Rect rect = new Rect(x, y, w, h);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // footer texture
            float bottomH = 60f;
            float bottomY = y + h - bottomH;

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            Rect bottomRect = new Rect(x, bottomY, w, bottomH);
            GUI.DrawTexture(bottomRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // rendering
            CustomCursor.BeginGroup(rect);

            CustomCursor.BeginArea(new Rect(
                HelpInnerOffset,
                HelpInnerOffset,
                rect.width - HelpOuterOffset,
                rect.height - bottomH - HelpOuterOffset));

            DrawHelpContent();
            CustomCursor.EndArea();

            CustomCursor.BeginArea(new Rect(0, rect.height - bottomH, rect.width, bottomH));
            DrawHelpFooter();
            CustomCursor.EndArea();

            CustomCursor.EndGroup();
        }


        private const float HelpWindowWidth = 650f;
        private const float HelpWindowHeight = 700f;
        private const float HelpInnerOffset = 10f;
        private const float HelpOuterOffset = 2 * HelpInnerOffset;

        private static Vector2 _helpWindowScrollPos;

        private static void DrawHelpContent()
        {
            var Loc = Localization.Loc;

            _helpWindowScrollPos = CustomCursor.BeginScrollView(_helpWindowScrollPos);

            GUILayout.BeginVertical("box");
            Color previous = GUI.color;
            GUI.color = new Color(1f, 0.7f, 0.2f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                Loc.Get("help.title", ("modversionnumber", Bonfire.ModVersion)),
                GUIStyles.BenchListAreaLabelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUI.color = previous;

            GUILayout.Label(Loc.Get("help.text"));
            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.guihotkeys.title"), GUIStyles.SettingsHeader2);
            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.guihotkeys.general.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.guihotkeys.general.text"));

            GUILayout.Label(Loc.Get("help.guihotkeys.benchteleport.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.guihotkeys.benchteleport.text"));

            GUILayout.Label(Loc.Get("help.guihotkeys.gamepad.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.guihotkeys.gamepad.text"));

            GUILayout.Space(10);
            GUILayout.Label(Loc.Get("help.goodtoknow.title"), GUIStyles.SettingsHeader2);
            GUILayout.Space(10);
            GUILayout.Label(Loc.Get("help.goodtoknow.text"));

            GUILayout.Space(25);

            GUILayout.Label(Loc.Get("help.leveling.title"), GUIStyles.SettingsHeader);

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.leveling.general.title"), GUIStyles.SettingsHeader2);
            GUILayout.Label(Loc.Get("help.leveling.general.text"));

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.leveling.stats.title"), GUIStyles.SettingsHeader2);

            GUILayout.Label(Loc.Get("help.leveling.stats.globalbuffs.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.leveling.stats.globalbuffs.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.currencymultiplier.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.currencymultiplier.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.attributes.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.leveling.stats.attributes.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.primary.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.primary.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.intelligence1.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.intelligence1.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.resilience1.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.resilience1.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.wisdom1.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.wisdom1.text"));

            GUILayout.Label(Loc.Get("help.leveling.stats.luck1.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.stats.luck1.text"));

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.leveling.milestones.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.leveling.milestones.text1"));

            GUILayout.Label(Loc.Get("help.leveling.milestones.text2"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.strength.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.strength.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.dexterity.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.dexterity.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.intelligence.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.intelligence.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.artifice.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.artifice.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.resilience.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.resilience.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.wisdom.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.wisdom.text"));
            GUILayout.Label(Loc.Get("help.leveling.milestones.luck.title"), GUIStyles.SettingsHeader4);
            GUILayout.Label(Loc.Get("help.leveling.milestones.luck.text"));

            GUILayout.EndVertical();

            GUILayout.Space(15);

            GUILayout.BeginVertical("box");
            GUILayout.Label(Loc.Get("help.teleport.title"), GUIStyles.SettingsHeader);
            GUILayout.Label(Loc.Get("help.teleport.text"));

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.teleport.unlock.title"), GUIStyles.SettingsHeader2);
            GUILayout.Label(Loc.Get("help.teleport.unlock.text"));

            GUILayout.Label(Loc.Get("help.teleport.unlock.usage.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.teleport.unlock.usage.text"));

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.teleport.extradetails.title"), GUIStyles.SettingsHeader2);

            GUILayout.Label(Loc.Get("help.teleport.extradetails.mapscreen.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.teleport.extradetails.mapscreen.text"));

            GUILayout.Label(Loc.Get("help.teleport.extradetails.styles.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.teleport.extradetails.styles.text"));

            GUILayout.Label(Loc.Get("help.teleport.extradetails.benchunlocking.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.teleport.extradetails.benchunlocking.text"));

            GUILayout.Label(Loc.Get("help.teleport.extradetails.benchavailability.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.teleport.extradetails.benchavailability.text"));

            GUILayout.EndVertical();

            GUILayout.Space(15);

            GUILayout.BeginVertical("box");
            GUILayout.Label(Loc.Get("help.settings.title"), GUIStyles.SettingsHeader);
            GUILayout.Label(Loc.Get("help.settings.text"));

            GUILayout.Space(10);

            GUILayout.Label(Loc.Get("help.settings.customcursor.title"), GUIStyles.SettingsHeader2);
            GUILayout.Label(Loc.Get("help.settings.customcursor.text"));

            GUILayout.Label(Loc.Get("help.settings.debug.title"), GUIStyles.SettingsHeader2);
            GUILayout.Label(Loc.Get("help.settings.debug.text"));

            GUILayout.Label(Loc.Get("help.settings.crestunlock.title"), GUIStyles.SettingsHeader2);
            GUILayout.Label(Loc.Get("help.settings.crestunlock.text"));
            GUILayout.Label(Loc.Get("help.settings.crestunlock.chapelaccess.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.settings.crestunlock.chapelaccess.text"));

            GUILayout.Label(Loc.Get("help.settings.crestunlock.slots.title"), GUIStyles.SettingsHeader3);
            GUILayout.Label(Loc.Get("help.settings.crestunlock.slots.text"));

            GUILayout.EndVertical();

            CustomCursor.EndScrollView();
        }

        private static void DrawHelpFooter()
        {
            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (CustomCursor.Button(
                    "Close window",
                    GUIStyles.CrestUnlockButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(35f)))
            {
                PendingHelpWindow = false;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }
    }
}