using System.Collections.Generic;

using UnityEngine;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Settings;

namespace BonfireSilksong.Leveling.UI
{
    /// <summary>
    /// Leveling system GUI.
    /// Namespace ends in 'UI' instead of 'GUI', this avoids naming collision with UnityEngine.GUI and allows the use 
    /// of 'GUI.*' instead of more verbose 'UnityEngine.GUI.*'
    /// </summary>
    public class LevelingGUI
    {
        public LevelingGUI()
        {
            _data = new GameData();
            _levelInfo = new LevelInfo(_data);
        }

        public void DrawLevelingUI()
        {
            CustomCursor.BeginArea(new Rect(
                Constants.LevelingAreaRectTopX,
                Constants.LevelingAreaRectTopY - 55,
                Constants.LevelingAreaDefaultRectWidth,
                50f));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string lvlStyleName = _levelingMenuStyle[Bonfire.Instance.Status.LevelingInfoStyle];

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button(
                $"Style: {lvlStyleName}",
                GUIStyles.ButtonStyle,
                GUILayout.Width(240f),
                GUILayout.Height(40f)))
            {
                ChangeLevelingMenuStyle();
            }

            GUILayout.Space(20);

            if (CustomCursor.Button(
                "Mod Info",
                GUIStyles.ButtonStyle,
                GUILayout.Width(240f),
                GUILayout.Height(40f)))
            {
                InfoWindow.PendingInfoWindow = true;
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            CustomCursor.EndArea();

            DrawUIPanels();
        }

        /// <summary>
        /// BeginArea GUI component for other buttons under leveling UI. Requires a manual EndArea() call for closing!
        /// </summary>
        public void DrawOtherUIBeginArea()
        {
            CustomCursor.BeginArea(new Rect(20f,
                Constants.LevelingAreaRectTopY
                + Constants.LevelingAreaDefaultRectHeight
                + Constants.LevelingAreaTextAndButtonsHeight,
                Constants.LevelingAreaDefaultRectWidth,
                200f));
        }


        private readonly GameData _data;
        private readonly LevelInfo _levelInfo;

        private Vector2 _infoScrollPos;

        // labels for leveling menu styles
        private readonly Dictionary<int, string> _levelingMenuStyle = new Dictionary<int, string>
        {
            {0, "Default"},
            {1, "Default + Info"},
            {2, "Simple"},
            {3, "Simple + Info"}
        };

        private void DrawDefaultUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.CheckStatLevels();
            _data.UpdateGUIFields();

