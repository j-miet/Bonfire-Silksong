using System;

namespace BonfireSilksong
{
    [Serializable]
    public class PlayerStatus
    {
        // leveling system values
        public int StrengthStat = 0;
        public int DexterityStat = 0;
        public int IntelligenceStat = 0;
        public int ArtificeStat = 0;
        public int ResilienceStat = 0;
        public int WisdomStat = 0;
        public int LuckStat = 0;

        public int TotalRosaryLevels = 1; // levelling starts from 1
        public int TotalSpentRosaries = 0;
        public int PendingRosaryLevels = 0;
        public int PendingRosaries = 0;

        public int RespecRelicLevels = 0; // available free levels earned back from respecs
        public int ConsumedRelicLevels = 0; // relics already absorbed into RespecRelicLevels or spent
        public int PendingRelicLevels = 0;
        public int RespecCost = 1;

        // other
        public bool InfiniteShellShardsEnabled = false; // obtaining/using shards replenishes them to full
        public bool InfiniteShardsBenchOnly = false; // shards replenish to full, but only when sitting on a bench

        public bool EnemyHealthBarsEnabled = false;
        public bool EnemyHpBarColorProgression = false;

        public bool UnlockedAllCrests = false;

        // bench fast travel
        public bool UnlockedFastTravel = false;
        public bool VisitedAbyssSecondTime = false;
        public int BenchMenuStyle = 1;
    }
}