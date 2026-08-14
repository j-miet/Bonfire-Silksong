using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// GUI fonts and styles.
    /// </summary>
    public static class GUIStyles
    {
        public static Font TrajanBold;
        public static Font TrajanNormal;

        public static GUIStyle LevelHeaderStyle;
        public static GUIStyle LabelStyle;
        public static GUIStyle ButtonStyle;

        public static GUIStyle EnemyHpStyle;
        public static GUIStyle BossHpStyle;
        public static GUIStyle HpHighStyle;
        public static GUIStyle HpMediumStyle;
        public static GUIStyle HpLowStyle;

        public static GUIStyle CrestUnlockLabelStyle;
        public static GUIStyle CrestUnlockButtonStyle;

        public static GUIStyle FastTravelUnlockLabelStyle;

        public static GUIStyle BenchListAreaLabelStyle;
        public static GUIStyle BenchListButtonStyle;
        public static GUIStyle BenchListButtonSelectedStyle;
        public static GUIStyle FastTravelConfirmHeaderStyle;
        public static GUIStyle FastTravelConfirmDestinationStyle;

        public static GUIStyle LvlInfoHeaderPrimaryStyle;
        public static GUIStyle LvlInfoHeaderSecondaryStyle;
        public static GUIStyle LvlInfoHeaderMilestoneStyle;
        public static GUIStyle LvlInfoLabelStyle;
        public static GUIStyle LvlInfoButtonStyle;
        public static GUIStyle LvlInfoLabelDisabledStyle;

        public static void EnsureFonts()
        {
            if (TrajanBold != null && TrajanNormal != null) return;

            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font == null) continue;
                if (font.name == "TrajanPro-Bold") TrajanBold = font;
                if (font.name == "TrajanPro-Regular") TrajanNormal = font;
            }
        }

        public static void EnsureStyles()
        {
            if (LevelHeaderStyle == null)
            {
                LevelHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 17,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (LabelStyle == null)
            {
                LabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (ButtonStyle == null)
            {
                ButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = TrajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (EnemyHpStyle == null)
            {
                EnemyHpStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.red }
                };
            }

            if (BossHpStyle == null)
            {
                BossHpStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = new Color(1f, 0.5f, 0f) }
                };
            }

            if (HpHighStyle == null)
            {
                HpHighStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.green }
                };
            }

            if (HpMediumStyle == null)
            {
                HpMediumStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.yellow }
                };
            }

            if (HpLowStyle == null)
            {
                HpLowStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.red }
                };
            }

            if (CrestUnlockLabelStyle == null)
            {
                CrestUnlockLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanBold,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
            }

            if (CrestUnlockButtonStyle == null)
            {
                CrestUnlockButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = TrajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (FastTravelUnlockLabelStyle == null)
            {
                FastTravelUnlockLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanBold,
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };
            }

            if (BenchListAreaLabelStyle == null)
            {
                BenchListAreaLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanBold,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleLeft,
                };
            }

            if (BenchListButtonStyle == null)
            {
                BenchListButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = TrajanBold,
                    fontSize = 14,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(10, 10, 5, 5),
                };
            }

            if (BenchListButtonSelectedStyle == null)
            {
                BenchListButtonSelectedStyle = new GUIStyle(GUI.skin.button)
                {
                    font = TrajanBold,
                    fontStyle = FontStyle.Normal,
                    fontSize = 15,
                    alignment = TextAnchor.MiddleLeft,
                };
                BenchListButtonSelectedStyle.normal.background = Texture2D.whiteTexture;
            }

            if (FastTravelConfirmHeaderStyle == null)
                FastTravelConfirmHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontSize = 11,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.7f, 0.3f) }
                };

            if (FastTravelConfirmDestinationStyle == null)
                FastTravelConfirmDestinationStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanBold,
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };

            if (LvlInfoHeaderPrimaryStyle == null)
            {
                LvlInfoHeaderPrimaryStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 17,
                    normal = { textColor = new Color(0.8f, 0.7f, 0.3f) }
                };
            }

            if (LvlInfoHeaderSecondaryStyle == null)
            {
                LvlInfoHeaderSecondaryStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 17,
                    normal = { textColor = new Color(0.35f, 0.85f, 0.6f) }
                };
            }

            if (LvlInfoHeaderMilestoneStyle == null)
            {
                LvlInfoHeaderMilestoneStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 17,
                    normal = { textColor = new Color(0.1f, 0.4f, 0.8f) }
                };
            }

            if (LvlInfoLabelStyle == null)
            {
                LvlInfoLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                };
            }

            if (LvlInfoButtonStyle == null)
            {
                LvlInfoButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                };
                LvlInfoButtonStyle.normal.background = Texture2D.whiteTexture;
            }

            if (LvlInfoLabelDisabledStyle == null)
            {
                LvlInfoLabelDisabledStyle = new GUIStyle(GUI.skin.label)
                {
                    font = TrajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 14,
                    normal = { textColor = Color.gray },
                };
            }
        }
    }
}