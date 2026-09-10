using UnityEngine;

using BonfireSilksong.GUIComponents;

namespace BonfireSilksong.Leveling.UI
{
    public class LevelInfo
    {
        public LevelInfo(GameData data)
        {
            _d = data;
            _infoButtonColor = new Color(0, 0.5f, 0.8f);
        }

        public void Draw(float scrollHeight)
        {
            float offset = 50f;
            GUILayout.BeginVertical();

            // these conditions check cursor position and render only some of the info boxes. While the current
            // implementation is very manual and has minor issues e.g. scrollbar nudging if dragged slowly, it's good 
            // to keep as it significantly reduces GUI rendering cost.
            if (scrollHeight < Constants.GlobalTotalHeight + offset)
            {
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
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + offset)
            {
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
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + offset)
            {
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
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + Constants.IntelligenceTotalHeight
                + offset)
            {
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
            }
            else if (scrollHeight <
                Constants.GlobalTotalHeight
                + Constants.StrengthTotalHeight
                + Constants.DexterityTotalHeight
                + Constants.IntelligenceTotalHeight
                + Constants.ArtificeTotalHeight
                + offset)
            {
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
            }
            else
            {
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
            }

            // DrawGlobalLevelInfo();
            // DrawStrengthInfo();
            // DrawDexterityInfo();
            // DrawIntelligenceInfo();
            // DrawArtificeInfo();
            // DrawResilienceInfo();
            // DrawWisdomInfo();
            // DrawLuckInfo();




            GUILayout.EndVertical();
        }


        private Color _infoButtonColor;
        private readonly GameData _d;

        private void DrawGlobalLevelInfo()
        {
            string s = "Global";
            var status = Bonfire.Instance.Status;

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.GlobalTotalHeight));
            Color colorOld = GUI.color;
            GUI.color = Color.cyan;
            GUILayout.Label("Global", GUIStyles.LabelStyle);
            GUI.color = colorOld;

            // lvl data
            GUILayout.BeginHorizontal();

