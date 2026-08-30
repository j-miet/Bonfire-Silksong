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
            if (!_LevelingColorsInitialized)
            {
                _infoButtonColor = new Color(0, 0.5f, 0.8f);
                _LevelingColorsInitialized = true;
            }

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

        private const float RowWidth = InfoAreaRectWidth - 55f;
        private const float BoxWidth = 0.5f * RowWidth;
        private const float InfoButtonWidth = InfoAreaRectWidth / 2.5f;
        private const float InfoButtonHeight = 40f;

        private const float StrengthBoxHeight = 80f;
        private const float DexterityBoxHeight = 120f;
        private const float IntelligenceBoxHeight = 120f;
        private const float ArtificeBoxHeight = 120f;
        private const float ResilienceBoxHeight = 260f;
        private const float WisdomBoxHeight = 350f;
        private const float LuckBoxHeight = 120f;

        private Vector2 _infoScrollPos;

        private bool _LevelingColorsInitialized;
        private Color _infoButtonColor;

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
        private string _weaverAuraDamage;
        private string _weaverAuraRange;

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
        private string _silkNailsDamage;
        private string _silkNailsSpawnCooldown;
        private string _cogflyDamage;
        private string _cogflyAttackCooldown;
        private string _lanternAttackCooldown;
        private string _lanternSpawnCooldown;

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

        private void DrawExtendedStatPanel()
        {
            GUI.color = new Color(0.8f, 0.7f, 0.3f, 0.9f);
            GUI.DrawTexture(
                new Rect(
                    InfoAreaRectTopX - 4, InfoAreaRectTopY - 4,
                    InfoAreaRectWidth + 8, InfoAreaRectHeight + 8),
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
            GUI.backgroundColor = Color.white;
            if (!SettingsGUI.PendingSettingsWindow &&
                !SettingsGUI.PendingCrestUnlock &&
                !SettingsGUI.PendingStatusReset)
            {
                _infoScrollPos = CustomCursor.BeginScrollView(_infoScrollPos);
            }
            GUI.backgroundColor = Color.white;

            GUILayout.BeginVertical();
            DrawStrengthInfo();
            DrawDexterityInfo();
            DrawIntelligenceInfo();
            DrawArtificeInfo();
            DrawResilienceInfo();
            DrawWisdomInfo();
            DrawLuckInfo();
            GUILayout.EndVertical();

            if (!SettingsGUI.PendingSettingsWindow &&
                !SettingsGUI.PendingCrestUnlock &&
                !SettingsGUI.PendingStatusReset)
            {
                CustomCursor.EndScrollView();
            }

            GUI.color = Color.white;
            CustomCursor.EndArea();

            GUI.enabled = true;
        }

        // info boxes for all stats
        private void DrawStrengthInfo()
        {
            string s = "strength";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Strength", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(StrengthBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Needle Damage: " + _needleBaseDamage);
            GUILayout.EndVertical();

            // secondary (filler, add still to keep GUI aligned)
            GUILayout.BeginVertical("box", GUILayout.Height(StrengthBoxHeight));
            DrawInfoStatLabel("");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // needle strike auto-release toggle
            GUI.enabled = StrengthLogic.CheckStrengthMilestone(1);

            string autoStrikeToggle = "Needle strike auto-release: " +
                $"{(Bonfire.Instance.Status.AutoReleaseNeedleStrike ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                autoStrikeToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(InfoButtonWidth),
                GUILayout.Height(InfoButtonHeight)))
            {
                Bonfire.Instance.Status.AutoReleaseNeedleStrike = !Bonfire.Instance.Status.AutoReleaseNeedleStrike;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(1)}] " +
                "Needle strike: active charge time reduced by 20%. " +
                "Can toggle on/off automatic release when fully charged.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(2)}] " +
                "Needle range increased by 33%. Needle strike initial charge time reduced by 20%.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthLogic.StrengthMilestone(3)}] " +
                "Flintslate and Flea Brew tools have infinite buff duration. " +
                "Downslash damage increased by 100%. " +
                "Needle strike: both initial and active charge time reduction buffed (20% -> 40%).",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawDexterityInfo()
        {
            string s = "dexterity";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Dexterity", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(DexterityBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Slash Speed Multiplier: " + _attackSpeed);
            DrawInfoStatLabel("Crit Damage Bonus: " + _critDamageBonus);
            GUILayout.EndVertical();

            // secondary (filler)
            GUILayout.BeginVertical("box", GUILayout.Height(DexterityBoxHeight));
            DrawInfoStatLabel("");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(1)}] " +
                "Clawline damage increased by 25%. " +
                $"Every {DexterityLogic.ClawlineHitLimit}th hit from Clawline deals 2x damage " +
                "(default Clawline use counts as 2 hits).",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(2)}] " +
                "Clawline silk cost removed, can now critically hit.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityLogic.DexterityMilestone(3)}] " +
                "All critical hits now heal 1 mask. Clawline's 7th hit will always heal 1 mask " +
                "and deals 3x damage. Clawline damage bonus increased (25% -> 50%).",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);


            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawIntelligenceInfo()
        {
            string s = "intelligence";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Intelligence", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(IntelligenceBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Skill Damage Bonus: " + _spellDamageBonus);
            DrawInfoStatLabel("Binding Cost: " + _bindCost);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(IntelligenceBoxHeight));
            GUILayout.Label("Weaver's Light", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Damage: " + _weaverAuraDamage);
            DrawInfoStatLabel("Range: " + _weaverAuraRange);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // weaver aura/pale burst toggle
            GUI.enabled = IntelligenceLogic.CheckIntelligenceMilestone(1);

            var mode = (WeaverAura)Bonfire.Instance.Status.WeaverAuraMode;
            string weaverAuraToggle = "Cast Weaver's Light when taunting: " + GetWeaverAuraString(mode);
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                weaverAuraToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(InfoButtonWidth),
                GUILayout.Height(InfoButtonHeight)))
            {
                Bonfire.Instance.Status.WeaverAuraMode = (int)RotateWeaverAuraState(mode);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(1)}] " +
                "Toggle on/off 'Weaver's Light'.\n" +
                "\n" +
                "<Weaver's Light>\n" +
                "- While active, taunt now enables an aura around you which flashes every second, consuming 1 silk " +
                "dealing damage to enemies inside it. Damage and range increases with Intelligence stat.\n" +
                "- If you use Throwing Rings: aura will instead cost 2 silk per second and has its range " +
                "reduced by 50%, but you get +3 extra damage for each silk in spool.\n" +
                "- Taunt no longer grants the 50% damage bonus for next hit\n" +
                "- Equipping Egg of Flealia reduces aura silk cost by 1 when on max hp.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(2)}] " +
                "While equipping Dead Bug's Purse/Shell Satchel yellow tool, you get 'Weak Bind'. " +
                "While Weak Bind is active" +
                "- Bind heals 1 mask and costs 1/3 of current cost while keeping its base speed,\n" +
                "- You take + 1 damage from everything,\n" +
                "but you the following bonuses:\n" +
                "- Silk Skills: cost reduced by 1, damage increased by 50% multiplicatively.\n" +
                "- Claw Mirror: procs additional time (similar to Multibinder effect), " +
                "binding speed increased by 33%.\n" +
                "- Weaver's Light: gives 1 silk if at least one enemy was hit.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(3)}] " +
                "Silk skills, Claw Mirror and Taunt can crit. Equipping Claw Mirror increases binding speed by 33%.",
            LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceLogic.IntelligenceMilestone(4)}] " +
                "All Intelligence-related abilities are buffed:\n" +
                "- Silk skills: silk cost reduced by 1.\n" +
                "- Claw Mirror: every 2 points in Intelligence adds +1 damage. Damage is applied after Artifice stat " +
                "scaling.\n" +
                "- Weaver's Light: Gains a new 'Void Burst' toggle state. Void Burst makes taunt a single-use " +
                "ability, consuming all silk when used while giving following benefits: \n" +
                "  - Range increased by 5.\n" +
                "  - Each silk in spool increases damage by +6.\n" +
                "\n" +
                "Taunting with Throwing Rings has no additional effects.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawArtificeInfo()
        {
            string s = "artifice";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Artifice", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(ArtificeBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Tool Damage Bonus: " + _toolDamageBonus);
            DrawInfoStatLabel("Tool Regen Interval: " + _toolRegenInterval);
            GUILayout.EndVertical();

            // secondary (filler)
            GUILayout.BeginVertical("box", GUILayout.Height(ArtificeBoxHeight));
            DrawInfoStatLabel("");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(1)}] " +
                "Pimpillo: has 2x tool capacity and can apply critical hits.\n" +
                "Sawtooth Circlet: can apply critical hits.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(2)}] " +
                "Rosary Cannon: has infinite, free refills. Every time you start the use animation, it becomes " +
                "fully loaded loaded.\n" +
                "Silkshot: silk cost removed, can apply critical hits.\n" +
                "Sawtooth Circlet: gives 1 silk from first enemy hit.\n",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(3)}] " +
                "Rosary Cannon: +2 flat damage which is applied before Artifice stat damage bonus.\n" +
                "Silkshot: has infinite charges.\n" +
                "Sawtooth Circlet: horizontal range increased by 50%.\n" +
                "Pimpillo: casts 'Rune Rage' silk skill on top of enemy on hit. Skill scales normally from any silk " +
                "skill buffs.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeLogic.ArtificeMilestone(4)}] " +
                "Offensive red tool has 33% chance to spawn a copy of itself when used. " +
                "When a copy is spawned, neither original nor copy cost charges.\n" +
                "Following tools cannot benefit from this: Silkshot, Delver's Drill, Rosary Cannon, " +
                "Voltvessels Spear (Bolas do work), Snare Setter.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawResilienceInfo()
        {
            string s = "resilience";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Resilience", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(ResilienceBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Lifeblood Masks: " + _extraMasks
                + $"{(_extraMasks == "8" ? " (max value reached)" : " (max value is 8)")}");
            DrawInfoStatLabel("Block Chance: " + _blockChance);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(ResilienceBoxHeight));
            GUILayout.Label("Vengeful", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Reflect Damage: " + _reflectDamage);
            DrawInfoStatLabel("Reflect Area: " + _reflectArea);
            GUILayout.Label("Hollow", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Mask Damage Bonus: " + _glassCannonMaskBonus);
            DrawInfoStatLabel("Milestone Bonus: " + _glassCannonMilestoneBonus);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // reflect/glass cannon mode toggle
            GUI.enabled = ResilienceLogic.CheckResilienceMilestone(1);

            var mode = (BlueState)Bonfire.Instance.Status.BlueMaskMode;

            string blueModeToggle = "Current blue mask mode: " + GetBlueStateString(mode);
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                blueModeToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(InfoButtonWidth),
                GUILayout.Height(InfoButtonHeight)))
            {
                Bonfire.Instance.Status.BlueMaskMode = (int)RotateBlueState(mode);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(1)}] " +
                "You can now select 'Vengeful' or 'Hollow' blue health mode.\n" +
                "\n" +
                "When Vengeful/Hollow is active:\n" +
                "- Blue masks effectively replace normal masks: if you have no blue hp left, the next hit will " +
                "inflict 'normal damage x 10'.\n" +
                "- All damage is reduced by 1, but must always deal at least 1 damage.\n" +
                "- You can enter Plasmified state (normally triggered with 9 blue masks) only after unlocking the " +
                "final Resilience milestone.\n" +
                "- Egg of Flealia gives silk cost reduction only at max blue hp.\n" +
                "=> Both playstyles are quite risky to play early, but Vengeful will lean more towards the tanky, " +
                "low-risk playstyle whereas with Hollow you want to minimize hits taken at all cost.\n" +
                "\n" +
                "<Vengeful>\n " +
                "- Reflect damage back to enemies around you when it. All your non-reflect damage is reduced by 99%, " +
                "but always dealing at least 1 damage. Only exception to this rule is the 'Memory Crystal' blue " +
                "tool.\n" +
                "- Base damage is 5. Damage scales from 1. Resilience stat extra blue masks count (max 8) and " +
                "2. mask upgrades (max 5).\n" +
                "- Bind now heals 1 blue mask.\n" +
                "- You get 1 silk after getting hit by an enemy.\n" +
                "=> Low-risk playstyle: you can become quite tanky, but also limit offensive capabilities to running " +
                "at enemies.\n" +
                "\n" +
                "<Hollow>\n" +
                "- Bind can no longer be used.\n" +
                "- You can no longer benefit from stat primary damage bonuses " +
                "(Strength, Dexterity, Intelligence, Artifice) or Luck crit chance. " +
                "Bonuses from milestones work normally.\n" +
                "- Damage from all sources is increased by 50%. Each active blue mask in hp bar gives 7% extra damage" +
                "additively, masks upgrades 8% instead.\n" +
                "All non-Resilience stat milestones require 3 less points to become active. You must still have at " +
                "least 1 point in stat to activate any milestones.\n" +
                "=> High-risk playstyle as healing is pretty much non-existent, but gives an universal damage boost " +
                "and allows reaching non-Resilience milestones with fewer stat points.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(2)}] " +
                "<Vengeful> Bind now heals 2 blue masks, reflect damage area increased by 1.\n" +
                "<Hollow> You get flat +10 damage bonus which is applied after percentage bonuses. Milestone bonus " +
                "increased to 5.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceLogic.ResilienceMilestone(3)}] " +
                "While in Vengful/Hollow state, you can now enter Plasmified state and Plasmium Phial will instantly " +
                "trigger Plasmified when used. While Plasmified, your use of Plasmium Phial is blocked but you get " +
                "a more offensive variant of current state:\n" +
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
                "- Crit change and crit damage can now be applied normally\n." +
                "- Milestone bonus increased to 10.\n" +
                "- Penalty: you take normal amount of damage + additional 2 masks (so 1 damage becomes 3 damage).\n" +
                "\n" +
                "Your max hp in Plasmified state is equal to your normal mask count and each mask is treated as a " +
                "blue mask, giving you damage buffs",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawWisdomInfo()
        {
            string s = "wisdom";

            GUILayout.BeginVertical("box");
            GUILayout.Label("Wisdom", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(WisdomBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Silk Gained On Enemy Hit: " + _extraSilk);
            DrawInfoStatLabel("Silk Regen Interval: " + _silkRegenInterval);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(WisdomBoxHeight));
            GUILayout.Label("Silk Nails", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Damage (single nail): " + _silkNailsDamage);
            DrawInfoStatLabel("Spawn Cooldown: " + _silkNailsSpawnCooldown);
            GUILayout.Label("Cogwork Warrior", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Damage: " + _cogflyDamage);
            DrawInfoStatLabel("Attack Cooldown: " + _cogflyAttackCooldown);
            GUILayout.Label("Wispfire Lantern", GUIStyles.LvlInfoHeaderSecondaryStyle);
            DrawInfoStatLabel("Attack Cooldown: " + _lanternAttackCooldown);
            DrawInfoStatLabel("Spawn Delay: " + _lanternSpawnCooldown);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box");
            GUILayout.BeginVertical();

            // QoL: block summons using Challenge taunt

            GUI.enabled = LevelingSystem.HighLightStatMilestone(s, 1) && PlayerData.instance.hasNeedolin;
            GUILayout.Label(
                $"[Quality of Life]\n" +
                "\n" +
                "If you have unlocked 1. Wisdom milestone and use Needolin, " +
                "you disable/re-enable all your following summons: \n" +
                "- Wispfire Lantern\n" +
                "- Silk Nails\n" +
                "- Cogwork Warrior.\n" +
                "\n" +
                "GUI buttons are also disabled during this 'BLOCKED' state.\n" +
                "\n" +
                "- Hornet's texture has a brief white flash if disable/enable worked.\n" +
                "- This setting will not be saved and starts always in enabled state. This means if you close the " +
                " game and then launch it again, the first taunt will always set state to 'BLOCKED' even if you have " +
                "them already disabled.\n" +
                "\n" +
                "=> This feature is meant to be used during platforming sections which could otherwise be quite" +
                "frustrating with multiple summons shooting at enemies.",
                GUIStyles.LvlInfoLabelStyle);
            GUILayout.Label(
                $"[Summons] => {(WisdomLogic.BlockSummons ? "BLOCKED" : "ALLOWED")}\n",
                GUIStyles.LvlInfoLabelStyle);

            GUI.enabled = true;

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // Silk Nails toggle
            GUI.enabled = WisdomLogic.CheckWisdomMilestone(1) && !WisdomLogic.BlockSummons;

            string silkNailsToggle = "Silk Nails: " +
                $"{(Bonfire.Instance.Status.SilkNailsEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                silkNailsToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(InfoButtonWidth),
                GUILayout.Height(InfoButtonHeight)))
            {
                Bonfire.Instance.Status.SilkNailsEnabled = !Bonfire.Instance.Status.SilkNailsEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            // Cogfly Warrior toggle
            GUI.enabled = WisdomLogic.CheckWisdomMilestone(2) && !WisdomLogic.BlockSummons;

            string cogflyWarriorToggle = "Cogfly Warrior: " +
                $"{(Bonfire.Instance.Status.CogflyWarriorEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = new Color(0, 0.5f, 0.8f);
            if (CustomCursor.Button(
                cogflyWarriorToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(InfoButtonWidth),
                GUILayout.Height(InfoButtonHeight)))
            {
                Bonfire.Instance.Status.CogflyWarriorEnabled = !Bonfire.Instance.Status.CogflyWarriorEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(1)}] " +
                "Toggle on/off 'Silk Nails' which is a weaker version of Pale Nails silk skill.\n" +
                "\n" +
                "<Silk Nails>\n" +
                "- Has its own damage and spawn cooldown scaling from Wisdom.\n" +
                "- Spawns automatically when enemies are near.\n" +
                "- Doesn't have a silk cost.\n",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(2)}] " +
                "Toggle on/off 'Cogfly Warrior' which is a stronger version of Cogfly.\n" +
                "\n" +
                "<Cogfly Warrior>\n" +
                "- Has its own damage and attack cooldown scaling from Wisdom.\n" +
                "- Has no max hit limit, meaning the minion will never die + it auto-summons itself if some game " +
                "event ends up destroying it (e.g. cutscenes).\n" +
                "- Total cogfly amount limited to 1, final milestone increases this by 1.\n" +
                "- While Cogfly Warrior is enabled, you cannot use Cogfly tool.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(3)}] " +
                "Wispfire Lantern silk cost removed, attack delay halved and spawn cooldown is down to 2 seconds.\n" +
                "[Reminder] Wispfire Lantern has no Wisdom stat scaling, only tool damage matters.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomLogic.WisdomMilestone(4)}] " +
                "All of Wisdom's offensive abilities buffed:\n" +
                "- Silk Nails: Can now critically hit.\n" +
                "- Cogfly Warrior: summons a second cogfly.\n" +
                "- Wispfire Lantern: attack cooldown down to 1/3 of original, spawns a wisp every second.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawLuckInfo()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Luck", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(LuckBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            DrawInfoStatLabel("Crit Chance: " + _critChance);
            DrawInfoStatLabel("Rosary & Shard Increase: " + _rosaryIncrease);
            GUILayout.EndVertical();

            // secondary (filler)
            // GUILayout.BeginVertical(GUILayout.Height(LuckBoxHeight));
            // DrawInfoStatLabel("");
            // GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones (if needed later)
            // GUILayout.BeginHorizontal("box");
            // GUILayout.BeginVertical();
            // GUILayout.Label("", GUIStyles.LvlInfoHeaderMilestoneStyle);
            // GUILayout.EndVertical();
            // GUILayout.EndHorizontal();

            GUILayout.EndVertical();
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
            GUILayout.Label(
                text,
                GUIStyles.LabelStyle,
                GUILayout.Width(185f),
                GUILayout.Height(LevelingAreaButtonHeight));
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
            GUILayout.Label(
                text,
                GUIStyles.LabelStyle,
                GUILayout.Width(75f),
                GUILayout.Height(LevelingAreaButtonHeight));
        }

        private void DrawInfoStatLabel(string text)
        {
            GUILayout.Label(
                text,
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(BoxWidth),
                 GUILayout.Height(LevelingAreaButtonHeight));
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
            if (IntelligenceLogic.CheckIntelligenceMilestone(4))
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

        private void UpdateGUIFields()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            int strNew = s.StrengthStat + ls.StrengthIncrease;
            int dexNew = s.DexterityStat + ls.DexterityIncrease;
            int intNew = s.IntelligenceStat + ls.IntelligenceIncrease;
            int artNew = s.ArtificeStat + ls.ArtificeIncrease;
            int resNew = s.ResilienceStat + ls.ResilienceIncrease;
            int wsdmNew = s.WisdomStat + ls.WisdomIncrease;
            int luckNew = s.LuckStat + ls.LuckIncrease;

            _availableRelicLevels = ls.GetAvailableRelicLevels();
            _gotFreeLevel = _availableRelicLevels > 0;

            _rosaryLevel = LevelingSystem.DisplayPendingRosaryLevel();
            _rosariesToLvlUp = LevelingSystem.GetLevelUpRosaryRequirement(_rosaryLevel);

            _currentLvl = LevelingSystem.GetCurrentLevel();

            // stat preview strings
            _totalStr = strNew.ToString();
            _totalDex = dexNew.ToString();
            _totalInt = intNew.ToString();
            _totalArt = artNew.ToString();
            _totalRes = resNew.ToString();
            _totalWsdm = wsdmNew.ToString();
            _totalLuck = luckNew.ToString();

            // stat formula strings
            // Strength
            _needleBaseDamage = StrengthLogic.NeedleDamage(5 + 4 * PlayerData.instance.nailUpgrades, strNew).ToString();

            // Dexterity
            _attackSpeed = DexterityLogic.AttackSpeed(dexNew).ToString();
            _critDamageBonus = (DexterityLogic.CritDamage(dexNew, 100) - 100).ToString() + "%";

            // Intelligence
            _spellDamageBonus = (IntelligenceLogic.SpellDamage(100, intNew) - 100).ToString() + "%";
            _bindCost = IntelligenceLogic.BindCost(intNew).ToString();
            _weaverAuraDamage = IntelligenceLogic.WeaverAuraDamage(intNew, isGUI: true).ToString() +
                " (With Silk Bonus: " +
                $"{IntelligenceLogic.WeaverAuraDamage(intNew, isGUI: true, GUISilkBonus: true)})";
            _weaverAuraRange = IntelligenceLogic.WeaverAuraRange(intNew, isGUI: true).ToString();

            // Artifice
            _toolDamageBonus = (ArtificeLogic.ToolDamage(100, artNew) - 100).ToString() + "%";
            _toolRegenInterval = "-";
            if (artNew > 0)
            {
                int toolRegen = ArtificeLogic.ToolRegenInterval(artNew);
                _toolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            // Resilience
            _extraMasks = ResilienceLogic.BlueMasks(resNew).ToString();
            _blockChance = resNew > 0 ? ResilienceLogic.AverageBlockChance(resNew).ToString("F0") + "%" : "0%";
            _reflectDamage = "0";
            _reflectArea = "0";
            _glassCannonMaskBonus = "0%";
            _glassCannonMilestoneBonus = "0%";
            if (resNew > 0)
            {
                string reflectDamage = ResilienceLogic.ReflectDamage(resNew, isGUI: true).ToString();
                string reflectArea = ResilienceLogic.ReflectArea(isGUI: true).ToString();

                string ggMaskBonus = ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON)
                    ? (ResilienceLogic.GlassCannonDamageMaskBonus(isGUI: true) * 100).ToString("F0") + "%"
                    : "0%";
                string ggMilestoneBonus = ResilienceLogic.GetGlassCannonMilestoneBonus(resNew).ToString();

                if (ResilienceLogic.CheckResilienceMilestone(3))
                {
                    if (ResilienceLogic.IsInBlueState(BlueState.REFLECT))
                    {
                        reflectDamage += " (Plasmified: " + ResilienceLogic.ReflectDamage(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            + ")";
                        reflectArea += " (Plasmified: " + ResilienceLogic.ReflectArea(
                            isGUI: true, GUIPlasmified: true)
                            .ToString() + ")";
                    }
                    else if (ResilienceLogic.IsInBlueState(BlueState.GLASSCANNON))
                    {
                        ggMaskBonus += " (Plasmified: "
                            + (ResilienceLogic.GlassCannonDamageMaskBonus(isGUI: true, GUIPlasmified: true) * 100)
                            .ToString("F0") + "%)";
                        ggMilestoneBonus += " (Plasmified: " + ResilienceLogic.GetGlassCannonMilestoneBonus(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            .ToString("F0") + ")";
                    }
                }

                _reflectDamage = reflectDamage;
                _reflectArea = reflectArea;
                _glassCannonMaskBonus = ggMaskBonus;
                _glassCannonMilestoneBonus = ggMilestoneBonus;
            }

            // Wisdom
            _extraSilk = WisdomLogic.ExtraSilk(wsdmNew, 1).ToString();
            _silkRegenInterval = "-";
            if (wsdmNew > 0)
            {
                int silkRegen = WisdomLogic.SilkRegenInterval(wsdmNew);
                _silkRegenInterval = silkRegen > 0 ? silkRegen.ToString() + " s" : "-";
            }
            _silkNailsDamage = WisdomLogic.SilkNailsDamage(wsdmNew).ToString();
            _silkNailsSpawnCooldown = WisdomLogic.SilkNailsSpawnCooldown(wsdmNew).ToString("F2");
            _cogflyDamage = WisdomLogic.CogflyWarriorDamage(wsdmNew).ToString();
            _cogflyAttackCooldown = WisdomLogic.CogflyWarriorAttackCooldown(wsdmNew).ToString("F2");

            (float lanternAttack, float lanternSpawn) = WisdomLogic.WispLanternSpeed();
            _lanternAttackCooldown = lanternAttack.ToString();
            _lanternSpawnCooldown = lanternSpawn.ToString();

            // Luck
            _critChance = LuckLogic.CritChance(luckNew).ToString() + "%";
            _rosaryIncrease = LuckLogic.GetRosaryIncreasePercentage(s.LuckStat, ls.LuckIncrease);

            // "Apply" button label
            int pRelics = s.PendingRelicLevels;
            int pRosaries = s.PendingRosaries;
            if (pRelics > 0 && pRosaries > 0)
            {
                _applyText =
                    $"Apply \n({pRosaries} rosaries + {pRelics} " +
                    $"{(pRelics > 1 ? "relics" : "relic")})";
            }
            else if (pRelics > 0)
            {
                _applyText =
                    $"Apply ({pRelics} relic {(pRelics > 1 ? "levels" : "level")})";
            }
            else if (_gotFreeLevel)
            {
                _applyText = $"{_availableRelicLevels} Free Levels available!";
            }
            else
            {
                _applyText = $"Apply ({pRosaries} rosaries)";
            }
        }
    }
}