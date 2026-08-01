using System;
using System.Linq;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Leveling system logic. 
    /// 1. All Bonfire.instance.PlayerStatus write operations should also be done here.
    /// 2. All methods should be instanced: accessing leveling system through a single shared LevelingSystem.Instance 
    /// makes code clearer without mixing instanced/static calls, even if it technically violates OOP principles where 
    /// non-instance methods should be always static.
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

        public static LevelingSystem Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        // -- stat formulas -- //

        // strength: needle damage
        public int NeedleDamage(int totalStr) =>
            (int)Math.Round((5 + 4 * PlayerData.instance.nailUpgrades) * Math.Pow(1.2, Math.Log(totalStr + 1, 2.0)));

        // dexterity: needle attack speed
        public float AttackSpeed(int totalDex) =>
            (float)Math.Round(2.7 / (1.0 + 1.82 * Math.Exp(-0.08 * (totalDex + 1))) - 0.01, 2);

        // dexterity: needle crit damage
        public int CritDamage(int totalDex, int needleDamage) => (int)(needleDamage * (1.2 + Math.Log(totalDex + 1)));

        // intelligence: silk skill damage
        public int SpellDamage(int baseDamage, int totalInt) =>
            (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));

        // intelligence: binding (healing) cost
        public int BindCost(int totalInt) => (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));

        // artifice: red and blue tool damage
        public int ToolDamage(int baseDamage, int totalArt)
        {
            if (totalArt <= 0) return baseDamage;

            return (int)Math.Round(baseDamage * (1.0 + 2.0 / (1.0 + 15.0 * Math.Exp(-0.1 * totalArt))));
        }

        // artifice: amount of time it takes to passively replenish a single tool charge (without costing any shards)
        // starts from 60 seconds timer at lvl 5: this prevents player from putting just a single point for such an 
        // impactful upgrade
        public int ToolRegenInterval(int totalArt)
        {
            if (totalArt < 5) return -1;

            return (int)(2.0 + 58.0 * Math.Exp(-0.06 * (totalArt - 5)));
        }

        // resilience: extra lifeblood masks. Caps at 8 on level 40; using a single plasmium from phial will then
        // activate overdose regen effect
        public int ExtraMasks(int totalRes)
        {
            if (totalRes >= 40) return 8;

            return (int)Math.Round(8.0 * (1.0 - Math.Exp(-0.055 * (totalRes - 1))));
        }

        // resilience: chance to block an enemy hit
        public float BlockChance(int totalRes) => (float)(0.72 / (1.0 + 12.0 * Math.Exp(-0.09 * totalRes)));

        // wisdom: extra silk gained when striking an enemy with needle (+1 silk for every 20 levels)
        public int ExtraSilk(int totalWsdm, int baseSilk) => (int)(baseSilk + Math.Floor(totalWsdm / 20f));

        // wisdom: amount of time it takes to passively produce a single silk
        // starts from 40 seconds timer at lvl 5 to prevents players from putting just a single point and
        // reaping the benefits
        public int SilkRegenInterval(int totalWsdm)
        {
            if (totalWsdm < 5) return -1;

            return (int)(4.0 + 36.0 * Math.Exp(-0.06 * (totalWsdm - 5)));
        }

        // luck: crit chance
        public int CritChance(int totalLck)
        {
            if (totalLck <= 0) return 0;

            return (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));
        }

        // luck: rosary drop multiplier from enemies.
        // Multiplier per level is additive 5% to current stat e.g. 1x -> 1.05x -> 1.10x -> 1.15x etc.
        public int IncreaseRosaries(int droppedRosaries, int totalLck) =>
            (int)(droppedRosaries * (1f + (totalLck / 20f)));

        // -- stat formulas end here -- //

        public bool CanLevelUp(bool gotFreeLevel, int rosariesToLvlUp)
        {
            var s = Bonfire.Instance.Status;
            return PlayerData.instance.atBench &&
                   (gotFreeLevel || (rosariesToLvlUp + s.PendingRosaries <= PlayerData.instance.geo));
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

            int relicsObtained = CachedRelicsObtained;
            if (relicsObtained < 0)
            {
                Bonfire.Log.LogError("Failed to fetch relic data");
                return;
            }

            int availableRelicLevels = relicsObtained - s.ConsumedRelicLevels
                                     + s.RespecRelicLevels - s.PendingRelicLevels;

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
        /// Commits the pending stat allocations, deducting rosaries and relic levels
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
                "Level up applied: "
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

            HeroController.instance.CharmUpdate();
            Bonfire.UpdateBlueHP();
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

            s.RespecRelicLevels = 0;
            s.RespecRelicLevels += relicLevelsSpent;

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

            HeroController.instance.CharmUpdate();
            Bonfire.UpdateBlueHP();
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

        public int GetCurrentLevel()
        {
            var s = Bonfire.Instance.Status;

            return s.StrengthStat + s.DexterityStat + s.IntelligenceStat
                    + s.ArtificeStat + s.ResilienceStat + s.WisdomStat + s.LuckStat
                    + s.PendingRelicLevels
                    + s.PendingRosaryLevels
                    + 1; // base level
        }

        public int GetAvailableRelicLevels()
        {
            var s = Bonfire.Instance.Status;
            return CachedRelicsObtained - s.ConsumedRelicLevels + s.RespecRelicLevels - s.PendingRelicLevels;
        }

        public int DisplayPendingRosaryLevel()
        {
            var s = Bonfire.Instance.Status;
            return s.TotalRosaryLevels + s.PendingRosaryLevels;
        }

        public int GetLevelUpRosaryRequirement(int rosaryLevels)
        {
            return (int)(Math.Pow(rosaryLevels, 2.0) + (10 * rosaryLevels) + 50.0);
        }

        /// <summary>
        /// Returns average damage blocking chance from current Resilience stat value.
        /// </summary>
        public float AverageBlockChance(int totalRes)
        {
            float f = BlockChance(totalRes);
            float[] multipliers = { 10f, 20f, 30f, 50f, 70f, 80f, 90f };
            float sum = 0f;
            foreach (var m in multipliers)
                sum += m * f;
            return sum / multipliers.Length;
        }

        /// <summary>
        /// Helper for caching obtained relics data; otherwise bench ui would re-calculate this every single frame.
        /// To access cached data, use CachedRelicsObtained getter.
        /// </summary>
        public void RefreshRelicsObtained()
        {
            _cachedRelicsObtained = Instance.RelicsObtained();
        }
        public int CachedRelicsObtained => _cachedRelicsObtained;


        private int _cachedRelicsObtained = 0;
        private static LevelingSystem _instance;

        private void Awake()
        {
            _instance = this;
        }

        // counts all tier 3-5 relics the player has given to relic seeker npc
        private int RelicsObtained()
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
    }
}