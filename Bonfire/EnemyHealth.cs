using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Enemy health tracking.
    /// </summary>
    public static class EnemyHealth
    {
        public static readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        public static readonly Dictionary<HealthManager, bool> EnemyIsBoss = new Dictionary<HealthManager, bool>();
        public static readonly Dictionary<HealthManager, Collider2D> EnemyColliders =
            new Dictionary<HealthManager, Collider2D>();
        public static readonly Dictionary<HealthManager, Renderer> EnemyRenderers =
            new Dictionary<HealthManager, Renderer>();

        public static bool IsEnabled(HealthManager hm)
        {
            if (hm == null) return false;

            var collider = EnemyColliders.TryGetValue(hm, out var c) ? c : null;
            if (collider != null && collider.enabled) return true;

            var renderer = EnemyRenderers.TryGetValue(hm, out var r) ? r : null;
            if (renderer != null && renderer.enabled) return true;

            return false;
        }

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Add enemy hp data.
        /// </summary>
        public static void AddEnemyHpData(HealthManager instance)
        {
            if (!EnemyMaxHp.ContainsKey(instance))
            {
                EnemyMaxHp.Add(instance, instance.hp);
                EnemyIsBoss.Add(instance, BossSceneController.Instance != null);

                if (instance.GetComponentInChildren<Collider2D>() is Collider2D c)
                    EnemyColliders.Add(instance, c);
                if (instance.GetComponentInChildren<Renderer>() is Renderer r)
                    EnemyRenderers.Add(instance, r);

                Bonfire.Log.LogDebug($"[EnemyHealth] Filled data for {instance.name}");
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Flush used enemy hp data.
        /// </summary>
        public static void RemoveUnused()
        {
            toRemove.Clear();

            foreach (var e in EnemyMaxHp.Keys)
            {
                if (e == null || e.hp <= 0 || e.isDead || !e.isActiveAndEnabled || !e.enabled)
                {
                    if (e != null)
                    {
                        Bonfire.Log.LogDebug($"[EnemyHealth]: Removed enemy {e.name}\n" +
                            $"hp: {e.hp},  id={e.GetInstanceID()} enabled: {e.enabled}, " +
                            $"isActive: {e.isActiveAndEnabled}, isDead: {e.isDead}");
                    }

                    toRemove.Add(e);
                }
            }

            foreach (var e in toRemove)
            {
                EnemyMaxHp.Remove(e);
                EnemyIsBoss.Remove(e);
                EnemyColliders.Remove(e);
                EnemyRenderers.Remove(e);
            }
        }

        /// <summary>
        /// HeroController.LeaveScene (Prefix): Reset all hp data before after leaving current scene.
        /// </summary>
        public static void ResetEnemyHpData()
        {
            EnemyMaxHp.Clear();
            EnemyIsBoss.Clear();
            EnemyColliders.Clear();
            EnemyRenderers.Clear();

            Bonfire.Log.LogDebug("[EnemyHealth] Reset all enemy hp data");
        }


        private static readonly List<HealthManager> toRemove = new List<HealthManager>();
    }
}