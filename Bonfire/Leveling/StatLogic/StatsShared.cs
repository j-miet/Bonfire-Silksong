using System.Collections.Generic;
using System.Reflection;

namespace BonfireSilksong
{
    /// <summary>
    /// Logic shared between multiple stat classes.
    /// </summary>
    public static class StatsShared
    {
        public static int CritRoll = 0;
        public static bool Crit = false;
        public static bool BlockNextHit = false;

        public static readonly FieldInfo AttackCooldownField = typeof(HeroController)
            .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic);

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
            if (GameManager.instance.inputHandler.inputActions.Attack.WasPressed == true)
            {
                Bonfire.Log.LogDebug("[SharedStatLogic] CritRoll updated");
                CritRoll = UnityEngine.Random.Range(1, 100);
            }
        }
    }
}