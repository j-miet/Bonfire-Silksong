namespace BonfireSilksong.Leveling.UI
{
    public static class LevelInfoContent
    {
        public static Localization Loc = Localization.Loc;

        public static string StrengthMilestone1 => Loc.Get("levelinfo.strength.milestone1");
        public static string StrengthMilestone2 => Loc.Get("levelinfo.strength.milestone2");
        public static string StrengthMilestone3 => Loc.Get("levelinfo.strength.milestone3");

        public static string DexterityMilestone1 => Loc.Get(
            "levelinfo.dexterity.milestone1",
            ("clawlinehitlimit", DexterityStat.ClawlineHitLimit));
        public static string DexterityMilestone2 => Loc.Get("levelinfo.dexterity.milestone2");
        public static string DexterityMilestone3 => Loc.Get(
            "levelinfo.dexterity.milestone3",
            ("dexteritycritbonus", LuckStat.CritBonusDexterityFinalMilestone));

        public static string IntelligenceMilestone1 => Loc.Get(
            "levelinfo.intelligence.milestone1",
            ("weaverauraringmultiplier", IntelligenceStat.WeaverAuraRingTauntDamageMultiplier));
        public static string IntelligenceMilestone2 => Loc.Get("levelinfo.intelligence.milestone2");
        public static string IntelligenceMilestone3 => Loc.Get(
            "levelinfo.intelligence.milestone3",
            ("intelligencecritbonus", LuckStat.CritBonusIntelligenceMilestone3));
        public static string IntelligenceMilestone4 => Loc.Get(
            "levelinfo.intelligence.milestone4",
            ("burstrangeincrease", IntelligenceStat.BurstRangeIncrease),
            ("burstdamagemultiplier", IntelligenceStat.BurstSilkDamageMultiplier));

        public static string ArtificeMilestone1 => Loc.Get("levelinfo.artifice.milestone1");
        public static string ArtificeMilestone2 => Loc.Get("levelinfo.artifice.milestone2");
        public static string ArtificeMilestone3 => Loc.Get("levelinfo.artifice.milestone3");
        public static string ArtificeMilestone4 => Loc.Get("levelinfo.artifice.milestone4");

        public static string ResilienceMilestone1 => Loc.Get("levelinfo.resilience.milestone1");
        public static string ResilienceMilestone2
        {
            get
            {
                float area = ResilienceStat.ReflectArea(isGUI: true);
                return Loc.Get(
                    "levelinfo.resilience.milestone2",
                    ("reflectarea", area),
                    ("extendedreflectarea", 2 * area));
            }
        }
        public static string ResilienceMilestone3 => Loc.Get(
            "levelinfo.resilience.milestone3",
            ("bluestateplasmifiedcritbonus", LuckStat.CritBonusBlueStatePlasmified));

        public static string WisdomMilestone1 => Loc.Get("levelinfo.wisdom.milestone1");
        public static string WisdomMilestone2 => Loc.Get("levelinfo.wisdom.milestone2");
        public static string WisdomMilestone3 => Loc.Get("levelinfo.wisdom.milestone3");
        public static string WisdomMilestone4 => Loc.Get(
            "levelinfo.wisdom.milestone4",
            ("silknailscritbonus", LuckStat.CritBonusSilkNails));

        public static string LuckMilestone1 => Loc.Get("levelinfo.luck.milestone1");
        public static string LuckMilestone2 => Loc.Get(
            "levelinfo.luck.milestone2",
            ("threefoldpinm3bonus", LuckStat.ThreefoldPinMilestone3DamageBonus));
        public static string LuckMilestone3 => Loc.Get(
            "levelinfo.luck.milestone3",
            ("threadstormduration", LuckStat.ThreadStormDuration));
        public static string LuckMilestone4 => Loc.Get(
            "levelinfo.luck.milestone4",
            ("threefoldpinm3bonus", LuckStat.ThreefoldPinMilestone3DamageBonus),
            ("threefoldpinm4bonus", LuckStat.ThreefoldPinMilestone4DamageBonus),
            ("threadstormduration", LuckStat.ThreadStormDuration),
            ("threadstormdurationextended", LuckStat.ThreadStormDurationExtended));

        public static string GetGlobalInfoContent(GlobalMilestone m)
        {
            var pd = PlayerData.instance;

            switch (m)
            {
                case GlobalMilestone.CURRENCYBASE:
                    return Loc.Get(
                        "levelinfo.global.currencybase",
                        ("extrarosarythreshold", GlobalStat.ExtraRosaryThreshold));

                case GlobalMilestone.BENCHTELEPORT:
                    return Loc.Get("levelinfo.global.benchteleport");

                case GlobalMilestone.QOLTOOLS:
                    return Loc.Get(
                        "levelinfo.global.qoltools",
                        ("qoltoollevelreq", GlobalStat.QoLToolsLevelRequirement));

                case GlobalMilestone.SHARDREFILL:
                    return Loc.Get(
                        "levelinfo.global.shardrefill",
                        ("shardrefilllevelreq", GlobalStat.ShardRefillLevelRequirement));

                case GlobalMilestone.MAGGOTREPEL:
                    return Loc.Get(
                        "levelinfo.global.maggotrepel",
                        ("mapcountreq", GlobalStat.MapCountRequirement),
                        ("mapcount", pd.MapCount));

                case GlobalMilestone.EXTRADOTDAMAGE:
                    return Loc.Get(
                        "levelinfo.global.extradotdamage",
                        ("dotdamagemultiplier", GlobalStat.DotDamageMultiplier));

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return Loc.Get(
                        "levelinfo.global.currencymultiplier",
                        ("currencybuffincrease", GlobalStat.RosaryPercentageIncrease * 100));

                case GlobalMilestone.SILKREGEN:
                    return Loc.Get(
                        "levelinfo.global.silkregen",
                        ("silkregeninterval", GlobalStat.SilkRegenInterval));

                case GlobalMilestone.RESPECFREE:
                    return Loc.Get("levelinfo.global.respecfree");

                default: return "";
            }
        }
    }
}