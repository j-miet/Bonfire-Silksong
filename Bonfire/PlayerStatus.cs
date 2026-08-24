using System;

namespace BonfireSilksong
{
    [Serializable]
    public class PlayerStatus
    {
        // leveling system
        public int StrengthStat = 0;
        public int DexterityStat = 0;
        public int IntelligenceStat = 0;
        public int ArtificeStat = 0;
        public int ResilienceStat = 0;
        public int WisdomStat = 0;
        public int LuckStat = 0;

        public int TotalRosaryLevels = 1; // leveling starts from 1
        public int TotalSpentRosaries = 0;
        public int PendingRosaryLevels = 0;
        public int PendingRosaries = 0;

        // - RespecRelicLevels: available free levels after a respec. Respec resets this back to total deposited.
        // - ConsumedRelicLevels: relics already part of RespecRelicLevels total pool + ones added after. Thus useful 
        // when calculating how many deposits have occurred since last respec.
        // - PendingRelicLevels: free levels put in stats, but not yet confirmed. Reset back to 0 on confirm & cancel.
        public int RespecRelicLevels = 0;
        public int ConsumedRelicLevels = 0;
        public int PendingRelicLevels = 0;
        public int RespecCost = 1;

        // leveling GUI
        public int LevelingInfoStyle = 0;

        // stat milestones
        public bool AutoReleaseNeedleStrike = false;
        public int BlueMaskMode = 0;

        // settings
        // - custom cursor
        public int CustomCursorSpeed = 400;
        public bool DisplayGameCursorWithGamepad = false;

        // - other
        public bool ShowCustomRangeIndicator = true;

        // - gameplay
        public bool EnemyHealthBarsEnabled = false;
        public bool EnemyHpBarColorProgression = false;

        public bool DeliveryQuestsRemoveTimer = false;
        public bool DeliveryQuestsRemoveDamage = false;

        public bool AlwaysHaveCompassTool = false;
        public bool RefillShardsAtBench = false;
        public bool ReduceFleaGamesScoreLimit = false;
        public bool EasierSprintmasterRaces = false;

        public bool UnlockedAllCrests = false;

        // bench fast travel
        public bool UnlockedFastTravel = false;
        public bool VisitedAbyssSecondTime = false;
        public int BenchMenuStyle = 1;
    }
}