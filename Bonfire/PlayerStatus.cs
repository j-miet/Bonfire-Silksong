using System;

namespace Bonfire
{
    [Serializable]
    public class PlayerStatus
    {
        public int StrengthStat = 1;
        public int DexterityStat = 1;
        public int IntelligenceStat = 1;
        public int ResilienceStat = 1;
        public int WisdomStat = 1;
        public int LuckStat = 1;

        public int TotalRosaryLevels = 1; // levelling starts from 1
        public int TotalSpentRosaries = 0;
        public int PendingRosaryLevels = 0;
        public int PendingRosaries = 0;

        public int RespecRelicLevels = 0; // available free levels earned back from respecs
        public int PendingRelicLevels = 0;
        public int AvailableRuneHarps = 0;
        public int AvailableArcaneEggs = 0;
        public int RespecCost = 1;

        public bool InfiniteShellShardsEnabled = false; // obtaining/using shards replenishes them to full
        public bool InfiniteShardsBenchOnly = false; // shards replenish only when sitting on a bench

        public bool EnemyHealthBarsEnabled = false;
        public bool EnemyHpBarColorProgression = false;
    }
}