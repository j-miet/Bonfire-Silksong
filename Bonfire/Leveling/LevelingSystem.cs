using System;
using System.Linq;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong.Leveling
{
    /// <summary>
    /// High-level leveling system logic. Knows only the numerical values of each stat, but not their underlying logic.
    /// </summary>
    public class LevelingSystem : MonoBehaviour
    {
        public const int MaxStatLevel = 99;

        public static int MilestoneBonus = 0;

        public int CachedRelicsObtained => _cachedRelicsDeposited;
        public static LevelingSystem Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        public int StrengthIncrease = 0;
        public int DexterityIncrease = 0;
        public int IntelligenceIncrease = 0;
        public int ArtificeIncrease = 0;
        public int ResilienceIncrease = 0;
        public int WisdomIncrease = 0;
        public int LuckIncrease = 0;

        /// <summary>
        /// For highlighting stat milestones in LevelingGUI.
        /// </summary>
        public static bool HighLightStatMilestone(string statName, int m)
        {
            string normalized = statName.ToLower();

            switch (normalized)
            {
                case "strength":
                    return (Bonfire.Instance.Status.StrengthStat
                        + Instance.StrengthIncrease
                        + MilestoneBonus
                        >= StrengthStat.StrengthMilestone(m)) &&
                        (Bonfire.Instance.Status.StrengthStat + Instance.StrengthIncrease >= 1);

                case "dexterity":
                    return (Bonfire.Instance.Status.DexterityStat
                        + Instance.DexterityIncrease
                        + MilestoneBonus
                        >= DexterityStat.DexterityMilestone(m)) &&
                        (Bonfire.Instance.Status.DexterityStat + Instance.DexterityIncrease >= 1); ;

                case "intelligence":
                    return (Bonfire.Instance.Status.IntelligenceStat
                        + Instance.IntelligenceIncrease
                        + MilestoneBonus
                        >= IntelligenceStat.IntelligenceMilestone(m)) &&
                        (Bonfire.Instance.Status.IntelligenceStat + Instance.IntelligenceIncrease >= 1);

                case "artifice":
                    return (Bonfire.Instance.Status.ArtificeStat
                        + Instance.ArtificeIncrease
                        + MilestoneBonus
                        >= ArtificeStat.ArtificeMilestone(m)) &&
                        (Bonfire.Instance.Status.ArtificeStat + Instance.ArtificeIncrease >= 1);

                case "resilience":
                    return (Bonfire.Instance.Status.ResilienceStat
                        + Instance.ResilienceIncrease
                        + MilestoneBonus
                        >= ResilienceStat.ResilienceMilestone(m)) &&
                        (Bonfire.Instance.Status.ResilienceStat + Instance.ResilienceIncrease >= 1);

                case "wisdom":
                    return (Bonfire.Instance.Status.WisdomStat
                        + Instance.WisdomIncrease
                        + MilestoneBonus
                        >= WisdomStat.WisdomMilestone(m)) &&
                        (Bonfire.Instance.Status.WisdomStat + Instance.WisdomIncrease >= 1);

                case "luck":
                    return (Bonfire.Instance.Status.LuckStat
                        + Instance.LuckIncrease
                        + MilestoneBonus
                        >= LuckStat.LuckMilestone(m)) &&
                        (Bonfire.Instance.Status.LuckStat + Instance.LuckIncrease >= 1);

                case "global":
                    return GlobalStat.CheckGlobalMilestone((GlobalMilestone)m);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Resets all initialization flags when in main menu.
        /// </summary>
        public static void ResetInitFlags()
        {
            if (GameManager.instance == null) return;

            if (GameManager.instance.GameState == GameState.MAIN_MENU)
            {
                IntelligenceStat.BindCostInit = false;
                BlueHealthUpdate.BlueHpInit = false;
                Bonfire.Instance.SessionInitialized = false;
            }
        }

        public static bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
        }

        public static int GetCurrentLevel()
        {
            var s = Bonfire.Instance.Status;

            return s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                + s.PendingRelicLevels
                + s.PendingRosaryLevels
                + 1; // base level
        }

        public static int DisplayPendingRosaryLevel()
        {
            var s = Bonfire.Instance.Status;
            return s.TotalRosaryLevels + s.PendingRosaryLevels;
        }

        public static int GetLevelUpRosaryRequirement(int rosaryLevels)
        {
            return (int)(Math.Pow(rosaryLevels, 2.0) + (10 * rosaryLevels) + 50.0);
        }

        /// <summary>
        /// Increments the pending allocation for a single stat.
        /// </summary>
        public void IncreaseStat(string stat, PlayerStatus s, int rosariesToLvlUp)
        {
            if (stat == "Strength") StrengthIncrease++;
            else if (stat == "Dexterity") DexterityIncrease++;
            else if (stat == "Intelligence") IntelligenceIncrease++;
            else if (stat == "Artifice") ArtificeIncrease++;
            else if (stat == "Resilience") ResilienceIncrease++;
            else if (stat == "Wisdom") WisdomIncrease++;
            else LuckIncrease++;

            if (!CheckStatLevels()) return;

            int relicsObtained = _cachedRelicsDeposited;
            if (relicsObtained < 0)
            {
                Bonfire.Log.LogError("Failed to fetch relic data");
                return;
            }

            int availableRelicLevels = GetAvailableRelicLevels();

            if (availableRelicLevels > 0)
            {
                s.PendingRelicLevels++;
            }
            else
            {
                s.PendingRosaries += rosariesToLvlUp;
                s.PendingRosaryLevels++;
            }
        }

        /// <summary>
        /// Commits the pending stat allocations, deducting rosaries and relic levels.
        /// </summary>
        public void ApplyLevel()
        {
            var s = Bonfire.Instance.Status;

            int fromRespecPool = Math.Min(s.PendingRelicLevels, s.RespecRelicLevels);
            int fromNewDeposits = s.PendingRelicLevels - fromRespecPool;
            if (s.PendingRosaries == 0 && fromRespecPool == 0 && fromNewDeposits == 0) return;

            s.RespecRelicLevels -= fromRespecPool;
            s.ConsumedRelicLevels += fromNewDeposits;
            s.PendingRelicLevels = 0;

            HeroController.instance.TakeGeo(s.PendingRosaries);
            s.TotalSpentRosaries += s.PendingRosaries;
            s.PendingRosaries = 0;

            s.TotalRosaryLevels += s.PendingRosaryLevels;
            s.PendingRosaryLevels = 0;

            s.StrengthStat += StrengthIncrease;
            s.DexterityStat += DexterityIncrease;
            s.IntelligenceStat += IntelligenceIncrease;
            s.ArtificeStat += ArtificeIncrease;
            s.ResilienceStat += ResilienceIncrease;
            s.WisdomStat += WisdomIncrease;
            s.LuckStat += LuckIncrease;

            Bonfire.Log.LogDebug("[LevelingSystem] Level up applied, stats increased: " +
                $"Str: {StrengthIncrease}, Dex: {DexterityIncrease}, Int: {IntelligenceIncrease}, " +
                $"Art: {ArtificeIncrease}, Res: {ResilienceIncrease}, Wsdm: {WisdomIncrease}, Luck: {LuckIncrease}");

            StrengthIncrease = 0;
            DexterityIncrease = 0;
            IntelligenceIncrease = 0;
            ArtificeIncrease = 0;
            ResilienceIncrease = 0;
            WisdomIncrease = 0;
            LuckIncrease = 0;

            MilestoneBonus = ResilienceStat.GetGlassCannonMilestoneBonus(s.ResilienceStat);
            WisdomStat.UpdateCogflyWarriorSpawnLimit();

            HeroController.instance.CharmUpdate();
            BlueHealthUpdate.CancelBlueHPUpdate();
            BlueHealthUpdate.CallUpdateBlueHP();

            WisdomStat.HandleSilkSpoolUpdate(reset: false);
        }

        /// <summary>
        /// Refunds all spent levels and resets stats to 1, consuming silkeaters.
        /// </summary>
        public void Respec(bool ignoreCost = false)
        {
            var s = Bonfire.Instance.Status;

            int relicLevelsSpent = s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                                + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                                - (s.TotalRosaryLevels - 1);

            if (s.TotalRosaryLevels == 1 && relicLevelsSpent == 0) return;

            var silkEaters = Resources.FindObjectsOfTypeAll<CollectableItem>()
                .FirstOrDefault(i => i.name == "Silk Grub");

            if (!(GlobalStat.CheckGlobalMilestone(GlobalMilestone.RESPECFREE) || ignoreCost))
            {
                if (silkEaters == null || silkEaters.CollectedAmount < s.RespecCost)
                    return;
            }

            RefreshRelicsDeposited();
            if (CachedRelicsObtained < 0) return;
            s.RespecRelicLevels = s.ConsumedRelicLevels = CachedRelicsObtained;

            HeroController.instance.AddGeo(s.TotalSpentRosaries);
            s.TotalSpentRosaries = 0;
            s.TotalRosaryLevels = 1;

            s.StrengthStat = 0;
            s.DexterityStat = 0;
            s.IntelligenceStat = 0;
            s.ArtificeStat = 0;
            s.ResilienceStat = 0;
            s.WisdomStat = 0;
            s.LuckStat = 0;

            StrengthIncrease = 0;
            DexterityIncrease = 0;
            IntelligenceIncrease = 0;
            ArtificeIncrease = 0;
            ResilienceIncrease = 0;
            WisdomIncrease = 0;
            LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;

            if (GlobalStat.CheckGlobalMilestone(GlobalMilestone.RESPECFREE) || ignoreCost)
            {
                s.RespecCost = 0;
            }
            else
            {
                if (s.RespecCost > 0)
                    silkEaters.Take(s.RespecCost);

                s.RespecCost++;
            }

            ResetMilestoneFlags();

            HeroController.instance.CharmUpdate();
            BlueHealthUpdate.CancelBlueHPUpdate();
            BlueHealthUpdate.CallUpdateBlueHP();

            WisdomStat.HandleSilkSpoolUpdate(reset: true);
        }

        /// <summary>
        /// Reset pending stat levels.
        /// </summary>
        public void ResetPendingLevelUp()
        {
            var s = Bonfire.Instance.Status;
            var ls = Instance;

            ls.StrengthIncrease = 0;
            ls.DexterityIncrease = 0;
            ls.IntelligenceIncrease = 0;
            ls.ArtificeIncrease = 0;
            ls.WisdomIncrease = 0;
            ls.ResilienceIncrease = 0;
            ls.LuckIncrease = 0;

            s.PendingRosaries = 0;
            s.PendingRosaryLevels = 0;
            s.PendingRelicLevels = 0;
        }

        /// <summary>
        /// Checks stat level range and prevents leveling beyond stat cap.
        /// </summary>
        public bool CheckStatLevels()
        {
            var s = Bonfire.Instance.Status;
            bool valid = true;

            if (s.StrengthStat < 0) s.StrengthStat = 0;
            else if (s.StrengthStat > MaxStatLevel) s.StrengthStat = MaxStatLevel;
            else if (s.StrengthStat + StrengthIncrease > MaxStatLevel)
            {
                StrengthIncrease--;
                valid = false;
            }

            if (s.DexterityStat < 0) s.DexterityStat = 0;
            else if (s.DexterityStat > MaxStatLevel) s.DexterityStat = MaxStatLevel;
            else if (s.DexterityStat + DexterityIncrease > MaxStatLevel)
            {
                DexterityIncrease--;
                valid = false;
            }

            if (s.IntelligenceStat < 0) s.IntelligenceStat = 0;
            else if (s.IntelligenceStat > MaxStatLevel) s.IntelligenceStat = MaxStatLevel;
            else if (s.IntelligenceStat + IntelligenceIncrease > MaxStatLevel)
            {
                IntelligenceIncrease--;
                valid = false;
            }

            if (s.ArtificeStat < 0) s.ArtificeStat = 0;
            else if (s.ArtificeStat > MaxStatLevel) s.ArtificeStat = MaxStatLevel;
            else if (s.ArtificeStat + ArtificeIncrease > MaxStatLevel)
            {
                ArtificeIncrease--;
                valid = false;
            }

            if (s.ResilienceStat < 0) s.ResilienceStat = 0;
            else if (s.ResilienceStat > MaxStatLevel) s.ResilienceStat = MaxStatLevel;
            else if (s.ResilienceStat + ResilienceIncrease > MaxStatLevel)
            {
                ResilienceIncrease--;
                valid = false;
            }

            if (s.WisdomStat < 0) s.WisdomStat = 0;
            else if (s.WisdomStat > MaxStatLevel) s.WisdomStat = MaxStatLevel;
            else if (s.WisdomStat + WisdomIncrease > MaxStatLevel)
            {
                WisdomIncrease--;
                valid = false;
            }

            if (s.LuckStat < 0) s.LuckStat = 0;
            else if (s.LuckStat > MaxStatLevel) s.LuckStat = MaxStatLevel;
            else if (s.LuckStat + LuckIncrease > MaxStatLevel)
            {
                LuckIncrease--;
                valid = false;
            }

            return valid;
        }

        public int GetAvailableRelicLevels()
        {
            var s = Bonfire.Instance.Status;
            return CachedRelicsObtained - s.ConsumedRelicLevels + s.RespecRelicLevels - s.PendingRelicLevels;
        }

        /// <summary>
        /// Updates deposited relic count which can be then accessed via CachedRelicsObtained getter.
        /// </summary>
        public void RefreshRelicsDeposited()
        {
            _cachedRelicsDeposited = RelicsDeposited();
        }


        private static LevelingSystem _instance;

        private int _cachedRelicsDeposited = 0;

        // counts all tier 3-5 relics the player has given to relic seeker npc
        private static int RelicsDeposited()
        {
            var relicChoralCommandments = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Seal Chit");

            var relicRuneHarps = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Weaver Record");

            var relicArcaneEggs = Resources.FindObjectsOfTypeAll<CollectableItemRelicType>()
                .FirstOrDefault(r => r.name == "R Ancient Egg");

            if (relicChoralCommandments == null || relicRuneHarps == null || relicArcaneEggs == null)
            {
                Bonfire.Log.LogDebug("One or more null relic field");
                return -1;
            }

            int total = 0;
            foreach (var relic in relicChoralCommandments.Relics
                .Concat(relicRuneHarps.Relics)
                .Concat(relicArcaneEggs.Relics))
            {
                var data = relic.SavedData;
                if (data.IsDeposited) total++;
            }

            Bonfire.Log.LogDebug("[LevelingSystem] Updated relic cache");

            return total;
        }

        private static void ResetMilestoneFlags()
        {
            var s = Bonfire.Instance.Status;

            s.AutoReleaseNeedleStrike = false;
            s.WeaverAuraMode = (int)WeaverAura.DISABLED;
            s.WeakBindEnabled = false;
            s.BlueMaskState = (int)BlueState.DISABLED;
            s.SilkNailsEnabled = false;
            s.CogflyWarriorEnabled = false;
            s.PinwielderEnabled = false;

            MilestoneBonus = ResilienceStat.GetGlassCannonMilestoneBonus(0);
            WisdomStat.UpdateCogflyWarriorSpawnLimit();
        }

        private void Awake()
        {
            _instance = this;
        }
    }
}