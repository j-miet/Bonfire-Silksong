using System;
using System.Reflection;

using UnityEngine;

namespace BonfireSilksong
{
    // not a static class because MonoBehavior is required, but should otherwise include static entities only.
    public class LuckLogic : MonoBehaviour
    {
        public static bool Crit = false;

        public static int LuckMilestone(int m)
        {
            switch (m)
            {
                case 1: return 1;
                case 2: return 2;
                case 3: return 3;
                default: return 999;
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

            if (ResilienceLogic.IsInBlueStateAndPlasmified(BlueState.GLASSCANNON))
                critChance += 10;

            critChance += (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));

            return critChance;
        }

        /// <summary>
        /// Rosary (and shell shard) drop multiplier from enemies.
        /// Multiplier per level is additive 5% to current stat e.g. 1x -> 1.05x -> 1.10x -> 1.15x etc.
        /// </summary>
        public static int IncreaseRosaries(int droppedRosaries, int totalLck)
        {
            if (totalLck <= 0) return droppedRosaries;

            return (int)(droppedRosaries * (1f + (totalLck / 20f)));
        }

        // LevelingGUI helper
        public static string GetRosaryIncreasePercentage(int luckStat, int luckIncrease)
        {
            return (5 * (luckStat + luckIncrease)).ToString() + "%";
        }

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Update enemy rosary and shell shard drop count.
        /// </summary>
        public static void UpdateCurrencyDrops(HealthManager instance)
        {
            var s = Bonfire.Instance.Status;

            Bonfire.Log.LogDebug("[LuckLogic] Check currency values");
            Bonfire.Log.LogDebug("<Vanilla> rosary and shell shard values:");
            Bonfire.Log.LogDebug($"small={GetRosaries("small", instance)}");
            Bonfire.Log.LogDebug($"medium={GetRosaries("medium", instance)}");
            Bonfire.Log.LogDebug($"large={GetRosaries("large", instance)}");
            Bonfire.Log.LogDebug($"shards={GetShellShards(instance)}");

            Bonfire.Log.LogDebug("<Modded> rosary and shell shard values:");
            Bonfire.Log.LogDebug(
                $"small={IncreaseRosaries(GetRosaries("small", instance), s.LuckStat)}");
            Bonfire.Log.LogDebug(
                $"medium={IncreaseRosaries(GetRosaries("medium", instance), s.LuckStat)}");
            Bonfire.Log.LogDebug(
                $"large={IncreaseRosaries(GetRosaries("large", instance), s.LuckStat)}");
            Bonfire.Log.LogDebug(
                $"shards={IncreaseRosaries(GetShellShards(instance), s.LuckStat)}");

            instance.SetGeoSmall(
                IncreaseRosaries(GetRosaries("small", instance), s.LuckStat));
            instance.SetGeoMedium(
                IncreaseRosaries(GetRosaries("medium", instance), s.LuckStat));
            instance.SetGeoLarge(
                IncreaseRosaries(GetRosaries("large", instance), s.LuckStat));

            instance.SetShellShards(
                IncreaseRosaries(GetShellShards(instance), s.LuckStat));
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

            if (ResilienceLogic.GlassCannonButNotPlasmified)
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

            Bonfire.Log.LogDebug($"[LuckLogic] Crit chance: {critChance}. Rolled {critRoll}.");

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
                var prefab = HeroController.instance.takeHitBlackHealthNullifyPrefab;

                // create a copy of prefab to avoid modifying the original
                var effect = Instantiate(
                    prefab,
                    target.transform.position,
                    target.transform.rotation
                );

                effect.transform.SetScaleX(0.5f);
                effect.transform.SetScaleY(0.5f);

                Destroy(effect, 3f); // clean up the copy after a while; 3 seconds should be more than enough.
            }
        }


        // helpers for getting current rosary and shell shard values
        private static int GetRosaries(string size, HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField(size + "GeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object rosaries = fi.GetValue(enemy);
            return rosaries == null ? 0 : (int)rosaries;
        }

        private static int GetShellShards(HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField("shellShardDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object shards = fi.GetValue(enemy);
            return shards == null ? 0 : (int)shards;
        }
    }
}