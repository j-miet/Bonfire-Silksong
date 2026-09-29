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
        EXTRADOTDAMAGE,
        SILKREGEN,
        RESPECFREE,
    }

    public static class GlobalStat
    {
        public const int QoLToolsLevelRequirement = 5;
        public const int ShardRefillLevelRequirement = 15;
        public const int CurrencyBuffLevelRequirement = 30;
        public const int ExtraRosaryThreshold = 3;
        public const int DotDamageMultiplier = 2;
        public const float RosaryPercentageIncrease = 0.05f;
        public const int SilkRegenInterval = 10;
        public const int MapCountRequirement = 26; // base game map count is 28 -> Abyss & Verdania are in Act 3

        public static bool CheckFastTravelCondition => CheckGlobalMilestone(GlobalMilestone.BENCHTELEPORT);

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
                    return currentLvl >= ShardRefillLevelRequirement && pd.defeatedCogworkDancers;

                case GlobalMilestone.MAGGOTREPEL:
                    return pd.DefeatedSwampShaman || pd.MapCount >= MapCountRequirement;

                case GlobalMilestone.EXTRADOTDAMAGE:
                    return pd.defeatedZapCoreEnemy && pd.defeatedDockForemen && pd.defeatedRoachkeeperChef;

                case GlobalMilestone.CURRENCYMULTIPLIER:
                    return pd.blackThreadWorld && pd.UnlockedFastTravelTeleport;

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
        public static int ApplyCurrencyMultiplier(int droppedRosaries, int currentLvl)
        {
            return (int)Math.Round(droppedRosaries * (1f + GetCurrencyMultiplier(currentLvl)));
        }

        public static float GetCurrencyMultiplier(int currentLvl)
        {
            return currentLvl * RosaryPercentageIncrease;
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
                Gameplay.CompassTool.SavedData.IsUnlocked &&
                CheckGlobalMilestone(GlobalMilestone.QOLTOOLS))
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
                Gameplay.MaggotCharm.SavedData.IsUnlocked &&
                CheckGlobalMilestone(GlobalMilestone.MAGGOTREPEL))
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
            if (magnetTool != null &&
                magnetTool.SavedData.IsUnlocked &&
                CheckGlobalMilestone(GlobalMilestone.QOLTOOLS))
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


        private static float _silkRegenTime = 0f;
    }
}