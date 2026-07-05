using System;
using System.Linq;

using UnityEngine;

namespace BonfireSilksong
{
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

        private void Awake()
        {
            _instance = this;
        }

        /* -- stat formulas -- */

        // strength: needle damage
        public int NeedleDamage(int totalStr) =>
            (int)Math.Round((5 + 4 * PlayerData.instance.nailUpgrades) * Math.Pow(1.25, Math.Log(totalStr + 1, 2.0)));

        // dexterity: needle attack speed
        public float AttackSpeed(int totalDex) =>
            (float)Math.Round(2.7 / (1.0 + 1.82 * Math.Exp(-0.08 * (totalDex + 1))) - 0.01, 2);

        // dexterity: nail crit damage
        public int CritDamage(int totalDex, int needleDamage) => (int)(needleDamage * (1.2 + Math.Log(totalDex + 1)));

        // intelligence: silk skill damage
        public int SpellDamage(int baseDamage, int totalInt) =>
            (int)Math.Round(baseDamage * Math.Pow(1.25, Math.Log(totalInt + 1, 2.0)));

        // intelligence: binding (healing) cost
        public int BindCost(int totalInt) => (int)Math.Round(9.1 * Math.Exp(-0.01 * (totalInt + 2.0)));

        // artifice: red/blue tool damage
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

            return (int)(4.0 + 56.0 * Math.Exp(-0.06 * (totalArt - 5)));
        }

        // resilience: extra lifeblood masks
        public int ExtraMasks(int totalRes) => (int)Math.Round(-0.4 + 2.6 * Math.Log(totalRes + 1));

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

        // luck: rosary drop multiplier from enemies (5% per stat level)
        public int IncreaseRosaries(int droppedRosaries, int totalLck) =>
            (int)(droppedRosaries * (1f + (totalLck / 20f)));

        /* -- stat formulas end here */

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

            int relicsObtained = RelicsObtained();
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
            EventRegister.SendEvent(EventRegisterEvents.UpdateBlueHealth);
        }

        /// <summary>
        /// Refunds all spent levels and resets stats to 1, consuming silkeaters.
        /// </summary>
        public void Respec()
        {
            var s = Bonfire.Instance.Status;
            var silkGrubs = Resources.FindObjectsOfTypeAll<CollectableItem>()
                .FirstOrDefault(i => i.name == "Silk Grub");

            if (silkGrubs == null || silkGrubs.CollectedAmount < s.RespecCost) return;

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

            silkGrubs.Take(s.RespecCost);
            s.RespecCost++;

            HeroController.instance.CharmUpdate();
            EventRegister.SendEvent(EventRegisterEvents.UpdateBlueHealth);
        }

        /// <summary>
        /// Counts all rune harps and arcane eggs the player has given to relic seeker npc
        /// </summary>
        public int RelicsObtained()
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

        /// <summary>
        /// Returns average damage blocking chance from current resiliency stat value
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
        /// Helper for caching obtained relics data; otherwise bench ui would re-calculate this every single frame
        /// To access cached data, use CachedRelicsObtained getter.
        /// </summary>
        public void RefreshRelicsObtained()
        {
            _cachedRelicsObtained = Instance.RelicsObtained();
        }
        public int CachedRelicsObtained => _cachedRelicsObtained;


        private static LevelingSystem _instance;
        private int _cachedRelicsObtained = 0;
    }
}