using System.Collections.Generic;

namespace BonfireSilksong
{
    /// <summary>
    /// Enemy health tracking.
    /// </summary>
    public static class EnemyHealth
    {
        public static readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public static readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Add enemy hp data.
        /// </summary>
        public static void AddEnemyHpData(HealthManager instance)
        {
            if (!EnemyMaxHp.ContainsKey(instance))
            {
                EnemyMaxHp.Add(instance, instance.hp);
                EnemyIsBoss.Add(instance, BossSceneController.Instance != null);
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Flush used enemy hp data.
        /// </summary>
        public static void FlushEnemyHPData()
        {
            var toRemove = new List<HealthManager>();

            // flush used enemy hp bar data
            foreach (var e in EnemyMaxHp.Keys)
            {
                if (e == null || e.hp <= 0)
                    toRemove.Add(e);
            }
            foreach (var e in toRemove)
            {
                EnemyMaxHp.Remove(e);
                EnemyIsBoss.Remove(e);
            }
        }
    }
}