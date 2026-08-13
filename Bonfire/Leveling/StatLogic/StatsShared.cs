using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

namespace BonfireSilksong
{
    /// <summary>
    /// Logic shared between multiple stat classes.
    /// For know it also includes the few general patcher helpers e.g. enemy hp bar data stuff.
    /// </summary>
    public static class StatsShared
    {
        public static int CritRoll = 0;
        public static bool Crit = false;
        public static bool BlockNextHit = false;

        public static readonly FieldInfo AttackCooldownField = typeof(HeroController)
            .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly MethodInfo PlayerDataInstanceGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "instance");
        public static readonly MethodInfo PlayerDataNailDamageGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "nailDamage");

        public static readonly MethodInfo CustomNailDamageGetterDamageEnemies = AccessTools.Method(
            typeof(StatsShared),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(DamageEnemies) });
        public static readonly MethodInfo CustomNailDamageGetterHitInstance = AccessTools.Method(
            typeof(StatsShared),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(HitInstance) });

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Add enemy hp data.
        /// </summary>
        public static void AddEnemyHpData(HealthManager instance)
        {
            var gui = BonfireGUI.Settings;

            // get enemy hp data for GUI to draw their hp bars
            if (!gui.EnemyMaxHp.ContainsKey(instance))
            {
                gui.EnemyMaxHp.Add(instance, instance.hp);
                gui.EnemyIsBoss.Add(instance, BossSceneController.Instance != null);
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Flush used enemy hp data.
        /// </summary>
        public static void FlushEnemyHPData()
        {
            var gui = BonfireGUI.Settings;
            var toRemove = new List<HealthManager>();

            // flush used enemy hp bar data
            foreach (var e in gui.EnemyMaxHp.Keys)
            {
                if (e == null || e.hp <= 0)
                    toRemove.Add(e);
            }
            foreach (var e in toRemove)
            {
                gui.EnemyMaxHp.Remove(e);
                gui.EnemyIsBoss.Remove(e);
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Roll crit value for later check.
        /// </summary>
        public static void RollCritValue()
        {
            // currently only basic attack can crit.
            // TODO add conditional silk skill and Resilience reflect damage crit checks
            if (GameManager.instance.inputHandler.inputActions.Attack.WasPressed)
            {
                Bonfire.Log.LogDebug("[StatsShared] CritRoll updated");
                CritRoll = UnityEngine.Random.Range(1, 100);
            }
        }

        /// <summary>
        /// DamageEnemies.DoDamage (Transpiler):
        /// Override normal getter behavior: strength will scale needle base damage for all sources except silk skills. 
        /// This way intelligence keeps its own identity as spell-oriented stat. Silk skills do still have their base 
        /// game scaling.
        /// </summary>
        public static int CustomNeedleDamageGetter(DamageEnemies instance)
        {
            int damage = PlayerData.instance.nailDamage;

            if (instance.RepresentingTool != null && instance.RepresentingTool.Type == ToolItemType.Skill)
            {
                return damage;
            }

            damage = StrengthLogic.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogInfo($"[StatsShared] updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// HealthManager.DoLagHits (Transpiler): Instead of DamageEnemies, uses HitInstance.
        /// </summary>
        public static int CustomNeedleDamageGetter(HitInstance hitInstance)
        {
            int damage = PlayerData.instance.nailDamage;

            if (hitInstance.RepresentingTool != null && hitInstance.RepresentingTool.Type == ToolItemType.Skill)
            {
                return damage;
            }

            damage = StrengthLogic.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogInfo($"[StatsShared] updated needle damage={damage}");

            return damage;
        }
    }
}