            int cLvl = _d.CurrentLvl - status.PendingRelicLevels - status.PendingRosaryLevels;
            int rosaryLvl = _d.RosaryLevel - status.PendingRosaryLevels;
            int spentRosaries = status.TotalSpentRosaries + status.PendingRosaries;
            int relicLvl = _d.CurrentLvl - _d.RosaryLevel;
            int relicsIn = relicLvl - status.PendingRelicLevels;
            int relicsLeft = _d.AvailableRelicLevels + status.PendingRelicLevels;

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
            GUILayout.Label($"Rosaries spent in total: {status.TotalSpentRosaries}",
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(w));
            if (spentRosaries > status.TotalSpentRosaries)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {spentRosaries}", GUIStyles.LvlInfoLabelStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Relic Levels: {relicsIn} ({relicsLeft} available)",
                GUIStyles.LvlInfoLabelStyle,
                GUILayout.Width(w));
            if (status.PendingRelicLevels > 0)
            {
                GUI.color = afterColor;
                GUILayout.Label($" -› {relicLvl} ({_d.AvailableRelicLevels} available)", GUIStyles.LvlInfoLabelStyle);
                GUI.color = colorOld;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.color = colorOld;

            // global bonuses
            GUILayout.BeginVertical("box", GUILayout.Height(Constants.GlobalBoxHeight));
            GUILayout.Label("Bonus", GUIStyles.LvlInfoGlobalBonusStyle);
            Components.InfoStatLabel($"Rosary & Shell Shard Drop Count: {_d.RosaryBaseIncrease}\n");
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // unlocks
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.GlobalMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Unlocks", GUIStyles.LvlInfoGlobalUnlockStyle);

            GUI.color = new Color(0.9f, 0.9f, 0.9f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.CURRENCYBASE)}\n" +
                "Increases the base amount of rosary beads and shell shards dropped by enemies.\n" +
                $"Starts at +1 and gains another +1 every {GlobalStat.ExtraRosaryThreshold} levels.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.CURRENCYBASE)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(1f, 0.8f, 0.7f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.BENCHTELEPORT)}\n" +
                "Unlocks access to bench fast travel.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.BENCHTELEPORT)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.7f, 1f, 0.9f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.QOLTOOLS)}\n" +
                "The effects of the \"Compass\" and \"Magnetite Brooch\" are always active once you obtain the " +
                "corresponding tool.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.QOLTOOLS)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.8f, 0.7f, 1f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.SHARDREFILL)}\n" +
                "Shell Shards are fully replenished during bench visits.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.SHARDREFILL)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.3f, 0.8f, 0.7f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.CURRENCYMULTIPLIER)}\n" +
                $"All enemies drop {GlobalStat.CurrencyMultiplier}x rosaries and shell shards.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.CURRENCYMULTIPLIER)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.6f, 0.9f, 0.6f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.MAGGOTREPEL)}\n" +
                "\"Wreath of Purity\" effect is always active once you obtain the tool.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.MAGGOTREPEL)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(1f, 0.6f, 0.5f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.SILKREGEN)}\n" +
                $"Passively regenerate 1 silk every {GlobalStat.SilkRegenInterval} seconds.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.SILKREGEN)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUI.color = new Color(0.7f, 0.6f, 0.4f);
            GUILayout.Label(
                $"{GlobalStat.GetGlobalMilestoneString(GlobalMilestone.RESPECFREE)}\n" +
                "Respec cost removed.",
                LevelingSystem.HighLightStatMilestone(s, (int)GlobalMilestone.RESPECFREE)
                    ? GUIStyles.LvlInfoLabelStyle
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUI.color = colorOld;

            GUILayout.EndVertical();
        }

        private void DrawStrengthInfo()
        {
            string s = "Strength";

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
            GUI.enabled = StrengthStat.CheckStrengthMilestone(1);

            string autoStrikeToggle = "Needle strike auto-release: " +
                $"{(Bonfire.Instance.Status.AutoReleaseNeedleStrike ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                autoStrikeToggle,
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
                $"[{StrengthStat.StrengthMilestone(1)} points]\n" +
                "Needle strike: active charge time reduced by 20%. " +
                "Can toggle on/off automatic release when fully charged.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthStat.StrengthMilestone(2)} points]\n" +
                "Needle range increased by 33% and its initial charge time reduced by 20%.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{StrengthStat.StrengthMilestone(3)} points]\n" +
                "- Flintslate and Flea Brew tools have infinite buff duration.\n" +
                "- Downslash damage increased by 100%.\n" +
                "- Needle strike: both initial and active charge time reduction buffed (20% -> 40%).",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawDexterityInfo()
        {
            string s = "Dexterity";

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
                $"[{DexterityStat.DexterityMilestone(1)} points]\n" +
                "Clawline damage increased by 25%. " +
                $"Every {DexterityStat.ClawlineHitLimit}th hit from Clawline deals 2x damage " +
                "(default Clawline use counts as 2 hits).",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityStat.DexterityMilestone(2)} points]\n" +
                "Clawline silk cost removed, can now critically hit.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{DexterityStat.DexterityMilestone(3)} points]\n" +
                "All critical hits now heal 1 mask.\n" +
                "\n" +
                "Clawline:\n" +
                " - damage bonus increased (25% -> 50%).\n" +
                " - 7th hit will always heal 1 mask and casts 'Silk Spear' silk skill without costing silk.\n" +
                "   Skill scales normally from silk skill damage bonuses.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawIntelligenceInfo()
        {
            string s = "intelligence";

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
            GUI.enabled = IntelligenceStat.CheckIntelligenceMilestone(1);

            var mode = (WeaverAura)Bonfire.Instance.Status.WeaverAuraMode;
            string weaverAuraToggle = "Cast Weaver's Light when taunting: " + GetWeaverAuraString(mode);
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                weaverAuraToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.WeaverAuraMode = (int)RotateWeaverAuraState(mode);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.enabled = IntelligenceStat.CheckIntelligenceMilestone(2);

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
                $"[{IntelligenceStat.IntelligenceMilestone(1)} point]\n" +
                "Toggle on/off \"Weaver's Light\".\n" +
                "\n" +
                "<Weaver's Light>\n" +
                "- Taunt now enables an aura around you which flashes every second, consuming 1 silk and " +
                "dealing damage to enemies inside it. Damage and range increases with Intelligence stat.\n" +
                "- If you taunt with Throwing Rings: aura will instead cost 2 silk per second and has its range " +
                "reduced by 50%, but you get +3 extra damage from each active silk in spool.\n" +
                "- Taunt no longer grants the 50% damage bonus for next hit, however its initial cost is also" +
                "removed. Taunting again disables active aura effect, meaning you can use it tactically" +
                "without needing to wait until all your silk is depleted.\n" +
                "- Equipping Egg of Flealia reduces aura silk cost by 1 when on max hp.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceStat.IntelligenceMilestone(2)} points]\n" +
                "Toggle on/off \"Weak Bind\".\n" +
                "\n" +
                "<Weak Bind>\n" +
                "- Bind heals 1 mask and costs 1/3 of current cost, but keeps its current speed,\n" +
                "- You take + 1 damage from everything, but get the following bonuses:\n" +
                "  - Silk Skills: Each hit instance spawns Voltvessels bola projectile on top of enemy. Bolas have a " +
                "custom, flat intelligence damage scaling which can only benefit from global damage buffs. " +
                "This effect also disables normal use of Voltvessels tool.\n" +
                "  - Claw Mirror: procs additional time (similar to Multibinder effect), binding speed increased " +
                "by 33%.\n" +
                "  - Weaver's Light: gives 1 silk if at least one enemy got hit by the aura.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceStat.IntelligenceMilestone(3)} points]\n" +
                "Silk skills, Claw Mirror and Taunt can crit. Equipping Claw Mirror increases binding speed by 33%.",
            LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{IntelligenceStat.IntelligenceMilestone(4)} points]\n" +
                "Give more effects to all Intelligence-focused abilities:\n" +
                "- Silk skills: silk cost reduced by 1.\n" +
                "- Claw Mirror: every 2 points in Intelligence adds +1 damage. Damage is applied after Artifice stat " +
                "scaling.\n" +
                "- Weaver's Light: Gains a new \"Void Burst\" toggle state. Void Burst makes taunt a single-use " +
                "ability, consuming all silk when used while giving following benefits: \n" +
                "  - Range increased by 5.\n" +
                "  - Each silk in spool increases damage by +6.\n" +
                "\n" +
                "Taunting with Throwing Rings has no additional effects when Void Burst is enabled.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawArtificeInfo()
        {
            string s = "artifice";

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
                $"[{ArtificeStat.ArtificeMilestone(1)} points]\n" +
                "Pimpillo: has 2x tool capacity and can apply critical hits.\n" +
                "Sawtooth Circlet: can apply critical hits.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeStat.ArtificeMilestone(2)} points]\n" +
                "Rosary Cannon: has infinite, free refills -> every time you begin the use animation, it becomes " +
                "fully loaded.\n" +
                "Silkshot: silk cost removed, can apply critical hits.\n" +
                "Sawtooth Circlet: gives 1 silk from first enemy hit.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeStat.ArtificeMilestone(3)} points]\n" +
                "Rosary Cannon: +2 flat damage which is applied before Artifice stat damage bonus.\n" +
                "Silkshot: has infinite charges.\n" +
                "Sawtooth Circlet: horizontal range increased by 50%.\n" +
                "Pimpillo: casts \"Rune Rage\" silk skill on top of enemy on hit without costing silk. Skill scales " +
                "normally from any silk skill damage buffs.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ArtificeStat.ArtificeMilestone(4)} points]\n" +
                "Offensive red tool has 33% chance to spawn a copy of itself when used. " +
                "When a copy is spawned, neither original nor copy cost charges.\n" +
                "Following tools cannot benefit from this: Silkshot, Delver's Drill, Rosary Cannon, " +
                "Voltvessels Spear (Bolas do work), Snare Setter.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawResilienceInfo()
        {
            string s = "resilience";

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
            GUI.enabled = ResilienceStat.CheckResilienceMilestone(1);

            var mode = (BlueState)Bonfire.Instance.Status.BlueMaskMode;

            string blueModeToggle = "Current blue mask state: " + GetBlueStateString(mode);
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                blueModeToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.BlueMaskMode = (int)RotateBlueState(mode);
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            GUI.backgroundColor = bgOld;

            GUILayout.Label(
                $"[{ResilienceStat.ResilienceMilestone(1)} point]\n" +
                "You can now select \"Vengeful\" or \"Hollow\" blue mask state.\n" +
                "\n" +
                "When Vengeful/Hollow is active:\n" +
                "- Blue masks effectively replace normal masks: if you have no blue hp left, the next hit will " +
                "inflict \"default damage x 10\".\n" +
                "- All damage taken is reduced by 1, but must always deal at least 1 damage.\n" +
                "- You can enter Plasmified state only after unlocking the final Resilience milestone. Plasmified " +
                "refers to blue health regen state which is normally triggered after getting 9 blue masks.\n" +
                "- Egg of Flealia gives silk cost reduction only if your active blue mask count is greater than or " +
                "equal to current mask limit from Resilience. So if Resilience gives 3 masks and you active mask " +
                "is to 2 or less, cost reduction no longer applies. Be aware this is no longer true while in " +
                "Plasmified state as max hp is again equal to normal mask count and ignores Resilience bonus masks " +
                "entirely.\n" +
                "=> Both playstyles are quite risky to play early, but Vengeful will lean more towards the tanky/" +
                "low-risk, but one-dimensional playstyle whereas with Hollow you want to minimize hits taken at all " +
                "cost to keep huge damage bonus benefits.\n" +
                "\n" +
                "<Vengeful>\n " +
                "- Reflect damage back to enemies around you when you take damage. All your non-reflect damage is " +
                "reduced by 99%, but deals always at least 1 damage. Only exception to this rule is the \"Memory " +
                "Crystal\" blue tool which deals its full damage.\n" +
                "- Reflect base damage is 5. Damage scales from 1. Resilience stat extra blue masks count (max 8) " +
                "and 2. mask upgrades (max 5).\n" +
                "- Bind no longer heals normal masks, but instead 1 blue mask.\n" +
                "- You get 1 silk after getting hit by an enemy.\n" +
                "=> Low-risk playstyle: you can become very tanky, but are also limited to one-dimensional " +
                "offensive gameplay of ramming your body at enemies.\n" +
                "\n" +
                "<Hollow>\n" +
                "- Bind can no longer be used at all.\n" +
                "- You can no longer benefit from stat primary damage bonuses " +
                "(Strength, Dexterity, Intelligence, Artifice) or Luck crit chance, except during Plasmified state " +
                "having unlocked final milestone. Bonuses from milestones work normally.\n" +
                "- Damage from all sources is increased by 50%. Each active blue mask in hp bar gives 7% extra damage" +
                "additively while mask upgrades give 8% instead.\n" +
                "- All non-Resilience stat milestones require 3 less points to become active. You must still have at " +
                "least 1 point in a stat to activate any of its milestones.\n" +
                "=> High-risk playstyle as healing is pretty much non-existent, but gives a universal damage boost " +
                "and allows reaching non-Resilience milestones with fewer stat points.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceStat.ResilienceMilestone(2)} points]\n" +
                "Additional buffs to blue health modes:\n" +
                "<Vengeful> Bind now heals 2 blue masks, reflect damage area increased by 1.\n" +
                "<Hollow> You get flat +10 damage bonus which is applied after mask bonuses. Milestone bonus " +
                "increased to 5.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{ResilienceStat.ResilienceMilestone(3)} points]\n" +
                "While in Vengful/Hollow state, you can now enter Plasmified state and Plasmium Phial will instantly " +
                "trigger Plasmified when used. While Plasmified, your use of Plasmium Phial is again blocked but " +
                "you get a more offensive variant of current state:\n" +
                "\n" +
                "<Vengeful>\n" +
                "- Each active blue mask in hp bar now also gives additional +10 reflect damage.\n" +
                "- Reflect damage can crit.\n" +
                "- Reflect area increased by 2.\n" +
                "- Penalty: you take normal amount of damage again and bind heals only the default amount of 1 during" +
                "Plasmification.\n" +
                "\n" +
                "<Hollow>\n" +
                "- Active blue mask bonus increased (7% -> 20%).\n" +
                "- Crit change and crit damage can now be applied normally.\n" +
                "- Milestone bonus increased to 10.\n" +
                "- Penalty: you take normal amount of damage + additional 2 masks (1 -> 3, 2 -> 4 and so on).\n" +
                "\n" +
                "Your max hp in Plasmified state is equal to your normal mask count. Each active mask is counted " +
                "into blue mask damage bonus while Vengeful/Hollow state is active.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawWisdomInfo()
        {
            string s = "wisdom";

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
            GUI.enabled = WisdomStat.CheckWisdomMilestone(1) && !WisdomStat.BlockSummons;

            string silkNailsToggle = "Silk Nails: " +
                $"{(Bonfire.Instance.Status.SilkNailsEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                silkNailsToggle,
                GUIStyles.LvlInfoButtonStyle,
                GUILayout.Width(Constants.InfoButtonWidth),
                GUILayout.Height(Constants.InfoButtonHeight)))
            {
                Bonfire.Instance.Status.SilkNailsEnabled = !Bonfire.Instance.Status.SilkNailsEnabled;
            }
            GUI.enabled = true;
            GUILayout.Space(5);

            // Cogfly Warrior toggle
            GUI.enabled = WisdomStat.CheckWisdomMilestone(2) && !WisdomStat.BlockSummons;

            string cogflyWarriorToggle = "Cogfly Warrior: " +
                $"{(Bonfire.Instance.Status.CogflyWarriorEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = new Color(0, 0.5f, 0.8f);
            if (CustomCursor.Button(
                cogflyWarriorToggle,
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
                $"[{WisdomStat.WisdomMilestone(1)} point]\n" +
                "- Toggle on/off \"Silk Nails\" which is a weaker version of Pale Nails silk skill.\n" +
                "\n" +
                "<Silk Nails>\n" +
                "- Has its own damage and spawn cooldown scaling from Wisdom.\n" +
                "- Spawns automatically when enemies are near.\n" +
                "- Doesn't have a silk cost.\n" +
                "- While Silk Nails is enabled, you can no longer use Pale Nails silk skill.\n" +
                "\n" +
                "Additionally: you can now toggle your summons on/off using Needolin. This will also disable the " +
                "GUI toggle buttons.\n" +
                "- These are counted as summons: Wispfire Lantern, Silk Nails, Cogfly Warrior.\n" +
                "- Hornet's texture displays a brief flash when Needolin animation starts, this signals the toggle " +
                "was succesful.\n" +
                "- This setting will not be saved between sessions and starts always in \"ALLOWED\" state. This " +
                "means if you close the game and relaunch it, the first Needolin toggle will always switch to " +
                "\"BLOCKED\".\n" +
                "=> This feature is meant to serve as a quality of life during platforming sections where bouncing " +
                "off of enemies could become very difficult with summons hitting them.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomStat.WisdomMilestone(2)} points]\n" +
                "Toggle on/off \"Cogfly Warrior\" which is a stronger version of Cogfly.\n" +
                "\n" +
                "<Cogfly Warrior>\n" +
                "- Has its own damage and attack cooldown scaling from Wisdom.\n" +
                "- Has no max hit limit, meaning the minion will never die + it auto-summons itself if some game " +
                "event ends up destroying it (e.g. cutscenes).\n" +
                "- Total cogfly amount limited to 1, final milestone increases this by 1.\n" +
                "- While Cogfly Warrior is enabled, you can no longer use Cogfly tool.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomStat.WisdomMilestone(3)} points]\n" +
                "Wispfire Lantern silk cost removed, attack delay halved and spawn cooldown is down to 2 seconds.\n" +
                "=> Wispfire Lantern has no Wisdom stat scaling, only this and the next milestone buffs it.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{WisdomStat.WisdomMilestone(4)} points]\n" +
                "All of Wisdom's offensive abilities buffed:\n" +
                "- Silk Nails: Can now critically hit.\n" +
                "- Cogfly Warrior: summons a second cogfly.\n" +
                "- Wispfire Lantern: attack cooldown down to 1/3 of original, spawns a wisp every second.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawLuckInfo()
        {
            string s = "Luck";

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.LuckTotalHeight));
            GUILayout.Label("Luck", GUIStyles.LabelStyle);

            // stats
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical("box", GUILayout.Height(Constants.LuckBoxHeight));
            GUILayout.Label("Primary", GUIStyles.LvlInfoHeaderPrimaryStyle);
            Components.InfoStatLabel("Crit Chance: " + _d.CritChance);
            Components.InfoStatLabel("Extra Currency From Enemies: " + _d.RosaryCountIncrease);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // milestones
            GUILayout.BeginHorizontal("box", GUILayout.Height(Constants.LuckMilestoneHeight));
            GUILayout.BeginVertical();

            GUILayout.Label("Milestones", GUIStyles.LvlInfoHeaderMilestoneStyle);

            // Pinwielder Toggle
            GUI.enabled = LuckStat.CheckLuckMilestone(1);

            Color bgOld = GUI.backgroundColor;

            string pinwielderToggle = "Pinwielder: " +
                $"{(Bonfire.Instance.Status.PinwielderEnabled ? "ENABLED" : "DISABLED")}";
            GUI.backgroundColor = _infoButtonColor;
            if (CustomCursor.Button(
                pinwielderToggle,
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
                $"[{LuckStat.LuckMilestone(1)} points]\n" +
                "Can toggle \"Pinwielder\" on/off. All of the Luck milestone bonuses require this state to " +
                "be active.\n" +
                "\n" +
                "<Pinwielder>\n" +
                "- Focuses on 3 pin tools: Straight Pin, Threefold Pin and Longpin.\n" +
                "- Has infinite ranged attack, but with very low damage output in early game. Relies a lot on " +
                "higher crit chance, milestone bonuses and buffs from other stat trees. Later on with final " +
                "milestones, high crit chance and various other synergies, damage output will be very high.\n" +
                "- Later milestones will also utilize Thread Storm silk skill:\n" +
                "\n" +
                "=> Pinwielder's Thread Storm effect:\n" +
                "- Casts the Thread Storm silk skill normally except it spawns on top of enemy. Skill scales from " +
                "silk skill damage bonuses.\n" +
                "- Only one instance of Thread Storm can be active at a time: effect cannot be re-triggered until " +
                "previous one ends.\n" +
                "- You can no longer cast Thread Storm skill normally.\n" +
                "\n" +
                "# Initial bonuses #\n" +
                "- Straight Pin: has infinite charges, but deals 1/4 of its normal damage (rounded up to the next " +
                "integer value)\n" +
                "- Threefold Pin & Longpin: can now critically hit.",
                LevelingSystem.HighLightStatMilestone(s, 1)
                    ? GUIStyles.LvlInfoLabelMilestone1Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{LuckStat.LuckMilestone(2)} points]\n" +
                "Straight Pin: can now critically hit + crits give 1 silk.\n" +
                "Threefold Pin: crits get +8 damage which is applied before Dexterity crit multiplier.",
                LevelingSystem.HighLightStatMilestone(s, 2)
                    ? GUIStyles.LvlInfoLabelMilestone2Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{LuckStat.LuckMilestone(3)} points]\n" +
                "Longpin: casts Thread Storm on crit. " +
                $"Effect lasts for {LuckStat.ThreadStormDuration} seconds.",
                LevelingSystem.HighLightStatMilestone(s, 3)
                    ? GUIStyles.LvlInfoLabelMilestone3Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.Label(
                $"[{LuckStat.LuckMilestone(4)} points]\n" +
                "- Straight Pin: crit casts Thread Storm with effect duration of " +
                $"{LuckStat.ThreadStormDuration} seconds.\n" +
                "- Threefold Pin: crit damage bonus buffed (+8 -> +30).\n" +
                "- Longpin: now always casts Thread Storm on enemy hit (no crit required), effect lasts " +
                $"{LuckStat.ThreadStormDurationExtended} seconds.",
                LevelingSystem.HighLightStatMilestone(s, 4)
                    ? GUIStyles.LvlInfoLabelMilestone4Style
                    : GUIStyles.LvlInfoLabelDisabledStyle);

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }


        public static bool LevelingColorsInitialized;
        public static Color InfoButtonColor;

        private static string GetWeaverAuraString(WeaverAura state)
        {
            switch (state)
            {
                case WeaverAura.AURA: return "ENABLED";
                case WeaverAura.BURST: return "VOID BURST";
                default: return "DISABLED";
            }
        }

        private static WeaverAura RotateWeaverAuraState(WeaverAura state)
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

        private static string GetBlueStateString(BlueState state)
        {
            switch (state)
            {
                case BlueState.REFLECT: return "VENGEFUL";
                case BlueState.GLASSCANNON: return "HOLLOW";
                default: return "DISABLED";
            }
        }

        private static BlueState RotateBlueState(BlueState state)
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