            // layout
            CustomCursor.BeginArea(new Rect(
                Constants.LevelingAreaRectTopX,
                Constants.LevelingAreaRectTopY,
                Constants.LevelingAreaDefaultRectWidth,
                Constants.LevelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            Components.StatButton("Strength", _data.TotalStr, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Dexterity", _data.TotalDex, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Intelligence", _data.TotalInt, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Artifice", _data.TotalArt, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Resilience", _data.TotalRes, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Wisdom", _data.TotalWsdm, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButton("Luck", _data.TotalLuck, _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabel("Needle Damage: " + _data.NeedleBaseDamage);
            Components.StatLabel("Slash Speed Multiplier: " + _data.AttackSpeed);
            Components.StatLabel("Silk Skill Damage Bonus: " + _data.SpellDamageBonus);
            Components.StatLabel("Tool Damage Bonus: " + _data.ToolDamageBonus);
            Components.StatLabel("Lifeblood Masks: " + _data.ExtraMasks);
            Components.StatLabel($"Gain Silk After {_data.ExtraSilkOnHit} Hits");
            Components.StatLabel("Crit Chance: " + _data.CritChance);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabel("");   // spacer to align with column 1, because Str doesn't have a second modifier
            Components.StatLabel("Crit Damage Bonus: " + _data.CritDamageBonus);
            Components.StatLabel("Bind Cost: " + _data.BindCost);
            Components.StatLabel("Tool Regen Interval: " + _data.ToolRegenInterval);
            Components.StatLabel("Block Chance: " + _data.BlockChance);
            Components.StatLabel("Extra Silk in Spool: " + _data.ExtraSilkTotal);
            Components.StatLabel("Extra Currency From Enemies: " + _data.RosaryCountIncrease);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            CustomCursor.EndArea();

            // level and cost text + apply/cancel/respec buttons
            CustomCursor.BeginArea(new Rect(
                Constants.LevelingAreaRectTopX,
                Constants.LevelingAreaRectTopY + Constants.LevelingAreaDefaultRectHeight,
                Constants.LevelingAreaDefaultRectWidth,
                Constants.LevelingAreaTextAndButtonsHeight));

            GUILayout.Label("Current Level: " + _data.CurrentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _data.RosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (CustomCursor.Button(
                    _data.ApplyText,
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.ApplyCancelButtonsWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button(
                    "Cancel",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.ApplyCancelButtonsWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            int r = s.RespecCost;
            if (CustomCursor.Button(
                    $"Respec" + $"{(r <= 0 ? "" : $" ({r} {(r > 1 ? $"Silkeaters" : "Silkeater")})")}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.LevelingAreaDefaultRectWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            CustomCursor.EndArea();
        }

        private void DrawSimpleUI()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            ls.CheckStatLevels();
            _data.UpdateGUIFields();

            // layout
            CustomCursor.BeginArea(new Rect(
                Constants.LevelingAreaRectTopX,
                Constants.LevelingAreaRectTopY,
                Constants.LevelingAreaDefaultRectWidth,
                Constants.LevelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            Components.StatButtonExtended("Strength", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Dexterity", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Intelligence", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Artifice", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Resilience", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Wisdom", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            Components.StatButtonExtended("Luck", _data.GotFreeLevel, s, _data.RosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabelExtended($"{s.StrengthStat}");
            Components.StatLabelExtended($"{s.DexterityStat}");
            Components.StatLabelExtended($"{s.IntelligenceStat}");
            Components.StatLabelExtended($"{s.ArtificeStat}");
            Components.StatLabelExtended($"{s.ResilienceStat}");
            Components.StatLabelExtended($"{s.WisdomStat}");
            Components.StatLabelExtended($"{s.LuckStat}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            Components.StatLabelExtended("→");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabelExtended($"{(s.StrengthStat + ls.StrengthIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.DexterityStat + ls.DexterityIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.IntelligenceStat + ls.IntelligenceIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.ArtificeStat + ls.ArtificeIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.ResilienceStat + ls.ResilienceIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.WisdomStat + ls.WisdomIncrease).ToString()}");
            Components.StatLabelExtended($"{(s.LuckStat + ls.LuckIncrease).ToString()}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            Components.StatLabelExtended(_data.NeedleBaseDamage);
            Components.StatLabelExtended($"{_data.AttackSpeed}\n{_data.CritDamageBonus}");
            Components.StatLabelExtended($"{_data.SpellDamageBonus}\n{_data.BindCost}");
            Components.StatLabelExtended($"{_data.ToolDamageBonus}\n{_data.ToolRegenInterval}");
            Components.StatLabelExtended($"{_data.ExtraMasks}\n{_data.BlockChance}");
            Components.StatLabelExtended($"{_data.ExtraSilkOnHit}\n{_data.ExtraSilkTotal}");
            Components.StatLabelExtended($"{_data.CritChance}\n{_data.RosaryCountIncrease}");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            CustomCursor.EndArea();

            // level and cost text + apply/cancel/respec buttons
            CustomCursor.BeginArea(new Rect(
                Constants.LevelingAreaRectTopX,
                Constants.LevelingAreaRectTopY + Constants.LevelingAreaDefaultRectHeight,
                Constants.LevelingAreaDefaultRectWidth,
                150f));

            GUILayout.Label("Current Level: " + _data.CurrentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _data.RosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (CustomCursor.Button(
                    _data.ApplyText,
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.ApplyCancelButtonsWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button(
                    "Cancel",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.ApplyCancelButtonsWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            int r = s.RespecCost;
            if (CustomCursor.Button(
                    $"Respec" + $"{(r <= 0 ? "" : $" ({r} {(r > 1 ? $"Silkeaters" : "Silkeater")})")}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(Constants.LevelingAreaDefaultRectWidth),
                    GUILayout.Height(Constants.LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.Respec();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            CustomCursor.EndArea();
        }

        private void DrawExtendedStatPanel()
        {
            GUI.color = new Color(0.8f, 0.7f, 0.3f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    Constants.InfoAreaRectTopX - 4, Constants.InfoAreaRectTopY - 4,
                    Constants.InfoAreaRectWidth + 8, Constants.InfoAreaRectHeight + 8),
                    Texture2D.whiteTexture);

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    Constants.InfoAreaRectTopX, Constants.InfoAreaRectTopY,
                    Constants.InfoAreaRectWidth, Constants.InfoAreaRectHeight),
                    Texture2D.whiteTexture);
            GUI.color = Color.white;

            CustomCursor.BeginArea(new Rect(
                Constants.InfoAreaRectTopX,
                Constants.InfoAreaRectTopY,
                Constants.InfoAreaRectWidth,
                Constants.InfoAreaRectHeight));

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            if (BonfireGUI.NoWindowsPending)
            {
                _infoScrollPos = CustomCursor.BeginScrollView(_infoScrollPos);
                _levelInfo.Draw(_infoScrollPos.y);
                CustomCursor.EndScrollView();
            }

            CustomCursor.EndArea();

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            GUI.enabled = true;
        }

        private void DrawUIPanels()
        {
            var s = Bonfire.Instance.Status.LevelingInfoStyle;

            switch (s)
            {
                case 0:
                    DrawDefaultUI();
                    return;
                case 1:
                    DrawDefaultUI();
                    DrawExtendedStatPanel();
                    return;
                case 2:
                    DrawSimpleUI();
                    return;
                case 3:
                    DrawSimpleUI();
                    DrawExtendedStatPanel();
                    return;
            }
        }

        private void ChangeLevelingMenuStyle()
        {
            int s = Bonfire.Instance.Status.LevelingInfoStyle + 1;
            if (s >= 4 || s < 0)
                s = 0;

            Bonfire.Instance.Status.LevelingInfoStyle = s;
        }

        private string GetWeaverAuraString(WeaverAura state)
        {
            switch (state)
            {
                case WeaverAura.AURA: return "ENABLED";
                case WeaverAura.BURST: return "VOID BURST";
                default: return "DISABLED";
            }
        }

        private WeaverAura RotateWeaverAuraState(WeaverAura state)
        {
            if (IntelligenceStat.CheckIntelligenceMilestone(4))
            {
                switch (state)
                {
                    case WeaverAura.AURA: return WeaverAura.BURST;
                    case WeaverAura.BURST: return WeaverAura.DISABLED;
                    case WeaverAura.DISABLED: return WeaverAura.AURA;
                    default: return WeaverAura.DISABLED;
                }
            }

            switch (state)
            {
                case WeaverAura.AURA: return WeaverAura.DISABLED;
                case WeaverAura.DISABLED: return WeaverAura.AURA;
                default: return WeaverAura.DISABLED;
            }
        }

        private string GetBlueStateString(BlueState state)
        {
            switch (state)
            {
                case BlueState.REFLECT: return "VENGEFUL";
                case BlueState.GLASSCANNON: return "HOLLOW";
                default: return "DISABLED";
            }
        }

        private BlueState RotateBlueState(BlueState state)
        {
            switch (state)
            {
                case BlueState.REFLECT: return BlueState.GLASSCANNON;
                case BlueState.GLASSCANNON: return BlueState.DISABLED;
                case BlueState.DISABLED: return BlueState.REFLECT;
                default: return BlueState.DISABLED;
            }
        }
    }
}