using System;

using GlobalSettings;

namespace BonfireSilksong.Leveling
{
    public enum GlobalMilestone
    {
        CURRENCYBASE,
        BENCHTELEPORT,
        QOLTOOLS,
        SHARDREFILL,
        CURRENCYMULTIPLIER,
        MAGGOTREPEL,
        SILKREGEN,
        RESPECFREE,
    }

    public static class GlobalStat
    {
        public const int ExtraRosaryThreshold = 5;
        public const int CurrencyMultiplier = 2;
        public const int SilkRegenInterval = 10;

        public static bool CheckFastTravelCondition => CheckGlobalMilestone(GlobalMilestone.BENCHTELEPORT);

        public static string GetGlobalMilestoneString(GlobalMilestone m)
        {
            switch (m)
            {
                case GlobalMilestone.CURRENCYBASE:
                    return "★ Defeat \"Moss Mother\" boss ★";

                case GlobalMilestone.BENCHTELEPORT:
                    return "★ Defeat \"Bell Beast\" boss & Obtain a Map + Bench Pins from Shakra ★";

                case GlobalMilestone.QOLTOOLS:
                    return $"★ Reach level {QoLToolsLevelRequirement} ★";

                case GlobalMilestone.SHARDREFILL:
                    return $"★ Reach level {ShellShardRefillLevelRequirement} & Defeat \"Cogwork Dancers\" boss ★";

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return $"★ Reach level {CurrencyBuffLevelRequirement} & Obtain Threefold Melody ★";

                case GlobalMilestone.MAGGOTREPEL:
                    return "★ Defeat \"Raging Conchfly\" boss & Visit Pale Lake ‹- OR -› " +
                        "Defeat \"Groal the Great\" boss ★";

                case GlobalMilestone.SILKREGEN:
                    return "★ Ascend from the Abyss ★";

                case GlobalMilestone.RESPECFREE:
                    return "★ Obtain any of the old hearts in Act 3 ★";

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
                    return currentLvl >= ShellShardRefillLevelRequirement && pd.defeatedCogworkDancers;

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return currentLvl >= CurrencyBuffLevelRequirement &&
                        pd.HasMelodyArchitect && pd.HasMelodyConductor && pd.HasMelodyLibrarian;

                case GlobalMilestone.MAGGOTREPEL:
                    return (pd.defeatedCoralDrillerSolo && pd.SeenFleatopiaEmpty) || pd.DefeatedSwampShaman;

                case GlobalMilestone.SILKREGEN:
                    return pd.blackThreadWorld && pd.completedAbyssAscent;

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
        /// ToolItem.IsEquipped (Postfix): Have Compass effect on map without equipping the tool.
        /// Player must still own the tool before effect can be applied.
        /// </summary>
        public static bool GetCompassToolEffect(ToolItem tool)
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
        /// ToolItem.IsEquipped (Postfix): Have Wreath of Purity effect without equipping the tool.
        /// Player must still own the tool before effect can be applied.
        /// </summary>
        public static bool GetWreathOfPurityToolEffect(ToolItem tool)
        {
            if (tool != null &&
                Gameplay.MaggotCharm != null &&
                tool.name == Gameplay.MaggotCharm.name &&
                Gameplay.MaggotCharm.SavedData.IsUnlocked)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// CurrencyObjectBase.MagnetToolIsEquipped (Postfix): Have Magnetite Brooch effect without equipping the tool.
        /// Player must still own the tool before effect can be applied.
        /// </summary>
        public static bool GetMagnetiteBroochToolEffect()
        {
            var magnetTool = ToolItemManager.GetToolByName("Rosary Magnet");
            if (magnetTool != null && magnetTool.SavedData.IsUnlocked)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// HeroController.Update (Postfix): Passive silk regen.
        /// </summary>
        public static void CheckSilkRegen(float dt)
        {
            if (!CheckGlobalMilestone(GlobalMilestone.SILKREGEN)) return;

            if (_silkRegenTime > 0)
            {
                _silkRegenTime -= dt;
            }
            else
            {
                HeroController.instance.AddSilk(1, heroEffect: false);
                _silkRegenTime = SilkRegenInterval;

                Bonfire.Log.LogDebug("[GlobaStat] Regenerated 1 silk");
            }
        }


        private const int QoLToolsLevelRequirement = 5;
        private const int ShellShardRefillLevelRequirement = 15;
        private const int CurrencyBuffLevelRequirement = 25;

        private static float _silkRegenTime = 0f;

        // private static int SilkRegenInterval(int totalLvl)
        // {
        //     return (int)(4.0 + 36.0 * Math.Exp(-0.06 * totalLvl));
        // }
    }
}