using System;

using GlobalSettings;

namespace BonfireSilksong
{
    public enum GlobalMilestone
    {
        CURRENCYBASE,
        BENCHTELEPORT,
        QOLTOOLS,
        SHARDREFILL,
        CURRENCYMULTIPLIER,
        RESPECFREE
    }

    public static class GlobalStat
    {
        public const int ExtraRosaryThreshold = 5;

        public static bool CheckFastTravelCondition => CheckGlobalMilestone(GlobalMilestone.BENCHTELEPORT);

        public static string GetGlobalMilestoneString(GlobalMilestone m)
        {
            switch (m)
            {
                case GlobalMilestone.CURRENCYBASE:
                    return "Defeat \"Moss Mother\" boss";

                case GlobalMilestone.BENCHTELEPORT:
                    return "Defeat \"Bell Beast\" boss & obtain a map + Bench Pins from Shakra";

                case GlobalMilestone.QOLTOOLS:
                    return $"Reach level {QoLToolsLevelRequirement}";

                case GlobalMilestone.SHARDREFILL:
                    return $"Reach level {ShellShardRefillLevelRequirement} & Defeat \"Widow\" boss";

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return $"Reach level {CurrencyBuffLevelRequirement} & Defeat \"Groal the Great\" boss";

                case GlobalMilestone.RESPECFREE:
                    return "Obtain any of the old hearts in Act 3";

                default: return "";
            }
        }

        public static bool CheckGlobalMilestone(GlobalMilestone m)
        {
            int currentLvl = LevelingSystem.GetCurrentLevel();
            var pd = PlayerData.instance;

            switch (m)
            {
                case GlobalMilestone.CURRENCYBASE:
                    return pd.defeatedMossMother;

                case GlobalMilestone.BENCHTELEPORT:
                    return pd.defeatedBellBeast && pd.HasAnyMap && pd.hasPinBench;

                case GlobalMilestone.QOLTOOLS:
                    return currentLvl >= QoLToolsLevelRequirement;

                case GlobalMilestone.SHARDREFILL:
                    return currentLvl >= ShellShardRefillLevelRequirement;

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return currentLvl >= CurrencyBuffLevelRequirement && pd.DefeatedSwampShaman;

                case GlobalMilestone.RESPECFREE:
                    return pd.blackThreadWorld &&
                        (pd.CollectedHeartClover ||
                        pd.CollectedHeartCoral ||
                        pd.CollectedHeartFlower ||
                        pd.CollectedHeartHunter);

                default: return false;
            }
        }

        /// <summary>
        /// Increase rosary (and shell shard) base drop count.
        /// </summary>
        public static int IncreaseCurrencyBaseDrops(int totalLck, bool isGUI = false)
        {
            if (!CheckGlobalMilestone(GlobalMilestone.CURRENCYBASE) && !isGUI) return 0;

            return 1 + (int)Math.Floor(totalLck / (float)ExtraRosaryThreshold);
        }

        /// <summary>
        /// Rosary (and shell shard) drop multiplier from enemies.
        /// </summary>
        public static int ApplyCurrencyMultiplier(int droppedRosaries)
        {
            return droppedRosaries * CurrencyMultiplier;
        }

        /// <summary>
        /// CurrencyManager.ProcessAddCurrency (Postfix) + CurrencyManager.ProcessTakeCurrency (Postfix): shell shards 
        /// replenishment to full when visiting a bench.
        /// </summary>
        public static void RefillShellShardsAtBench(CurrencyType type)
        {
            if (!CheckGlobalMilestone(GlobalMilestone.SHARDREFILL)) return;

            if (!PlayerData.instance.atBench) return;

            if (type != CurrencyType.Shard) return;

            PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
        }

        /// <summary>
        /// Toolitem.IsEquipped (Postfix): Have compass effect on map without equipping the tool.
        /// Player must still own the tool before effect can be applied.
        /// </summary>
        public static bool ToggleCompassTool(ToolItem tool)
        {
            if (tool != null &&
                Gameplay.CompassTool != null &&
                tool.name == Gameplay.CompassTool.name &&
                Gameplay.CompassTool.SavedData.IsUnlocked)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// CurrencyObjectBase.MagnetToolIsEquipped (Postfix): Have Magnetite Brooch effect without equipping the tool.
        /// Player must still own the tool before effect can be applied.
        /// </summary>
        public static bool ToggleMagnetiteBroochTool()
        {
            var magnetTool = ToolItemManager.GetToolByName("Rosary Magnet");
            if (magnetTool != null && magnetTool.SavedData.IsUnlocked)
            {
                return true;
            }

            return false;
        }


        private const int QoLToolsLevelRequirement = 5;
        private const int ShellShardRefillLevelRequirement = 15;
        private const int CurrencyBuffLevelRequirement = 25;
        private const int CurrencyMultiplier = 2;
    }
}