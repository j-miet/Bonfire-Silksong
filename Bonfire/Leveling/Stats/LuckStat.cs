using System;

using UnityEngine;

namespace BonfireSilksong.Leveling
{
    public enum CritBonus
    {
        DEXTERITYFINALMILESTONE,
        INTELLIGENMILESTONE3,
        BLUESTATEPLASMIFIED,
        SILKNAILS
    }

    public static class LuckStat
    {
        public const int CritBonusDexterityFinalMilestone = 5;
        public const int CritBonusIntelligenceMilestone3 = 4;
        public const int CritBonusBlueStatePlasmified = 8;
        public const int CritBonusSilkNails = 6;

        public const float ThreadStormDuration = 0.5f;
        public const float ThreadStormDurationExtended = 2f;

        public static bool Crit = false;

        public static bool PinWielderUnlocked => CheckLuckMilestone(1);
        public static bool IsPinwielderEnabled => CheckLuckMilestone(1) && Bonfire.Instance.Status.PinwielderEnabled;

        public static int LuckMilestone(int m)
        {
            switch (m)
            {
                case 1: return 5;
                case 2: return 15;
                case 3: return 25;
                case 4: return 35;
                default: return LevelingSystem.MaxStatLevel;
            }
        }

        public static bool CheckLuckMilestone(int m)
        {
            var s = Bonfire.Instance.Status.LuckStat;
            int b = LevelingSystem.MilestoneBonus;
            return s + b >= LuckMilestone(m) && s >= 1;
        }

        /// <summary>
        /// Crit chance (independent from game's own crit calculations e.g. from Magnetite Dice or Wanderer's Crest)
        /// </summary>
        public static int CritChance(int totalLck)
        {
            if (totalLck <= 0) return 0;

            return (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));
        }

        /// <summary>
        /// Cast 'Thread Storm' silk skill when enemy is hit. Thread Storm is limited to one instance meaning if one
        /// is active in scene, another cannot spawn until first ends.
        /// </summary>
        public static void CastThreadStormOnHit(HealthManager hm, in HitInstance hit, bool crit)
        {
            if (hm != null &&
                hm.gameObject != null &&
                hm.hp > 0 &&
                hit.RepresentingTool?.Type == ToolItemType.Red &&
                IsPinwielderEnabled)
            {
                string toolName = hit.RepresentingTool.name;

                if (toolName == "Harpoon" && CheckLuckMilestone(4))
                {
                    ThreadStormSpawner.CastThreadStorm(hm.gameObject, ThreadStormDurationExtended);
                }
                else if (crit &&
                    ((toolName == "Straight Pin" && CheckLuckMilestone(4)) ||
                    (toolName == "Harpoon" && CheckLuckMilestone(3))))
                {
                    ThreadStormSpawner.CastThreadStorm(hm.gameObject, ThreadStormDuration);
                }
            }
        }

        /// <summary>
        /// Helper method for ExtraCritChance & GUI panels.
        /// </summary>
        public static int GetBonusCritChance(CritBonus bonus, bool isGUI = false)
        {
            switch (bonus)
            {
                case CritBonus.DEXTERITYFINALMILESTONE:
                    return DexterityStat.CheckDexterityMilestone(3) ? CritBonusDexterityFinalMilestone : 0;
                case CritBonus.INTELLIGENMILESTONE3:
                    return IntelligenceStat.CheckIntelligenceMilestone(3) ? CritBonusIntelligenceMilestone3 : 0;
                case CritBonus.BLUESTATEPLASMIFIED:
                    return ResilienceStat.IsInAnyBlueStateAndPlasmified() ||
                        (isGUI && ResilienceStat.CheckResilienceMilestone(3))
                        ? CritBonusBlueStatePlasmified
                        : 0;
                case CritBonus.SILKNAILS:
                    return WisdomStat.CheckWisdomMilestone(4) ? CritBonusSilkNails : 0;
                default: return 0;
            }
        }

        /// <summary>
        /// Get bonus crit chance for this hit.
        /// </summary>
        public static int ExtraCritChance(HitInstance hit)
        {
            int extraCrit = 0;

            extraCrit += GetBonusCritChance(CritBonus.DEXTERITYFINALMILESTONE);
            extraCrit += GetBonusCritChance(CritBonus.BLUESTATEPLASMIFIED);

            if (hit.Source == null)
                return extraCrit;

            var n = hit.Source.name;
            if (n == "Bonfire-SilkNails")
            {
                extraCrit += GetBonusCritChance(CritBonus.SILKNAILS);
            }

            // intelligence crit bonus for silk skills, claw mirror and weaver aura taunt
            if (n == "Bonfire-WeaverAura")
            {
                extraCrit += GetBonusCritChance(CritBonus.INTELLIGENMILESTONE3);
            }
            else if (hit.RepresentingTool != null)
            {
                var t = hit.RepresentingTool;
                if (t.Type == ToolItemType.Skill ||
                    (t.Type == ToolItemType.Blue && (n == "Dazzle Bind" || n == "Dazzle Bind Upgraded")))
                {
                    extraCrit += GetBonusCritChance(CritBonus.INTELLIGENMILESTONE3);
                }
            }

            return extraCrit;
        }

        /// <summary>
        /// HealthManager.Hit (Prefix): Check if hit was a crit and update StatsShared.Crit flag.
        /// </summary>
        public static void CheckIfCrit(HitInstance hit)
        {
            // Actual crit handling sequence:
            // - DamageEnemies.DoDamage calls Hit at some point
            // - HealthManager.Hit checks crit roll and handles player's perspective by adding damage and displaying 
            // visual effect on player though DexterityStat logic.
            // - DamageEnemies.DoDamage Postfix displays a brief visual effect on enemy that got hit using the
            // PlayCritVisualOnEnemyHit method.

            if (!hit.IsHeroDamage) return;

            if (ResilienceStat.GlassCannonButNotPlasmified)
            {
                Crit = false;
                return;
            }

            int critChance = CritChance(Bonfire.Instance.Status.LuckStat);
            critChance += ExtraCritChance(hit);

            if (critChance <= 0) // must have at least 1% crit chance
            {
                Crit = false;
                return;
            }

            int critRoll = UnityEngine.Random.Range(1, 101);

            Bonfire.Log.LogDebug($"[LuckStat] Crit chance: {critChance}. Rolled {critRoll}.");

            bool didCrit = critRoll <= critChance;
            if (didCrit)
                Bonfire.Log.LogDebug($"Rolled a crit.");

            Crit = didCrit;
        }

        /// <summary>
        /// DamageEnemies.DoDamage (Postfix): Display a crit visual effect on enemy hit.
        /// </summary>
        public static void PlayCritVisualOnEnemyHit(GameObject target)
        {
            if (Crit)
            {
                // effect comes from prefab value, these can have both visual and sound included. 
                // Prefer one with no audio + only mild visuals; you will be hearing/seeing this effect a lot if
                // you level up luck stat
                SpawnPrefab.Spawn(target, HeroController.instance.takeHitBlackHealthNullifyPrefab);
            }
        }

        /// <summary>
        /// HeroController.ThrowTool (Postfix): Refund use cost of Straight Pin when Pinwielder mode in enabled.
        /// </summary>
        public static void CheckStraightPinRefund(ToolItem tool)
        {
            if (IsPinwielderEnabled && tool.name == "Straight Pin")
                tool.CollectFree(100);
        }
    }
}