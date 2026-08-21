using System;
using System.Linq;

using GlobalEnums;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// High-level leveling system logic. Knows only the numerical values of each stat, but not their underlying logic.
    /// </summary>
    public class LevelingSystem : MonoBehaviour
    {
        public int StrengthIncrease = 0;
        public int DexterityIncrease = 0;
        public int IntelligenceIncrease = 0;
        public int ArtificeIncrease = 0;
        public int ResilienceIncrease = 0;
        public int WisdomIncrease = 0;
        public int LuckIncrease = 0;

        public static int MilestoneBonus { get; private set; } = 0;
        public static LevelingSystem Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        /// <summary>
        /// For highlighting stat milestones in LevelingGUI.
        /// </summary>
        public static bool HighLightStatMilestone(string name, int m)
        {
            string normalized = name.ToLower();

            switch (normalized)
            {
                case "strength":
                    return Bonfire.Instance.Status.StrengthStat
                    + Instance.StrengthIncrease
                    + MilestoneBonus
                    >= StrengthLogic.StrengthMilestone(m);

                case "dexterity":
                    return Bonfire.Instance.Status.DexterityStat
                    + Instance.DexterityIncrease
                    + MilestoneBonus
                    >= DexterityLogic.DexterityMilestone(m);

                case "intelligence":
                    return Bonfire.Instance.Status.IntelligenceStat
                    + Instance.IntelligenceIncrease
                    + MilestoneBonus
                    >= IntelligenceLogic.IntelligenceMilestone(m);

                case "artifice":
                    return Bonfire.Instance.Status.ArtificeStat
                    + Instance.ArtificeIncrease
                    + MilestoneBonus
                    >= ArtificeLogic.ArtificeMilestone(m);

                case "resilience":
                    return Bonfire.Instance.Status.ResilienceStat
                    + Instance.ResilienceIncrease
                    + MilestoneBonus
                    >= ResilienceLogic.ResilienceMilestone(m);

                case "wisdom":
                    return Bonfire.Instance.Status.WisdomStat
                    + Instance.WisdomIncrease
                    + MilestoneBonus
                    >= WisdomLogic.WisdomMilestone(m);

                case "luck":
                    return Bonfire.Instance.Status.LuckStat
                    + Instance.LuckIncrease
                    + MilestoneBonus
                    >= LuckLogic.LuckMilestone(m);

                default:
                    return false;
            }
        }

        /// <summary>
        /// Resets all initialization flags when in main menu.
        /// </summary>
        public static void ResetInitFlags()
        {
            if (GameManager.instance.GameState == GameState.MAIN_MENU)
            {
                IntelligenceLogic.BindCostInit = false;
                ResilienceLogic.BlueHpInit = false;
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

            s.RespecRelicLevels -= fromRespecPool;
            s.ConsumedRelicLevels += fromNewDeposits;
            s.PendingRelicLevels = 0;

            PlayerData.instance.TakeGeo(s.PendingRosaries);
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

            Bonfire.Log.LogDebug(
                "[LevelingSystem] Level up applied: "
                + StrengthIncrease + " Strength, "
                + DexterityIncrease + " Dexterity, "
                + IntelligenceIncrease + " Intelligence, "
                + ArtificeIncrease + " Artifice, "
                + ResilienceIncrease + " Resilience, "
                + WisdomIncrease + " Wisdom and "
                + LuckIncrease + " Luck."
            );

            StrengthIncrease = 0;
            DexterityIncrease = 0;
            IntelligenceIncrease = 0;
            ArtificeIncrease = 0;
            ResilienceIncrease = 0;
            WisdomIncrease = 0;
            LuckIncrease = 0;

            MilestoneBonus = ResilienceLogic.GetBlueBloodedMilestoneBonus(s.ResilienceStat);

            HeroController.instance.CharmUpdate();
            ResilienceLogic.CancelBlueHPUpdate();
            ResilienceLogic.CallUpdateBlueHP();
        }

        /// <summary>
        /// Refunds all spent levels and resets stats to 1, consuming silkeaters.
        /// </summary>
        public void Respec()
        {
            var s = Bonfire.Instance.Status;
            var silkEaters = Resources.FindObjectsOfTypeAll<CollectableItem>()
                .FirstOrDefault(i => i.name == "Silk Grub");

            if (silkEaters == null || silkEaters.CollectedAmount < s.RespecCost) return;

            int relicLevelsSpent = s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                                + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                                - (s.TotalRosaryLevels - 1);

            if (s.TotalRosaryLevels == 1 && relicLevelsSpent == 0) return;

            RefreshRelicsDeposited();
            if (CachedRelicsObtained < 0) return;
            s.RespecRelicLevels = s.ConsumedRelicLevels = CachedRelicsObtained;

            PlayerData.instance.AddGeo(s.TotalSpentRosaries);
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

            silkEaters.Take(s.RespecCost);
            s.RespecCost++;

            MilestoneBonus = ResilienceLogic.GetBlueBloodedMilestoneBonus(0);

            HeroController.instance.CharmUpdate();
            ResilienceLogic.CancelBlueHPUpdate();
            ResilienceLogic.CallUpdateBlueHP();
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
        /// Checks stat level range and prevents leveling beyond stat cap of 99.
        /// </summary>
        public bool CheckStatLevels()
        {
            var s = Bonfire.Instance.Status;
            bool valid = true;

            if (s.StrengthStat < 0) s.StrengthStat = 0;
            else if (s.StrengthStat > 99) s.StrengthStat = 99;
            else if (s.StrengthStat + StrengthIncrease > 99)
            {
                StrengthIncrease--;
                valid = false;
            }

            if (s.DexterityStat < 0) s.DexterityStat = 0;
            else if (s.DexterityStat > 99) s.DexterityStat = 99;
            else if (s.DexterityStat + DexterityIncrease > 99)
            {
                DexterityIncrease--;
                valid = false;
            }

            if (s.IntelligenceStat < 0) s.IntelligenceStat = 0;
            else if (s.IntelligenceStat > 99) s.IntelligenceStat = 99;
            else if (s.IntelligenceStat + IntelligenceIncrease > 99)
            {
                IntelligenceIncrease--;
                valid = false;
            }

            if (s.ArtificeStat < 0) s.ArtificeStat = 0;
            else if (s.ArtificeStat > 99) s.ArtificeStat = 99;
            else if (s.ArtificeStat + ArtificeIncrease > 99)
            {
                ArtificeIncrease--;
                valid = false;
            }

            if (s.ResilienceStat < 0) s.ResilienceStat = 0;
            else if (s.ResilienceStat > 99) s.ResilienceStat = 99;
            else if (s.ResilienceStat + ResilienceIncrease > 99)
            {
                ResilienceIncrease--;
                valid = false;
            }

            if (s.WisdomStat < 0) s.WisdomStat = 0;
            else if (s.WisdomStat > 99) s.WisdomStat = 99;
            else if (s.WisdomStat + WisdomIncrease > 99)
            {
                WisdomIncrease--;
                valid = false;
            }

            if (s.LuckStat < 0) s.LuckStat = 0;
            else if (s.LuckStat > 99) s.LuckStat = 99;
            else if (s.LuckStat + LuckIncrease > 99)
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
        public int CachedRelicsObtained => _cachedRelicsDeposited;


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
                return -1;

            int total = 0;
            foreach (var relic in relicChoralCommandments.Relics
                .Concat(relicRuneHarps.Relics)
                .Concat(relicArcaneEggs.Relics))
            {
                var data = relic.SavedData;
                if (data.IsDeposited) total++;
            }

            return total;
        }

        private void Awake()
        {
            _instance = this;
        }
    }
}