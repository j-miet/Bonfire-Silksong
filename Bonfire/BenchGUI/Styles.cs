using UnityEngine;

namespace BonfireSilksong
{
    // GUI fonts and styles
    public static class Styles
    {
        public static Font _trajanBold;
        public static Font _trajanNormal;
        public static GUIStyle _levelHeaderStyle;
        public static GUIStyle _labelStyle;
        public static GUIStyle _buttonStyle;
        public static GUIStyle _crestUnlockLabelStyle;
        public static GUIStyle _crestUnlockButtonStyle;
        public static GUIStyle _fastTravelUnlockLabelStyle;
        public static GUIStyle _benchListAreaLabelStyle;
        public static GUIStyle _benchListButtonStyle;
        public static GUIStyle _benchListButtonSelectedStyle;
        public static GUIStyle _fastTravelConfirmHeaderStyle;
        public static GUIStyle _fastTravelConfirmDestinationStyle;

        public static void EnsureFonts()
        {
            if (_trajanBold != null && _trajanNormal != null) return;

            foreach (Font font in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (font == null) continue;
                if (font.name == "TrajanPro-Bold") _trajanBold = font;
                if (font.name == "TrajanPro-Regular") _trajanNormal = font;
            }
        }

        public static void EnsureStyles()
        {
            if (_levelHeaderStyle == null)
            {
                _levelHeaderStyle = new GUIStyle(GUI.skin.label)
                {
                    font = _trajanNormal,
                    fontStyle = FontStyle.Bold,
                    fontSize = 17,
                    alignment = TextAnchor.MiddleCenter,
                };
            }

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
    }
}