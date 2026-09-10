using UnityEngine;

namespace BonfireSilksong.GUIComponents
{
    public class DamageOverlay
    {
        public const float Width = 250f;
        public const float Height = 70f;
        public const float BoxOffset = 5f;

        public static void Display(float x = TopLeftX, float y = TopLeftY)
        {
            if (!_colorsInitialized)
            {
                _textColor = new Color(1f, 1f, 1f);
                _bgColor = new Color(0.2f, 0.2f, 0.2f);
                _borderColor = new Color(0.5f, 0.5f, 0.5f);

                _colorsInitialized = true;
            }

            GUI.color = _borderColor;
            GUI.DrawTexture(
                new Rect(x - BoxOffset, y - BoxOffset, Width + BoxOffsetDoubled, Height + BoxOffsetDoubled),
                Texture2D.whiteTexture);

            GUI.color = _bgColor;
            GUI.DrawTexture(
                new Rect(x, y, Width, Height),
                Texture2D.whiteTexture);

            GUI.color = _textColor;
            GUILayout.BeginArea(new Rect(x, y, Width, Height));
            GUILayout.BeginVertical();

            GUILayout.Label($"  Source: {_dpsData.source ?? "-"}");
            GUILayout.Label($"  Tool/Skill: {_dpsData.tool ?? "-"}");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"  Vanilla: {_dpsData.damageBase}");
            GUILayout.Space(15);
            GUILayout.Label($"  Modded: {_dpsData.damageUpdated}");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        public static void UpdateData(string source, string tool, int damage, int damageUpdated)
        {
            _dpsData.source = source;
            _dpsData.tool = tool;
            _dpsData.damageBase = damage;
            _dpsData.damageUpdated = damageUpdated;
        }


        private const float TopLeftX = 200f;
        private const float TopLeftY = 5f;
        private const float BoxOffsetDoubled = BoxOffset * 2;

        private static (string source, string tool, int damageBase, int damageUpdated) _dpsData;
        private static bool _colorsInitialized = false;
        private static Color _textColor;
        private static Color _bgColor;
        private static Color _borderColor;
    }
}