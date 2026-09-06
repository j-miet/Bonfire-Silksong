using System;

using UnityEngine;

namespace BonfireSilksong
{
    public static class LuckStat
    {
        public const float ThreadStormDuration = 0.5f;
        public const float ThreadStormDurationExtended = 2f;

        public static bool Crit = false;

        public static bool IsPinwielderEnabled => CheckLuckMilestone(1) && Bonfire.Instance.Status.PinwielderEnabled;

        public static int LuckMilestone(int m)
        {
            switch (m)
            {
                case 1: return 5;
                case 2: return 20;
                case 3: return 30;
                case 4: return 40;
                default: return 99;
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

            int critChance = 0;

            if (ResilienceStat.IsInBlueStateAndPlasmified(BlueState.GLASSCANNON))
                critChance += 10;

            critChance += (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));

            return critChance;
        }

        /// <summary>
        /// Rosary (and shell shard) drop count multiplier from enemies.
        /// Multiplier per level is additive 5% to current stat e.g. 1x -> 1.05x -> 1.10x -> 1.15x etc.
        /// </summary>
        public static int IncreaseRosaryCount(int droppedRosaries, int totalLck)
        {
            return (int)Math.Round(droppedRosaries * (1f + (totalLck * 0.05f)));
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