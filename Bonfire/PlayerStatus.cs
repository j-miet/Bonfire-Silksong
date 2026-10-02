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
        // - ConsumedRelicLevels: relics already part of RespecRelicLevels total pool + ones added after. Therefore 
        // useful when calculating how many deposits have occurred since last respec.
        // - PendingRelicLevels: free levels put in stats, but not yet confirmed. Reset back to 0 on confirm & cancel.
        public int RespecRelicLevels = 0;
        public int ConsumedRelicLevels = 0;
        public int PendingRelicLevels = 0;
        public int RespecCost = 0;

        // GUI styles
        public int LevelingInfoStyle = 1;
        public int BenchMenuStyle = 1;

        // stat milestones (always ensure Leveling.UI.GameData -> CacheUpdated checks all of these)
        public bool AutoReleaseNeedleStrike = false;
        public int WeaverAuraMode = 0;
        public bool WeakBindEnabled = false;
        public int BlueMaskState = 0;
        public bool SilkNailsEnabled = false;
        public bool CogflyWarriorEnabled = false;
        public bool PinwielderEnabled = false;

        // settings
        public bool DebugTeleport = true;
        public bool DebugLogs = false;
        public int AdjustButtonStep = 5;

        // - custom cursor
        public int CustomCursorSpeed = 500;
        public bool DisplayGameCursorWithGamepad = false;

        // - other
        public bool ShowCustomRangeIndicator = true;
        public bool ShowDamageOverlay = false;
        public int DamageOverlayX = 200;
        public int DamageOverlayY = 5;

        // - bench teleport
        public bool ResetMusicAfterTeleport = false;

        // - gameplay
        public bool EnemyHealthBarsEnabled = false;
        public bool EnemyHpBarColorProgression = false;
        public bool EnemyHpDisplayNumbers = false;

        public bool DeliveryQuestsRemoveTimer = false;
        public bool DeliveryQuestsRemoveDamage = false;

        public bool ReduceFleaGamesScoreLimit = false;
        public bool EasierSprintmasterRaces = false;

        // gamestates
        public bool VisitedWandererChapel = false;
        public bool VisitedReaperChapel = false;
        public bool VisitedArchitectChapel = false;
        public bool VisitedAbyssSecondTime = false;
    }
}