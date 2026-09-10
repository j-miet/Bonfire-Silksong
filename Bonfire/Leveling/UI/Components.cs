using UnityEngine;

using BonfireSilksong.GUIComponents;

namespace BonfireSilksong.Leveling.UI
{
    public static class Components
    {
        public static void StatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
            int rosariesToLvlUp)
        {
            if (CustomCursor.Button(
                    $"{statName}: {displayValue}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        public static void StatLabel(string text)
        {
            GUILayout.Label(
                text,
                GUIStyles.LabelStyle,
                GUILayout.Width(185f),
                GUILayout.Height(Constants.LevelingAreaButtonHeight));
        }

        public static void StatButtonExtended(string statName, bool gotFreeLevel, PlayerStatus s, int rosariesToLvlUp)
        {
            if (CustomCursor.Button(
                    $"{statName}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        public static void StatLabelExtended(string text)
        {
            GUILayout.Label(
                text,
                GUIStyles.LabelStyle,
                GUILayout.Width(75f),
                GUILayout.Height(Constants.LevelingAreaButtonHeight));
        }

        public static void InfoStatLabel(string text)
        {
            GUILayout.Label(
                text,
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(Constants.BoxWidth),
                GUILayout.Height(Constants.LevelingAreaButtonHeight));
        }
    }
}