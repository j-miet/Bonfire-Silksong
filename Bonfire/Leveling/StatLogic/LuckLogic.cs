using System;
using System.Reflection;

using UnityEngine;

namespace BonfireSilksong
{
    // not a static class because MonoBehavior is required, but should otherwise include static entities only.
    public class LuckLogic : MonoBehaviour
    {
        public static bool Crit = false;

        public static int LuckMilestone_1 => 1;
        public static int LuckMilestone_2 => 2;
        public static int LuckMilestone_3 => 3;
        public static bool CheckLuckMilestone_1 => Bonfire.Instance.Status.LuckStat >= LuckMilestone_1;
        public static bool CheckLuckMilestone_2 => Bonfire.Instance.Status.LuckStat >= LuckMilestone_2;
        public static bool CheckLuckMilestone_3 => Bonfire.Instance.Status.LuckStat >= LuckMilestone_3;

        // Stat scaling: crit chance
        public static int CritChance(int totalLck)
        {
            if (totalLck <= 0) return 0;

            return (int)Math.Round(60.0 / (1.0 + 15.0 * Math.Exp(-0.08 * totalLck)));
        }

        // Stat scaling: rosary (and shell shard) drop multiplier from enemies.
        // Multiplier per level is additive 5% to current stat e.g. 1x -> 1.05x -> 1.10x -> 1.15x etc.
        public static int IncreaseRosaries(int droppedRosaries, int totalLck)
        {
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
        /// DamageEnemies.DoDamage (Prefix): Check if hit was a crit and update StatsShared.Crit flag.
        /// </summary>
        public static void CheckIfCrit(DamageEnemies __instance, GameObject target)
        {
            // Actual crit handling sequence:
            // - DamageEnemies.DoDamage Prefix rolls crit chance and sets crit true/false (-> this method's logic 
            // inside patcher)
            // - Actual DamageEnemies.DoDamage calls Hit at some point
            // - HealthManager.Hit checks and handles player's perspective by adding damage and displaying visual 
            // effect on player
            // - DamageEnemies.DoDamage Postfix adds the effect on enemy that got hit (PlayCritVisualOnEnemyHit's logic)

            if (target == null || target.layer != 11) return;
            if (!IsHeroDamage(__instance)) return;

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
        public static void PlayCritVisualOnEnemyHit(DamageEnemies instance, GameObject target, bool result)
        {
            if (!result || target == null || target.layer != 11) return;
            if (!IsHeroDamage(instance)) return;

            if (Crit)
            {
                // effect comes from prefab value, these can have both visual and sound included. 
                // Prefer one with no audio + only mild visuals; you will be hearing/seeing this effect a lot if
                // you level up luck stat
                var prefab = HeroController.instance.takeHitBlackHealthNullifyPrefab;
                prefab.transform.SetScaleX(0.5f);
                prefab.transform.SetScaleY(0.5f);
                Instantiate(prefab, target.transform.position, target.transform.rotation);
            }
        }


        private readonly static FieldInfo IsHeroDamageField = typeof(DamageEnemies)
                .GetField("isHeroDamage", BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly static FieldInfo SourceIsHeroField = typeof(DamageEnemies)
            .GetField("sourceIsHero", BindingFlags.Instance | BindingFlags.NonPublic);

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

        // crit visuals
        // the visual effect in HealthManager_Hit (FlashGrimmflame) only applies to the player. To also display 
        // effect on the enemy, DoDamage needs to explicitly check if damage was inflicted by player.
        private static bool IsHeroDamage(DamageEnemies instance)
        {
            bool a = (bool)(IsHeroDamageField?.GetValue(instance) ?? false);
            bool b = (bool)(SourceIsHeroField?.GetValue(instance) ?? false);
            return a || b;
        }
    }
}