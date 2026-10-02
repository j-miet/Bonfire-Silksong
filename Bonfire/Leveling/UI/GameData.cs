namespace BonfireSilksong.Leveling.UI
{
    public class GameData
    {
        public int CurrentLvl { get; private set; }
        public int RosaryLevel { get; private set; }
        public int RosariesToLvlUp { get; private set; }
        public int AvailableRelicLevels { get; private set; }
        public bool GotFreeLevel { get; private set; }
        public string ApplyText { get; private set; }
        public string RespecText { get; private set; }

        public string TotalStr { get; private set; }
        public string TotalDex { get; private set; }
        public string TotalInt { get; private set; }
        public string TotalArt { get; private set; }
        public string TotalRes { get; private set; }
        public string TotalWsdm { get; private set; }
        public string TotalLuck { get; private set; }

        public string RosaryBaseIncrease { get; private set; }
        public string RosaryMultiplier { get; private set; }

        public string NeedleBaseDamage { get; private set; }

        public string AttackSpeed { get; private set; }
        public string CritDamageBonus { get; private set; }

        public string SpellDamageBonus { get; private set; }
        public string BindCost { get; private set; }
        public string WeakbindBolaDamage { get; private set; }
        public string WeaverAuraDamage { get; private set; }
        public string WeaverAuraRange { get; private set; }

        public string ToolDamageBonus { get; private set; }
        public string ToolRegenInterval { get; private set; }

        public string ExtraMasks { get; private set; }
        public string BlockChance { get; private set; }
        public string ReflectDamage { get; private set; }
        public string ReflectArea { get; private set; }
        public string GlassCannonMaskBonus { get; private set; }
        public string GlassCannonMilestoneBonus { get; private set; }

        public string ExtraSilkOnHit { get; private set; }
        public string ExtraSilkTotal { get; private set; }
        public string SilkNailsDamage { get; private set; }
        public string SilkNailsSpawnCooldown { get; private set; }
        public string CogflyDamage { get; private set; }
        public string CogflyAttackCooldown { get; private set; }
        public string LanternAttackCooldown { get; private set; }
        public string LanternSpawnCooldown { get; private set; }

        public string CritChance { get; private set; }

        public string NeedleAutoChargeStatus { get; private set; }
        public string WeaverAuraStatus { get; private set; }
        public string WeakBindStatus { get; private set; }
        public string BlueStateStatus { get; private set; }
        public string SilkNailsStatus { get; private set; }
        public string CogflyWarriorStatus { get; private set; }
        public string PinWielderStatus { get; private set; }

        public void UpdateGUIFields()
        {
            var s = Bonfire.Instance.Status;
            var ls = LevelingSystem.Instance;

            int lvl = LevelingSystem.GetCurrentLevel();
            CurrentLvl = lvl;

            if (!CacheUpdated(lvl))
                return;

            int strNew = s.StrengthStat + ls.StrengthIncrease;
            int dexNew = s.DexterityStat + ls.DexterityIncrease;
            int intNew = s.IntelligenceStat + ls.IntelligenceIncrease;
            int artNew = s.ArtificeStat + ls.ArtificeIncrease;
            int resNew = s.ResilienceStat + ls.ResilienceIncrease;
            int wsdmNew = s.WisdomStat + ls.WisdomIncrease;
            int luckNew = s.LuckStat + ls.LuckIncrease;

            AvailableRelicLevels = ls.GetAvailableRelicLevels();
            GotFreeLevel = AvailableRelicLevels > 0;

            RosaryLevel = LevelingSystem.DisplayPendingRosaryLevel();
            RosariesToLvlUp = LevelingSystem.GetLevelUpRosaryRequirement(RosaryLevel);

            // stat preview strings
            TotalStr = strNew.ToString();
            TotalDex = dexNew.ToString();
            TotalInt = intNew.ToString();
            TotalArt = artNew.ToString();
            TotalRes = resNew.ToString();
            TotalWsdm = wsdmNew.ToString();
            TotalLuck = luckNew.ToString();

            // stat formula strings
            // Global
            RosaryBaseIncrease = "+" + GlobalStat.IncreaseCurrencyBaseDrops(CurrentLvl).ToString();
            RosaryMultiplier = (GlobalStat.GetCurrencyMultiplier(lvl) * 100).ToString("F0") + "%";

            // Strength
            NeedleBaseDamage = StrengthStat.NeedleDamage(5 + 4 * PlayerData.instance.nailUpgrades, strNew).ToString();

            // Dexterity
            AttackSpeed = DexterityStat.AttackSpeed(dexNew).ToString();
            CritDamageBonus = (DexterityStat.CritDamage(dexNew, 100) - 100).ToString() + "%";

            // Intelligence
            SpellDamageBonus = (IntelligenceStat.SpellDamage(100, intNew) - 100).ToString() + "%";
            BindCost = IntelligenceStat.BindCost(intNew).ToString();
            WeakbindBolaDamage = IntelligenceStat.BolaDamage(intNew).ToString();
            WeaverAuraDamage = IntelligenceStat.WeaverAuraDamage(intNew, isGUI: true).ToString() +
                " (With Silk Bonus: " +
                $"{IntelligenceStat.WeaverAuraDamage(intNew, isGUI: true, GUISilkBonus: true)})";
            WeaverAuraRange = IntelligenceStat.WeaverAuraRange(intNew, isGUI: true).ToString();

            // Artifice
            ToolDamageBonus = (ArtificeStat.ToolDamage(100, artNew) - 100).ToString() + "%";
            ToolRegenInterval = "-";
            if (artNew > 0)
            {
                int toolRegen = ArtificeStat.ToolRegenInterval(artNew);
                ToolRegenInterval = toolRegen > 0 ? toolRegen.ToString() + " s" : "-";
            }

            // Resilience
            ExtraMasks = ResilienceStat.BlueMasks(resNew).ToString();
            BlockChance = resNew > 0 ? ResilienceStat.AverageBlockChance(resNew).ToString("F0") + "%" : "0%";
            ReflectDamage = "0";
            ReflectArea = "0";
            GlassCannonMaskBonus = "0%";
            GlassCannonMilestoneBonus = "0%";
            if (resNew > 0)
            {
                string reflectDamage = ResilienceStat.ReflectDamage(resNew, isGUI: true).ToString();
                string reflectArea = ResilienceStat.ReflectArea(isGUI: true).ToString();

                string ggMaskBonus = ResilienceStat.IsInBlueState(BlueState.GLASSCANNON)
                    ? (ResilienceStat.GlassCannonDamageMaskBonus(isGUI: true) * 100).ToString("F0") + "%"
                    : "0%";
                string ggMilestoneBonus = ResilienceStat.GetGlassCannonMilestoneBonus(resNew).ToString();

                if (ResilienceStat.CheckResilienceMilestone(3))
                {
                    if (ResilienceStat.IsInBlueState(BlueState.REFLECT))
                    {
                        reflectDamage += " (Plasmified: " + ResilienceStat.ReflectDamage(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            + ")";
                        reflectArea += " (Plasmified: " + ResilienceStat.ReflectArea(
                            isGUI: true, GUIPlasmified: true)
                            .ToString() + ")";
                    }
                    else if (ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
                    {
                        ggMaskBonus += " (Plasmified: "
                            + (ResilienceStat.GlassCannonDamageMaskBonus(isGUI: true, GUIPlasmified: true) * 100)
                            .ToString("F0") + "%)";
                        ggMilestoneBonus += " (Plasmified: " + ResilienceStat.GetGlassCannonMilestoneBonus(
                            s.ResilienceStat, isGUI: true, GUIPlasmified: true)
                            .ToString("F0") + ")";
                    }
                }

                ReflectDamage = reflectDamage;
                ReflectArea = reflectArea;
                GlassCannonMaskBonus = ggMaskBonus;
                GlassCannonMilestoneBonus = ggMilestoneBonus;
            }

            // Wisdom
            ExtraSilkOnHit = "-";
            ExtraSilkTotal = "-";
            if (wsdmNew > 0)
            {
                int silkCounterMax = WisdomStat.ExtraSilkThreshold(wsdmNew);
                ExtraSilkOnHit = silkCounterMax > 0 ? silkCounterMax.ToString() : "-";
                int spoolBonus = WisdomStat.SpoolExtraSilk(wsdmNew);
                ExtraSilkTotal = spoolBonus > 0 ? spoolBonus.ToString() : "-";
            }
            SilkNailsDamage = WisdomStat.SilkNailsDamage(wsdmNew).ToString();
            SilkNailsSpawnCooldown = WisdomStat.SilkNailsSpawnCooldown(wsdmNew).ToString("F2");
            CogflyDamage = WisdomStat.CogflyWarriorDamage(wsdmNew, isGUI: true).ToString();
            CogflyAttackCooldown = WisdomStat.CogflyWarriorAttackCooldown(wsdmNew).ToString("F2");

            (float lanternAttack, float lanternSpawn) = WisdomStat.WispLanternSpeed();
            LanternAttackCooldown = lanternAttack.ToString();
            LanternSpawnCooldown = lanternSpawn.ToString();

            // Luck
            CritChance = LuckStat.CritChance(luckNew).ToString() + "%";

            // "Apply" button label
            int pRelics = s.PendingRelicLevels;
            int pRosaries = s.PendingRosaries;
            if (pRelics > 0 && pRosaries > 0)
            {
                ApplyText = $"Apply \n({pRosaries} rosaries + {pRelics} {(pRelics > 1 ? "relics" : "relic")})";
            }
            else if (pRelics > 0)
            {
                ApplyText = $"Apply ({pRelics} relic {(pRelics > 1 ? "levels" : "level")})";
            }
            else if (GotFreeLevel)
            {
                ApplyText =
                    $"{AvailableRelicLevels} Free {(AvailableRelicLevels > 1 ? "Levels" : "Level")} available!";
            }
            else
            {
                ApplyText = $"Apply ({pRosaries} rosaries)";
            }

            // "Respec" button label
            int rCost = s.RespecCost;
            RespecText = $"Respec" + $"{(rCost <= 0 ? "" : $" ({rCost} {(rCost > 1 ? $"Silkeaters" : "Silkeater")})")}";

            // stat milestone status
            NeedleAutoChargeStatus = "Needle strike auto-release: " +
                $"{(Bonfire.Instance.Status.AutoReleaseNeedleStrike ? "ENABLED" : "DISABLED")}";

            var mode = (WeaverAura)Bonfire.Instance.Status.WeaverAuraMode;
            WeaverAuraStatus = "Cast Weaver's Light when taunting: " + GetWeaverAuraString(mode);

            WeakBindStatus = "Toggle Weak Bind: " +
                $"{(Bonfire.Instance.Status.WeakBindEnabled ? "ENABLED" : "DISABLED")}";

            var blueState = (BlueState)Bonfire.Instance.Status.BlueMaskState;
            BlueStateStatus = "Current blue mask state: " + GetBlueStateString(blueState);

            SilkNailsStatus = "Silk Nails: " +
                $"{(Bonfire.Instance.Status.SilkNailsEnabled ? "ENABLED" : "DISABLED")}";

            CogflyWarriorStatus = "Cogfly Warrior: " +
                $"{(Bonfire.Instance.Status.CogflyWarriorEnabled ? "ENABLED" : "DISABLED")}";

            PinWielderStatus = "Pinwielder: " +
                $"{(Bonfire.Instance.Status.PinwielderEnabled ? "ENABLED" : "DISABLED")}";
        }


        private int _cachedLvl = 0;
        private int _cachedPendingRosaryLevels = 0;
        private int _cachedRelicsObtained = 0;
        private int _cachedConsumedRelics = 0;
        private int _cachedRespecRelics = 0;
        private int _cachedRespecCost = 0;
        private bool _cacheAutoRelease = false;
        private int _cacheWeaverAuraMode = 0;
        private bool _cacheWeakBindEnabled = false;
        private int _cacheBlueMaskMode = 0;
        private bool _cacheSilkNailsEnabled = false;
        private bool _cacheCogflyWarriorEnabled = false;
        private bool _cachePinwielderEnabled = false;

        // this prevents unnecessary GUI data updates. Values to cache:
        // - current level -> this already handles almost everything
        // - pending states & respecs -> for applying levels & respecs, these update button texts
        // - milestone button states -> secondary fields might display actual values only when their respective state
        // is active, thus requiring a separate update.
        private bool CacheUpdated(int currentLevel)
        {
            var s = Bonfire.Instance.Status;
            bool updated = false;

            if (_cachedLvl != currentLevel)
            {
                _cachedLvl = currentLevel;
                updated = true;
            }

            if (_cachedRelicsObtained != LevelingSystem.Instance.CachedRelicsObtained)
            {
                _cachedRelicsObtained = LevelingSystem.Instance.CachedRelicsObtained;
                updated = true;
            }

            if (_cachedPendingRosaryLevels != s.PendingRosaryLevels)
            {
                _cachedPendingRosaryLevels = s.PendingRosaryLevels;
                updated = true;
            }

            if (_cachedConsumedRelics != s.ConsumedRelicLevels)
            {
                _cachedConsumedRelics = s.ConsumedRelicLevels;
                updated = true;
            }

            if (_cachedRespecRelics != s.RespecRelicLevels)
            {
                _cachedRespecRelics = s.RespecRelicLevels;
                updated = true;
            }

            if (_cachedRespecCost != s.RespecCost)
            {
                _cachedRespecCost = s.RespecCost;
                updated = true;
            }

            if (_cacheAutoRelease != s.AutoReleaseNeedleStrike)
            {
                _cacheAutoRelease = s.AutoReleaseNeedleStrike;
                updated = true;
            }

            if (_cacheWeaverAuraMode != s.WeaverAuraMode)
            {
                _cacheWeaverAuraMode = s.WeaverAuraMode;
                updated = true;
            }

            if (_cacheWeakBindEnabled != s.WeakBindEnabled)
            {
                _cacheWeakBindEnabled = s.WeakBindEnabled;
                updated = true;
            }

            if (_cacheBlueMaskMode != s.BlueMaskState)
            {
                _cacheBlueMaskMode = s.BlueMaskState;
                updated = true;
            }

            if (_cacheSilkNailsEnabled != s.SilkNailsEnabled)
            {
                _cacheSilkNailsEnabled = s.SilkNailsEnabled;
                updated = true;
            }

            if (_cacheCogflyWarriorEnabled != s.CogflyWarriorEnabled)
            {
                _cacheCogflyWarriorEnabled = s.CogflyWarriorEnabled;
                updated = true;
            }

            if (_cachePinwielderEnabled != s.PinwielderEnabled)
            {
                _cachePinwielderEnabled = s.PinwielderEnabled;
                updated = true;
            }

            return updated;
        }

        private static string GetWeaverAuraString(WeaverAura state)
        {
            switch (state)
            {
                case WeaverAura.AURA: return "ENABLED";
                case WeaverAura.BURST: return "VOID BURST";
                default: return "DISABLED";
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
    }
}