using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system GUI.
    /// </summary>
    public class LevelingGUI
    {
        public void DrawLevelingUI()
        {
            CustomCursor.BeginArea(new Rect(
                LevelingAreaRectTopX,
                LevelingAreaRectTopY - 55,
                LevelingAreaDefaultRectWidth,
                50f));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            string lvlStyleName = _levelingMenuStyle[Bonfire.Instance.Status.LevelingInfoStyle];

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button(
                $"Style: {lvlStyleName}",
                GUIStyles.ButtonStyle,
                GUILayout.Width(250f),
                GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ChangeLevelingMenuStyle();
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
                LevelingAreaRectTopY + LevelingAreaDefaultRectHeight + LevelingAreaTextAndButtonsHeight,
                LevelingAreaDefaultRectWidth,
                200f));
        }


        // user interface measures
        private const float LevelingAreaRectTopX = 20f;
        private const float LevelingAreaRectTopY = 275f;
        private const float LevelingAreaDefaultRectWidth = 560f;
        private const float LevelingAreaDefaultRectHeight = 350f;
        private const float LevelingAreaTextAndButtonsHeight = 155f;
        private const float LevelingAreaButtonHeight = 40;
        private const float ApplyCancelButtonsWidth = LevelingAreaDefaultRectWidth / 2 - 15;

        private const float InfoAreaRectTopX = 600f;
        private const float InfoAreaRectTopY = 130f;
        private const float InfoAreaRectWidth = 1200f;
        private const float InfoAreaRectHeight = 885f;
        // offset of 22f seems to be just enough to prevent the horizontal scroll bar from appearing while also
        // preventing extra unused area between right box cell and vertical scroll bar from getting generated.
        private const float ColWidth = InfoAreaRectWidth / 3f - 22f;

        private const float FirstRowHeight = 820f;
        private const float SecondRowHeight = 700f;
        private const float ThirdRowHeight = 400f;

        private Vector2 _infoScrollPos;

        // shared variables
        private int _currentLvl;
        private int _rosaryLevel;
        private int _rosariesToLvlUp;
        private int _availableRelicLevels;
        private bool _gotFreeLevel;

        private string _totalStr;
        private string _totalDex;
        private string _totalInt;
        private string _totalArt;
        private string _totalRes;
        private string _totalWsdm;
        private string _totalLuck;

        private string _needleBaseDamage;

        private string _attackSpeed;
        private string _critDamageBonus;

        private string _spellDamageBonus;
        private string _bindCost;
        private string _tauntDamage;
        private string _tauntRange;

        private string _toolDamageBonus;
        private string _toolRegenInterval;

        private string _extraMasks;
        private string _blockChance;
        private string _reflectDamage;
        private string _reflectArea;
        private string _glassCannonMaskBonus;
        private string _glassCannonMilestoneBonus;


        private string _extraSilk;
        private string _silkRegenInterval;

        private string _critChance;
        private string _rosaryIncrease;

        private string _applyText;

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
            UpdateGUIFields();

            // layout
            CustomCursor.BeginArea(new Rect(
                LevelingAreaRectTopX,
                LevelingAreaRectTopY,
                LevelingAreaDefaultRectWidth,
                LevelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButton("Strength", _totalStr, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Dexterity", _totalDex, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Intelligence", _totalInt, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Artifice", _totalArt, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Resilience", _totalRes, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Wisdom", _totalWsdm, _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButton("Luck", _totalLuck, _gotFreeLevel, s, _rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("Needle Damage: " + _needleBaseDamage);
            DrawStatLabel("Slash Speed Multiplier: " + _attackSpeed);
            DrawStatLabel("Silk Skill Damage Bonus: " + _spellDamageBonus);
            DrawStatLabel("Tool Damage Bonus: " + _toolDamageBonus);
            DrawStatLabel("Lifeblood Masks: " + _extraMasks);
            DrawStatLabel("Silk Gained On Enemy Hit: " + _extraSilk);
            DrawStatLabel("Crit Chance: " + _critChance);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabel("");   // spacer to align with column 1, because Str doesn't have a second modifier
            DrawStatLabel("Crit Damage Bonus: " + _critDamageBonus);
            DrawStatLabel("Binding Cost: " + _bindCost);
            DrawStatLabel("Tool Regen Interval: " + _toolRegenInterval);
            DrawStatLabel("Block Chance: " + _blockChance);
            DrawStatLabel("Silk Regen Interval: " + _silkRegenInterval);
            DrawStatLabel("Rosary & Shard Increase: " + _rosaryIncrease);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            CustomCursor.EndArea();

            // level and cost text + apply/cancel/respec buttons
            CustomCursor.BeginArea(new Rect(
                LevelingAreaRectTopX,
                LevelingAreaRectTopY + LevelingAreaDefaultRectHeight,
                LevelingAreaDefaultRectWidth,
                LevelingAreaTextAndButtonsHeight));

            GUILayout.Label("Current Level: " + _currentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _rosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (CustomCursor.Button(
                _applyText,
                GUIStyles.ButtonStyle,
                GUILayout.Width(ApplyCancelButtonsWidth),
                GUILayout.Height(LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button(
                "Cancel",
                GUIStyles.ButtonStyle,
                GUILayout.Width(ApplyCancelButtonsWidth),
                GUILayout.Height(LevelingAreaButtonHeight))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (CustomCursor.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(LevelingAreaDefaultRectWidth),
                    GUILayout.Height(LevelingAreaButtonHeight))
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
            UpdateGUIFields();

            // layout
            CustomCursor.BeginArea(new Rect(
                LevelingAreaRectTopX,
                LevelingAreaRectTopY,
                LevelingAreaDefaultRectWidth,
                LevelingAreaDefaultRectHeight));
            GUILayout.Label("Leveling", GUIStyles.LevelHeaderStyle);

            // stats
            GUILayout.BeginHorizontal("box");

            GUILayout.BeginVertical("box");
            DrawStatButtonExtended("Strength", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Dexterity", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Intelligence", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Artifice", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Resilience", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Wisdom", _gotFreeLevel, s, _rosariesToLvlUp);
            DrawStatButtonExtended("Luck", _gotFreeLevel, s, _rosariesToLvlUp);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended($"{s.StrengthStat}");
            DrawStatLabelExtended($"{s.DexterityStat}");
            DrawStatLabelExtended($"{s.IntelligenceStat}");
            DrawStatLabelExtended($"{s.ArtificeStat}");
            DrawStatLabelExtended($"{s.ResilienceStat}");
            DrawStatLabelExtended($"{s.WisdomStat}");
            DrawStatLabelExtended($"{s.LuckStat}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            DrawStatLabelExtended("→");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended($"{(s.StrengthStat + ls.StrengthIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.DexterityStat + ls.DexterityIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.IntelligenceStat + ls.IntelligenceIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.ArtificeStat + ls.ArtificeIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.ResilienceStat + ls.ResilienceIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.WisdomStat + ls.WisdomIncrease).ToString()}");
            DrawStatLabelExtended($"{(s.LuckStat + ls.LuckIncrease).ToString()}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
            DrawStatLabelExtended(_needleBaseDamage);
            DrawStatLabelExtended($"{_attackSpeed}\n{_critDamageBonus}");
            DrawStatLabelExtended($"{_spellDamageBonus}\n{_bindCost}");
            DrawStatLabelExtended($"{_toolDamageBonus}\n{_toolRegenInterval}");
            DrawStatLabelExtended($"{_extraMasks}\n{_blockChance}");
            DrawStatLabelExtended($"{_extraSilk}\n{_silkRegenInterval}");
            DrawStatLabelExtended($"{_critChance}\n{_rosaryIncrease}");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            CustomCursor.EndArea();

            // level and cost text + apply/cancel/respec buttons
            CustomCursor.BeginArea(new Rect(
                LevelingAreaRectTopX,
                LevelingAreaRectTopY + LevelingAreaDefaultRectHeight,
                LevelingAreaDefaultRectWidth,
                150f));

            GUILayout.Label("Current Level: " + _currentLvl, GUIStyles.LabelStyle);
            GUILayout.Label("Rosaries to Level Up: " + _rosariesToLvlUp, GUIStyles.LabelStyle);

            GUILayout.BeginHorizontal("box");

            GUI.backgroundColor = Color.green;
            if (CustomCursor.Button(_applyText, GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ApplyLevel();
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.white;
            if (CustomCursor.Button("Cancel", GUIStyles.ButtonStyle, GUILayout.Width(258f), GUILayout.Height(40f))
                && PlayerData.instance.atBench)
            {
                ls.ResetPendingLevelUp();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.red;
            if (CustomCursor.Button(
                    $"Respec ({s.RespecCost} " + $"{(s.RespecCost > 1 ? "Silkeaters" : "Silkeater")})",
                    GUIStyles.ButtonStyle,
                    GUILayout.Height(40f),
                    GUILayout.Width(522f))
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
                    InfoAreaRectTopX - 4, InfoAreaRectTopY - 4,
                    InfoAreaRectWidth + 8, InfoAreaRectHeight + 12),
                    Texture2D.whiteTexture);

            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    InfoAreaRectTopX, InfoAreaRectTopY,
                    InfoAreaRectWidth, InfoAreaRectHeight),
                    Texture2D.whiteTexture);
            GUI.color = Color.white;

            CustomCursor.BeginArea(new Rect(
                InfoAreaRectTopX,
                InfoAreaRectTopY,
                InfoAreaRectWidth,
                InfoAreaRectHeight));

            GUI.color = Color.white;
            GUI.backgroundColor = Color.gray;
            if (!SettingsGUI.PendingSettingsWindow &&
                !SettingsGUI.PendingCrestUnlock &&
                !SettingsGUI.PendingStatusReset)
            {
                _infoScrollPos = GUILayout.BeginScrollView(_infoScrollPos);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.BeginHorizontal("box");
            DrawStrengthInfo(FirstRowHeight);
            DrawDexterityInfo(FirstRowHeight);
            DrawIntelligenceInfo(FirstRowHeight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            DrawArtificeInfo(SecondRowHeight);
            DrawResilienceInfo(SecondRowHeight);
            DrawWisdomInfo(SecondRowHeight);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            DrawLuckInfo(ThirdRowHeight);
            DrawInfoFillerBox(ThirdRowHeight);   // draw 2 filler boxes to fill empty grid cells on 3rd row
            DrawInfoFillerBox(ThirdRowHeight);
            GUILayout.EndHorizontal();

            if (!SettingsGUI.PendingSettingsWindow &&
                !SettingsGUI.PendingCrestUnlock &&
                !SettingsGUI.PendingStatusReset)
            {
                GUILayout.EndScrollView();
            }

            GUI.color = Color.white;
            CustomCursor.EndArea();

            GUI.enabled = true;
        }

        private void DrawStatButton(string statName, string displayValue, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (CustomCursor.Button(
                    $"{statName}: {displayValue}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(LevelingAreaButtonHeight))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabel(string text)
        {
            GUILayout.Label(text, GUIStyles.LabelStyle, GUILayout.Width(185f), GUILayout.Height(40f));
        }

        private void DrawStatButtonExtended(string statName, bool gotFreeLevel, PlayerStatus s,
                                   int rosariesToLvlUp)
        {
            if (CustomCursor.Button(
                    $"{statName}",
                    GUIStyles.ButtonStyle,
                    GUILayout.Width(160f),
                    GUILayout.Height(LevelingAreaButtonHeight))
                && LevelingSystem.CanLevelUp(gotFreeLevel, rosariesToLvlUp)
                && PlayerData.instance.atBench)
            {
                LevelingSystem.Instance.IncreaseStat(statName, s, rosariesToLvlUp);
            }
        }

        private void DrawStatLabelExtended(string text)
        {
            GUILayout.Label(text, GUIStyles.LabelStyle, GUILayout.Width(75f), GUILayout.Height(40f));
        }

        private void DrawInfoStatLabel(string text)
        {
            GUILayout.Label(text, GUIStyles.LvlInfoLabelStyle, GUILayout.Width(ColWidth), GUILayout.Height(40f));
        }

        // info boxes for all stats
        private void DrawStrengthInfo(float height)
        {
            string s = "strength";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Strength", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Needle Damage: " + _needleBaseDamage);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            // needle strike auto-release toggle
            GUI.enabled = StrengthLogic.CheckStrengthMilestone(1);

            string autoStrike = "Needle strike auto-release: " +
                $"{(Bonfire.Instance.Status.AutoReleaseNeedleStrike ? "ENABLED" : "DISABLED")}";
            Color bgOld = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0, 0.5f, 0.8f);
            if (CustomCursor.Button(autoStrike, GUIStyles.LvlInfoButtonStyle, GUILayout.Height(30f)))
            {
                Bonfire.Instance.Status.AutoReleaseNeedleStrike = !Bonfire.Instance.Status.AutoReleaseNeedleStrike;
            }
            GUI.backgroundColor = bgOld;
            GUI.enabled = true;
            GUILayout.Space(5);

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(1)}] " +
                "Needle strike: active charge time reduced by 20%. " +
                "Can toggle on/off automatic release when fully charged.",
                LevelingSystem.HighLightStatMilestone(s, 1) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(2)}] " +
                "Needle range increased by 33%. Needle strike initial charge time reduced by 20%.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(3)}] " +
                "Flintslate and Flea Brew tools have infinite buff duration. " +
                "Downslash damage increased by 100%. " +
                "Needle strike: both initial and active charge time reduction buffed (20% -> 40%).",
                LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawDexterityInfo(float height)
        {
            string s = "dexterity";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Dexterity", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Slash Speed Multiplier: " + _attackSpeed);
            DrawInfoStatLabel("Crit Damage Bonus: " + _critDamageBonus);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(1)}] " +
                "Clawline damage increased by 25%. " +
                $"Every {DexterityLogic.ClawlineHitLimit}th hit from Clawline deals 2x damage " +
                "(default Clawline attack counts as 2 hits).",
                LevelingSystem.HighLightStatMilestone(s, 1) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(2)}] " +
                "Clawline silk cost removed, can now critically hit.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(3)}] " +
                "All critical hits now heal 1 mask. Clawline's 7th hit will always heal 1 mask " +
                "and deals 3x damage. Clawline damage bonus increased (25% -> 50%).",
            LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawIntelligenceInfo(float height)
        {
            string s = "intelligence";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Intelligence", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Skill Damage Bonus: " + _spellDamageBonus);
            DrawInfoStatLabel("Binding Cost: " + _bindCost);

            GUI.enabled = IntelligenceLogic.CheckIntelligenceMilestone(1);

            GUILayout.Label("Secondary", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Taunt Damage: " + _tauntDamage);
            DrawInfoStatLabel("Taunt Range: " + _tauntRange);

            GUI.enabled = true;

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(1)}] " +
                "Using 'Challenge' taunt now deals damage in a circular area around you. " +
                "Damage and area scales off of intelligence stat. " +
                "Taunt does not benefit from Silk Skill damage bonuses.",
                LevelingSystem.HighLightStatMilestone(s, 1) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(2)}] " +
                "While equipping Dead Bug's Purse/Shell Satchel tool, " +
                "Bind heals 1 mask and costs 1/3 of current cost, but you get following bonuses:\n" +
                "- Silk Skills: cost reduced by 1, damage increased by 50% multiplicatively.\n" +
                "- Claw Mirror: procs additional time (similar to Multibinder effect), " +
                "binding speed increased by 33%.\n" +
                "- Taunt: area increased by 50%.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(3)}] " +
                "Silk skills, Claw Mirror and Taunt can crit. Equipping Claw Mirror increases binding speed by 33%.",
            LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(4)}] " +
                "Silk skill cost reduced by 1. Claw Mirror damage now also scales off of intelligence while " +
                "keeping Artifice scaling. " +
                "Taunt gains +5 damage from each strand of silk in spool.",
                LevelingSystem.HighLightStatMilestone(s, 4) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawArtificeInfo(float height)
        {
            string s = "artifice";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Artifice", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Tool Damage Bonus: " + _toolDamageBonus);
            DrawInfoStatLabel("Tool Regen Interval: " + _toolRegenInterval);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(1)}] " +
                "Silkshot silk cost removed. Sawtooth Circlet and Pimpillo can critically hit.",
                LevelingSystem.HighLightStatMilestone(s, 1) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(2)}] " +
                "Sawtooth Circlet gives 1 silk when used. " +
                "Rosary Cannon keeps limited charges during use, but will always replenish them fully " +
                "when initial animation starts.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(3)}] " +
                "Silkshot has infinite charges. Sawtooth Circlet horizontal range increased by 50%.",
            LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(4)}] " +
                "Using an offensive red tool has 33% chance to spawn a copy of itself. " +
                "Neither original nor copy cost charges.\n" +
                "Following tools cannot utilize this effect: Silkshot, Delver's Drill, Rosary Cannon, " +
                "Voltvessels Spear (Bolas do work), Snare Setter.\n" +
                "Rosary Cannon: +2 flat damage which is applied before Artifice stat multiplier.",
                LevelingSystem.HighLightStatMilestone(s, 4) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawResilienceInfo(float height)
        {
            string s = "resilience";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Resilience", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Lifeblood Masks: " + _extraMasks
                + $"{(_extraMasks == "8" ? " (max value reached)" : " (max value is 8)")}");
            DrawInfoStatLabel("Block Chance: " + _blockChance);

            GUILayout.Label("Vengeful", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Reflect Damage: " + _reflectDamage);
            DrawInfoStatLabel("Reflect Area: " + _reflectArea);

            GUILayout.Label("Hollow", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Mask Damage Bonus: " + _glassCannonMaskBonus);
            DrawInfoStatLabel("Milestone Bonus: " + _glassCannonMilestoneBonus);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            // reflect/glass cannon mode toggle
            GUI.enabled = ResilienceLogic.CheckResilienceMilestone(1);

            var mode = (BlueState)Bonfire.Instance.Status.BlueMaskMode;

            string autoStrike = "Current blue mask mode: " + GetBlueStateString(mode);
            Color bgOld = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0, 0.5f, 0.8f);
            if (CustomCursor.Button(autoStrike, GUIStyles.LvlInfoButtonStyle, GUILayout.Height(30f)))
            {
                Bonfire.Instance.Status.BlueMaskMode = (int)RotateBlueState(mode);
            }
            GUI.backgroundColor = bgOld;
            GUI.enabled = true;
            GUILayout.Space(5);

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(1)}] " +
                "You can now optionally select 'Vengeful' or 'Hollow' state.\n" +
                "When Vengeful/Hollow is active:\n" +
                "- Blue masks effectively replace normal masks: if you have no blue hp left, the next hit will " +
                "inflict normal damage x 10.\n" +
                "- All damage is reduced by 1, but must always deal at least 1 damage.\n" +
                "- You can enter Plasmified (normally triggered with 9 blue masks) only after unlocking the final " +
                "milestone.\n" +
                "=> Both playstyles are quite risky to play early, but Vengeful will lean more towards the tanky, " +
                "low-risk playstyle whereas with Hollow you want to minimize hits taken at all cost." +
                "\n" +
                "<Vengeful>\n " +
                "- reflect damage back to enemies around you when it. All your non-reflect damage is reduced by 99%, " +
                "always dealing at least 1 damage.\n" +
                "- Base damage is 5. Damage scales from 1. Resilience stat extra blue masks count (max 8) and " +
                "2. mask upgrades (max 5).\n" +
                "- Bind now heals 1 blue mask.\n" +
                "- You get 1 silk after getting hit by an enemy.\n" +
                "=> Low-risk playstyle: you can become quite tanky, but also limit offensive capabilities to running " +
                "at enemies.\n" +
                "\n" +
                "<Hollow>\n" +
                "- Bind can no longer be used at all.\n" +
                "- You can no longer benefit from stat primary damage bonuses " +
                "(Strength, Dexterity, Intelligence, Artifice) or Luck crit chance." +
                "Bonuses from milestones work normally.\n" +
                "- Damage from all sources is increased by 50%. Each active blue mask in hp bar gives 7% extra damage" +
                "additively, masks upgrades 8% instead. With max 8 blue masks + 5 mask upgrades, this reaches 146%.\n" +
                "All non-Resilience stat milestones require 3 less points to become active. You must still have at " +
                "least 1 point in stat to activate any milestones.\n" +
                "=> High-risk playstyle as healing is pretty much non-existent, but gives an universal damage boost " +
                "and allows reaching non-Resilience milestones with fewer stat points.",
                LevelingSystem.HighLightStatMilestone(s, 1) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(2)}] " +
                "<Vengeful> Bind now heals 2 blue masks, small increase in reflect damage area.\n" +
                "<Hollow> You get flat +10 damage bonus which is applied after percentage bonuses. Milestone bonus " +
                "increased to 5.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(3)}] " +
                "You can now enter Plasmified state. Plasmium Phial will always trigger Plasmified when you are " +
                "in Vengeful/Hollow state. While Plasmified, you get a more offensive variant of current state:\n" +
                "\n" +
                "<Vengeful>\n" +
                "- Each active blue mask in hp bar now also gives additional +10 reflect damage.\n" +
                "- Reflect damage can crit.\n" +
                "- Reflect area increased by 2.\n" +
                "- Penalty: you take normal amount of damage again and bind heals only the default amount of 1 during" +
                "during Plasmification.\n" +
                "\n" +
                "<Hollow>\n" +
                "- Active blue mask bonus increased (7% -> 20%).\n" +
                "- Crit change and crit damage can now be applied normally\n" +
                "- Milestone bonus increased to 10.\n" +
                "- Penalty: you take normal amount of damage + additional 2 masks (so 1 damage becomes 3 damage).\n" +
                "\n" +
                "-> Reminder: your max hp in Plasmified state is equal to your normal mask count and each mask " +
                "functions as a blue hp.",
            LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawWisdomInfo(float height)
        {
            string s = "wisdom";

            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Wisdom", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Gained On Enemy Hit: " + _extraSilk);
            DrawInfoStatLabel("Silk Regen Interval: " + _silkRegenInterval);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(2)}] " +
                "Wispfire Lantern silk cost removed, attack delay halved and spawn cooldown is down to 2 seconds.",
                LevelingSystem.HighLightStatMilestone(s, 2) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);
            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(3)}] " +
                "Wispfire Lantern attack cooldown down to 1/3 of original, spawns a wisp every second.",
                LevelingSystem.HighLightStatMilestone(s, 3) ?
                    GUIStyles.LvlInfoLabelStyle : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
        }

        private void DrawLuckInfo(float height)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));
            GUILayout.Label("Luck", GUIStyles.LabelStyle);

            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Crit Chance: " + _critChance);
            DrawInfoStatLabel("Rosary & Shard Increase: " + _rosaryIncrease);

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            GUILayout.EndVertical();
        }

        // filler component for empty cell spaces
        private void DrawInfoFillerBox(float height)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(ColWidth), GUILayout.Height(height));

            DrawInfoStatLabel("");

            GUILayout.EndVertical();
        }

        private void ChangeLevelingMenuStyle()
        {
            int s = Bonfire.Instance.Status.LevelingInfoStyle + 1;
            if (s >= 4 || s < 0)
                s = 0;

            Bonfire.Instance.Status.LevelingInfoStyle = s;
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

        private void UpdateGUIFields()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            _availableRelicLevels = ls.GetAvailableRelicLevels();
            _gotFreeLevel = _availableRelicLevels > 0;

            _rosaryLevel = LevelingSystem.DisplayPendingRosaryLevel();
            _rosariesToLvlUp = LevelingSystem.GetLevelUpRosaryRequirement(_rosaryLevel);

            _currentLvl = LevelingSystem.GetCurrentLevel();

            // preview strings
            _totalStr = (s.StrengthStat + ls.StrengthIncrease).ToString();
            _totalDex = (s.DexterityStat + ls.DexterityIncrease).ToString();
            _totalInt = (s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            _totalArt = (s.ArtificeStat + ls.ArtificeIncrease).ToString();
            _totalRes = (s.ResilienceStat + ls.ResilienceIncrease).ToString();
            _totalWsdm = (s.WisdomStat + ls.WisdomIncrease).ToString();
            _totalLuck = (s.LuckStat + ls.LuckIncrease).ToString();

            // stat calculations
            _needleBaseDamage = StrengthLogic.NeedleDamage(
                5 + 4 * PlayerData.instance.nailUpgrades,
                s.StrengthStat + ls.StrengthIncrease)
                .ToString();

            _attackSpeed = DexterityLogic.AttackSpeed(s.DexterityStat + ls.DexterityIncrease).ToString();
            _critDamageBonus =
                (DexterityLogic.CritDamage(s.DexterityStat + ls.DexterityIncrease, 100) - 100).ToString() + "%";

            _spellDamageBonus = (IntelligenceLogic.SpellDamage(
                100, s.IntelligenceStat + ls.IntelligenceIncrease) - 100)
                .ToString() + "%";
            _bindCost = IntelligenceLogic.BindCost(s.IntelligenceStat + ls.IntelligenceIncrease).ToString();
            _tauntDamage = IntelligenceLogic.TauntDamage(s.IntelligenceStat + ls.IntelligenceIncrease, isGUI: true)
                .ToString();
            _tauntRange = IntelligenceLogic.TauntRange(s.IntelligenceStat + ls.IntelligenceIncrease, isGUI: true)
                .ToString();

            _toolDamageBonus = (ArtificeLogic.ToolDamage(100, s.ArtificeStat + ls.ArtificeIncrease) - 100)
                .ToString() + "%";
            _toolRegenInterval = "-";
            if (s.ArtificeStat + ls.ArtificeIncrease > 0)
            {
                int toolRegen = ArtificeLogic.ToolRegenInterval(s.ArtificeStat + ls.ArtificeIncrease);
                _toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            _extraMasks = ResilienceLogic.BlueMasks(s.ResilienceStat + ls.ResilienceIncrease).ToString();
            _blockChance = s.ResilienceStat + ls.ResilienceIncrease > 0
                ? ResilienceLogic.AverageBlockChance(s.ResilienceStat + ls.ResilienceIncrease).ToString("F0") + "%"
                : "0%";
            _reflectDamage = "0";
            _reflectArea = "0";
            _glassCannonMaskBonus = "0%";
            _glassCannonMilestoneBonus = "0%";
            if (s.ResilienceStat + ls.ResilienceIncrease > 0)
            {
                string reflectDamage = ResilienceLogic.ReflectDamage(
                    s.ResilienceStat + ls.ResilienceIncrease, isGUI: true)
                    .ToString();
                string reflectArea = ResilienceLogic.ReflectArea(isGUI: true).ToString();

                string ggMaskBonus = ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON)
                    ? (ResilienceLogic.GlassCannonDamageMaskBonus(isGUI: true) * 100 - 100).ToString("F0") + "%"
                    : "0%";
                string ggMilestoneBonus = ResilienceLogic.GetGlassCannonMilestoneBonus(
                    s.ResilienceStat + ls.ResilienceIncrease)
                    .ToString();

                if (ResilienceLogic.CheckResilienceMilestone(3))
                {
                    if (ResilienceLogic.IsInBlueState(BlueState.REFLECT))
                    {
                        reflectDamage += " (P: " + ResilienceLogic.ReflectDamage(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            + ")";
                        reflectArea += " (P: " + ResilienceLogic.ReflectArea(
                            isGUI: true, GUIPlasmified: true)
                            .ToString() + ")";
                    }
                    else if (ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON))
                    {
                        ggMaskBonus += " (P: "
                            + (ResilienceLogic.GlassCannonDamageMaskBonus(isGUI: true, GUIPlasmified: true) * 100 - 100)
                            .ToString("F0") + "%)";
                        ggMilestoneBonus += " (P: " + ResilienceLogic.GetGlassCannonMilestoneBonus(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            .ToString("F0") + ")";
                    }
                }

                _reflectDamage = reflectDamage;
                _reflectArea = reflectArea;
                _glassCannonMaskBonus = ggMaskBonus;
                _glassCannonMilestoneBonus = ggMilestoneBonus;
            }

            _extraSilk = WisdomLogic.ExtraSilk(s.WisdomStat + ls.WisdomIncrease, 1).ToString();
            _silkRegenInterval = "-";
            if (s.WisdomStat + ls.WisdomIncrease > 0)
            {
                int silkRegen = WisdomLogic.SilkRegenInterval(s.WisdomStat + ls.WisdomIncrease);
                _silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }

            _critChance = LuckLogic.CritChance(s.LuckStat + ls.LuckIncrease).ToString() + "%";
            _rosaryIncrease = LuckLogic.GetRosaryIncreasePercentage(s.LuckStat, ls.LuckIncrease);

            // "Apply" button label
            int pRelics = s.PendingRelicLevels;
            if (s.PendingRelicLevels > 0 && s.PendingRosaries > 0)
            {
                _applyText =
                    $"Apply \n({s.PendingRosaries} rosaries + {s.PendingRelicLevels} " +
                    $"{(s.PendingRelicLevels > 1 ? "relics" : "relic")})";
            }
            else if (s.PendingRelicLevels > 0)
            {
                _applyText =
                    $"Apply ({s.PendingRelicLevels} relic {(s.PendingRelicLevels > 1 ? "levels" : "level")})";
            }
            else if (_gotFreeLevel)
            {
                _applyText = $"{_availableRelicLevels} Free Levels available!";
            }
            else
            {
                _applyText = $"Apply ({s.PendingRosaries} rosaries)";
            }
        }
    }
}