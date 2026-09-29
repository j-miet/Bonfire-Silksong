using UnityEngine;

using BonfireSilksong.GUIComponents;

namespace BonfireSilksong.Leveling.UI
{
    public class LevelInfo
    {
        public static bool LevelingColorsInitialized;
        public static Color InfoButtonColor;

        public LevelInfo(GameData data)
        {
            _d = data;
            _infoButtonColor = new Color(0, 0.5f, 0.8f);
        }

        public void Draw(float scrollHeight)
        {
            // cache checks to avoid stutters when scrollwheel height changes during repaint.
            if (Event.current.type == EventType.Layout)
                _drawRegion = GetDrawRegion(scrollHeight);

            GUILayout.BeginVertical();

            // these conditions check cursor position and render only some of the info boxes. While the current
            // implementation is very manual and has minor issues e.g. scrollbar nudging if dragged slowly, it's good 
            // to keep as it significantly reduces GUI rendering cost.
            switch (_drawRegion)
            {
                case 0:
                    DrawGlobalLevelInfo();
                    DrawStrengthInfo();
                    DrawDexterityInfo();
                    DrawIntelligenceInfo();
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            +Constants.ArtificeTotalHeight
                            + Constants.ResilienceTotalHeight
                            + Constants.WisdomTotalHeight
                            + Constants.LuckTotalHeight));
                    break;
                case 1:
                    GUILayout.Label(GUIContent.none, GUILayout.Height(Constants.GlobalTotalHeight));
                    DrawStrengthInfo();
                    DrawDexterityInfo();
                    DrawIntelligenceInfo();
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.ArtificeTotalHeight
                            + Constants.ResilienceTotalHeight
                            + Constants.WisdomTotalHeight
                            + Constants.LuckTotalHeight));
                    break;
                case 2:
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.GlobalTotalHeight
                            + Constants.StrengthTotalHeight));
                    DrawDexterityInfo();
                    DrawIntelligenceInfo();
                    DrawArtificeInfo();
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.ResilienceTotalHeight
                            + Constants.WisdomTotalHeight
                            + Constants.LuckTotalHeight));
                    break;
                case 3:
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.GlobalTotalHeight
                            + Constants.StrengthTotalHeight
                            + Constants.DexterityTotalHeight));
                    DrawIntelligenceInfo();
                    DrawArtificeInfo();
                    DrawResilienceInfo();
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.WisdomTotalHeight
                            + Constants.LuckTotalHeight));
                    break;
                case 4:
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.GlobalTotalHeight
                            + Constants.StrengthTotalHeight
                            + Constants.DexterityTotalHeight
                            + Constants.IntelligenceTotalHeight));
                    DrawArtificeInfo();
                    DrawResilienceInfo();
                    DrawWisdomInfo();
                    GUILayout.Label(GUIContent.none, GUILayout.Height(Constants.LuckTotalHeight));
                    break;
                case 5:
                    GUILayout.Label(GUIContent.none,
                        GUILayout.Height(
                            Constants.GlobalTotalHeight
                            + Constants.StrengthTotalHeight
                            + Constants.DexterityTotalHeight
                            + Constants.IntelligenceTotalHeight
                            + Constants.ArtificeTotalHeight));
                    DrawResilienceInfo();
                    DrawWisdomInfo();
                    DrawLuckInfo();
                    break;
            }

            GUILayout.EndVertical();
        }


        private int _drawRegion;
        private Color _infoButtonColor;
        private readonly GameData _d;

        private void DrawGlobalLevelInfo()
        {
            string n = "Global";
            var s = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.GlobalTotalHeight));
            Color colorOld = GUI.color;
            //GUI.color = Color.cyan;
            //GUILayout.Label("Global", GUIStyles.LabelStyle);
            //GUI.color = colorOld;

            // lvl data
            GUILayout.BeginHorizontal();

            int cLvl = _d.CurrentLvl - s.PendingRelicLevels - s.PendingRosaryLevels;
            int rosaries = PlayerData.instance.geo;
            int rosariesAfter = rosaries - s.PendingRosaries;
            int rosaryLvl = _d.RosaryLevel - s.PendingRosaryLevels;
            int spentRosaries = s.TotalSpentRosaries + s.PendingRosaries;
            int relicLvl = _d.CurrentLvl - _d.RosaryLevel;
            int relicsIn = relicLvl - s.PendingRelicLevels;
            int relicsLeft = _d.AvailableRelicLevels + s.PendingRelicLevels;

            Color afterColor = GUIStyles.LvlInfoGlobalLvlDataStyle.normal.textColor;
            float wbox = Constants.BoxWidth;
            float w = 320f;

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.GlobalBoxHeight), GUILayout.Width(wbox));

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Current Level: {cLvl}", GUIStyles.LvlInfoHeaderPrimaryStyle, GUILayout.Width(w));
            if (_d.CurrentLvl > cLvl)
            {
                // arrows symbol are: ‹ (\u2039) and › (\u203A) + a normal dash '-'. 
                // These fit well into normal text whereas using other fancier arrows tend to take more space 
                // height-wise and make it harder to apply consistent styling.
                GUI.color = afterColor;
                GUILayout.Label($" -› {_d.CurrentLvl}", GUIStyles.LvlInfoGlobalLvlDataStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Rosary Levels: {rosaryLvl}", GUIStyles.LvlInfoLabelStyle, GUILayout.Width(w));
            if (_d.RosaryLevel > rosaryLvl)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {_d.RosaryLevel}", GUIStyles.LvlInfoLabelStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Relic Levels: {relicsIn} ({relicsLeft} available)",
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(w));
            if (s.PendingRelicLevels > 0)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {relicLvl} ({_d.AvailableRelicLevels} available)", GUIStyles.LvlInfoLabelStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label(
                $"Rosaries: {PlayerData.instance.geo}", GUIStyles.LvlInfoHeaderPrimaryStyle, GUILayout.Width(w));
            if (rosariesAfter < rosaries)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {rosariesAfter}", GUIStyles.LvlInfoGlobalLvlDataStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Rosaries spent in total: {s.TotalSpentRosaries}",
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(w));
            if (spentRosaries > s.TotalSpentRosaries)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {spentRosaries}", GUIStyles.LvlInfoLabelStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.color = colorOld;

            // global bonuses
            bool multiplierUnlocked = GlobalStat.CheckGlobalMilestone(GlobalMilestone.CURRENCYMULTIPLIER);

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.GlobalBoxHeight));
            GUILayout.Label("Bonus", GUIStyles.LvlInfoGlobalBonusStyle);
            Components.InfoStatLabel($"Enemy Currency Drop Count: {_d.RosaryBaseIncrease}");
            Components.InfoStatLabel($"Enemy Currency Drop Multiplier: " +
                $"{(multiplierUnlocked ? _d.RosaryMultiplier : "- (locked)")}");
            GUILayout.Label("Active states", GUIStyles.LvlInfoGlobalBonusStyle);

            if (StrengthStat.NeedleStrikeAutoReleaseUnlocked)
                GUILayout.Label(_d.NeedleAutoChargeStatus, GUIStyles.LvlInfoLabelStyle);
            if (IntelligenceStat.WeaverAuraUnlocked)
                GUILayout.Label(_d.WeaverAuraStatus, GUIStyles.LvlInfoLabelStyle);
            if (IntelligenceStat.WeakBindUnlocked)
                GUILayout.Label(_d.WeakBindStatus, GUIStyles.LvlInfoLabelStyle);
            if (ResilienceStat.BlueMaskStateUnlocked)
                GUILayout.Label(_d.BlueStateStatus, GUIStyles.LvlInfoLabelStyle);
            if (WisdomStat.SilkNailsUnlocked)
                GUILayout.Label(_d.SilkNailsStatus, GUIStyles.LvlInfoLabelStyle);
            if (WisdomStat.CogflyWarriorUnlocked)
                GUILayout.Label(_d.CogflyWarriorStatus, GUIStyles.LvlInfoLabelStyle);
            if (LuckStat.PinWielderUnlocked)
                GUILayout.Label(_d.PinWielderStatus, GUIStyles.LvlInfoLabelStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // unlocks
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.GlobalMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Unlocks", GUIStyles.LvlInfoGlobalUnlockStyle);

            GUI.color = new Color(1f, 0.8f, 0.7f);
            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.CURRENCYBASE),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.CURRENCYBASE)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.BENCHTELEPORT),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.BENCHTELEPORT)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.QOLTOOLS),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.QOLTOOLS)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.8f, 0.7f, 1f);
            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.SHARDREFILL),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.SHARDREFILL)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.MAGGOTREPEL),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.MAGGOTREPEL)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.EXTRADOTDAMAGE),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.EXTRADOTDAMAGE)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(1f, 0.6f, 0.5f);
            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.CURRENCYMULTIPLIER),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.CURRENCYMULTIPLIER)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.SILKREGEN),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.SILKREGEN)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                LevelInfoContent.GetGlobalInfoContent(GlobalMilestone.RESPECFREE),
                LevelingSystem.HighLightStatMilestone(n, (int)GlobalMilestone.RESPECFREE)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.color = colorOld;

            GUILayout.EndVertical();
        }

        private void DrawStrengthInfo()
        {
            string n = "Strength";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.StrengthTotalHeight));
            GUILayout.Label("Strength", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.StrengthBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Needle Damage: " + _d.NeedleBaseDamage);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.StrengthMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // needle strike auto-release toggle
            GUI.enabled = StrengthStat.NeedleStrikeAutoReleaseUnlocked;

            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                _d.NeedleAutoChargeStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.AutoReleaseNeedleStrike = !Bonfire.Instance.Status.AutoReleaseNeedleStrike;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                MilestoneHeader(StrengthStat.StrengthMilestone(1)) + LevelInfoContent.StrengthMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(StrengthStat.StrengthMilestone(2)) + LevelInfoContent.StrengthMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(StrengthStat.StrengthMilestone(3)) + LevelInfoContent.StrengthMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawDexterityInfo()
        {
            string n = "Dexterity";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.DexterityTotalHeight));
            GUILayout.Label("Dexterity", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.DexterityBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Slash Speed Multiplier: " + _d.AttackSpeed);
            Components.InfoStatLabel("Crit Damage Bonus: " + _d.CritDamageBonus);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.DexterityMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            GUILayout.Label(
                MilestoneHeader(DexterityStat.DexterityMilestone(1)) + LevelInfoContent.DexterityMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(DexterityStat.DexterityMilestone(2)) + LevelInfoContent.DexterityMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(DexterityStat.DexterityMilestone(3)) + LevelInfoContent.DexterityMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawIntelligenceInfo()
        {
            string n = "Intelligence";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.IntelligenceTotalHeight));
            GUILayout.Label("Intelligence", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.IntelligenceBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Silk Skill Damage Bonus: " + _d.SpellDamageBonus);
            Components.InfoStatLabel("Bind Cost: " + _d.BindCost);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.IntelligenceBoxHeight));
            GUILayout.Label("Weaver's Light", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Damage: " + _d.WeaverAuraDamage);
            Components.InfoStatLabel("Range: " + _d.WeaverAuraRange);
            GUILayout.Label("Weak Bind", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Bola Damage: " + _d.WeakbindBolaDamage);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.IntelligenceMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // weaver aura/pale burst toggle
            GUI.enabled = IntelligenceStat.WeaverAuraUnlocked;

            var weaverAuraMode = (WeaverAura)Bonfire.Instance.Status.WeaverAuraMode;
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                _d.WeaverAuraStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.WeaverAuraMode = (int)RotateWeaverAuraState(weaverAuraMode);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.enabled = IntelligenceStat.WeakBindUnlocked;

            string weakBindToggle = $"Toggle Weak Bind: " +
                $"{(Bonfire.Instance.Status.WeakBindEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                weakBindToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.WeakBindEnabled = !Bonfire.Instance.Status.WeakBindEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                MilestoneHeader(IntelligenceStat.IntelligenceMilestone(1)) + LevelInfoContent.IntelligenceMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(IntelligenceStat.IntelligenceMilestone(2)) + LevelInfoContent.IntelligenceMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(IntelligenceStat.IntelligenceMilestone(3)) + LevelInfoContent.IntelligenceMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(IntelligenceStat.IntelligenceMilestone(4)) + LevelInfoContent.IntelligenceMilestone4,
                LevelingSystem.HighLightStatMilestone(n, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawArtificeInfo()
        {
            string n = "Artifice";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.ArtificeTotalHeight));
            GUILayout.Label("Artifice", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.ArtificeBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Tool Damage Bonus: " + _d.ToolDamageBonus);
            Components.InfoStatLabel(
                $"Tool Regen Interval: " +
                $"{(_d.ToolRegenInterval == "-" ? "- (requires 5 stat points)" : _d.ToolRegenInterval)}");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.ArtificeMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            GUILayout.Label(
                MilestoneHeader(ArtificeStat.ArtificeMilestone(1)) + LevelInfoContent.ArtificeMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(ArtificeStat.ArtificeMilestone(2)) + LevelInfoContent.ArtificeMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(ArtificeStat.ArtificeMilestone(3)) + LevelInfoContent.ArtificeMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(ArtificeStat.ArtificeMilestone(4)) + LevelInfoContent.ArtificeMilestone4,
                LevelingSystem.HighLightStatMilestone(n, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawResilienceInfo()
        {
            string n = "Resilience";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.ResilienceTotalHeight));
            GUILayout.Label("Resilience", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.ResilienceBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Lifeblood Masks: " + _d.ExtraMasks
                + $"{(_d.ExtraMasks == "8" ? " (max value reached)" : " (max value is 8)")}");
            Components.InfoStatLabel("Block Chance: " + _d.BlockChance);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.ResilienceBoxHeight));
            GUILayout.Label("Vengeful", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Reflect Damage: " + _d.ReflectDamage);
            Components.InfoStatLabel("Reflect Area: " + _d.ReflectArea);
            GUILayout.Label("Hollow", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Mask Damage Bonus: " + _d.GlassCannonMaskBonus);
            Components.InfoStatLabel("Milestone Bonus: " + _d.GlassCannonMilestoneBonus);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.ResilienceMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // reflect/glass cannon mode toggle
            GUI.enabled = ResilienceStat.BlueMaskStateUnlocked;

            var blueState = (BlueState)Bonfire.Instance.Status.BlueMaskState;

            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                _d.BlueStateStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.BlueMaskState = (int)RotateBlueState(blueState);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                MilestoneHeader(ResilienceStat.ResilienceMilestone(1)) + LevelInfoContent.ResilienceMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(ResilienceStat.ResilienceMilestone(2)) + LevelInfoContent.ResilienceMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(ResilienceStat.ResilienceMilestone(3)) + LevelInfoContent.ResilienceMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawWisdomInfo()
        {
            string n = "Wisdom";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.WisdomTotalHeight));
            GUILayout.Label("Wisdom", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.WisdomBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel($"Gain Silk After {_d.ExtraSilkOnHit} Hits");
            Components.InfoStatLabel($"Extra Silk in Spool " +
                $"{(_d.ExtraSilkTotal == "-" ? "- (requires 5 stat points)" : _d.ExtraSilkTotal)}");
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.WisdomBoxHeight));
            GUILayout.Label("Silk Nails", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Damage (single nail): " + _d.SilkNailsDamage);
            Components.InfoStatLabel($"Spawn Cooldown: {_d.SilkNailsSpawnCooldown} s");
            GUILayout.Label("Cogfly Warrior", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel("Damage: " + _d.CogflyDamage);
            Components.InfoStatLabel($"Attack Cooldown: {_d.CogflyAttackCooldown} s");
            GUILayout.Label("Wispfire Lantern", GUIStyles.LvlInfoHeaderSecondaryStyle);
            Components.InfoStatLabel($"Attack Cooldown: {_d.LanternAttackCooldown} s");
            Components.InfoStatLabel($"Spawn Delay: {_d.LanternSpawnCooldown} s");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.WisdomMilestoneHeight));
            GUILayout.BeginVertical();

            Color wisdomQoLOld = GUI.color;
            GUI.color = Color.yellow;
            GUILayout.Label(
                $"[Summons] => {(WisdomStat.BlockSummons ? "BLOCKED" : "ALLOWED")}\n",
                GUIStyles.LvlInfoLabelStyle);
            GUI.color = wisdomQoLOld;

            GUI.enabled = true;

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);
            Color bgOld = GUI.backgroundColor;

            // Silk Nails toggle
            GUI.enabled = WisdomStat.SilkNailsUnlocked;

            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                _d.SilkNailsStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.SilkNailsEnabled = !Bonfire.Instance.Status.SilkNailsEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            // Cogfly Warrior toggle
            GUI.enabled = WisdomStat.CogflyWarriorUnlocked;

            GUI.backgroundColor = new Color(0, 0.5f, 0.8f);
            if (CustomCursor.Button(
                _d.CogflyWarriorStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.CogflyWarriorEnabled = !Bonfire.Instance.Status.CogflyWarriorEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                MilestoneHeader(WisdomStat.WisdomMilestone(1)) + LevelInfoContent.WisdomMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(WisdomStat.WisdomMilestone(2)) + LevelInfoContent.WisdomMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(WisdomStat.WisdomMilestone(3)) + LevelInfoContent.WisdomMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(WisdomStat.WisdomMilestone(4)) + LevelInfoContent.WisdomMilestone4,
                LevelingSystem.HighLightStatMilestone(n, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawLuckInfo()
        {
            string n = "Luck";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.LuckTotalHeight));
            GUILayout.Label("Luck", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.LuckBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Crit Chance: " + _d.CritChance);
            GUILayout.EndVertical();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.LuckBoxHeight));
            GUILayout.Label("Crit chance bonuses", GUIStyles.LvlInfoHeaderSecondaryStyle);
            GUILayout.Label("G = global crit bonus", GUIStyles.LvlInfoLabelMilestone4Style);

            GUILayout.Label("Dexterity", GUIStyles.LvlInfoLabelStyle);
            GUILayout.Label(
                $"- Final milestone (G): {LuckStat.GetBonusCritChance(CritBonus.DEXTERITYFINALMILESTONE)}%\n",
                GUIStyles.LvlInfoLabelStyle);

            GUILayout.Label("Intelligence", GUIStyles.LvlInfoLabelStyle);
            GUILayout.Label(
                $"- Silk skills, Claw Mirror & Weaver's Light: " +
                $"{LuckStat.GetBonusCritChance(CritBonus.INTELLIGENMILESTONE3)}%\n",
                GUIStyles.LvlInfoLabelStyle);

            GUILayout.Label("Resilience", GUIStyles.LvlInfoLabelStyle);
            GUILayout.Label(
                $"- Vengeful/Hollow + Plasmified (G): " +
                $"{LuckStat.GetBonusCritChance(CritBonus.BLUESTATEPLASMIFIED, isGUI: true)}%\n",
                GUIStyles.LvlInfoLabelStyle);

            GUILayout.Label("Wisdom", GUIStyles.LvlInfoLabelStyle);
            GUILayout.Label(
                $"- Silk Nails: {LuckStat.GetBonusCritChance(CritBonus.SILKNAILS)}%\n",
                GUIStyles.LvlInfoLabelStyle);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.LuckMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            // Pinwielder Toggle
            GUI.enabled = LuckStat.PinWielderUnlocked;

            Color bgOld = GUI.backgroundColor;

            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                _d.PinWielderStatus,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.PinwielderEnabled = !Bonfire.Instance.Status.PinwielderEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                MilestoneHeader(LuckStat.LuckMilestone(1)) + LevelInfoContent.LuckMilestone1,
                LevelingSystem.HighLightStatMilestone(n, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(LuckStat.LuckMilestone(2)) + LevelInfoContent.LuckMilestone2,
                LevelingSystem.HighLightStatMilestone(n, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(LuckStat.LuckMilestone(3)) + LevelInfoContent.LuckMilestone3,
                LevelingSystem.HighLightStatMilestone(n, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                MilestoneHeader(LuckStat.LuckMilestone(4)) + LevelInfoContent.LuckMilestone4,
                LevelingSystem.HighLightStatMilestone(n, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
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

        private string MilestoneHeader(int req)
        {
            if (req == 1) return $"[{req} point]\n";

            return $"[{req} points]\n";
        }

        private int GetDrawRegion(float scrollHeight)
        {
            float offset = 50f;
            if (scrollHeight < Constants.GlobalTotalHeight + offset)
            {
                return 0;
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + offset)
            {
                return 1;
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + offset)
            {
                return 2;
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + Constants.IntelligenceTotalHeight
                + offset)
            {
                return 3;
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + Constants.IntelligenceTotalHeight
                + Constants.ArtificeTotalHeight
                + offset)
            {
                return 4;
            }

            return 5;
        }
    }
}