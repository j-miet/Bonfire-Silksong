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
        public static void RemoveUnused(float dt)
        {
            toRemove.Clear();

            foreach (var e in EnemyMaxHp.Keys)
            {
                if (ShouldRemove(e, true))
                    toRemove.Add(e);
            }

            foreach (var e in toRemove)
            {
                EnemyMaxHp.Remove(e);
                EnemyIsBoss.Remove(e);
                EnemyColliders.Remove(e);
                EnemyRenderers.Remove(e);
            }

            // if enemies respawn, they are probably recycled from object pool. Therefore OnEnable and consequently
            // AddEnemyHpData are never called. This check performs rescan to add repooled entities. 
            _checkTimer -= dt;
            if (_checkTimer <= 0)
            {
                _checkTimer = 1f;
                RescanForMissing();
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
        private static float _checkTimer = 1f;

        private static void RescanForMissing()
        {
            var all = Object.FindObjectsByType<HealthManager>(FindObjectsSortMode.None);
            foreach (var hm in all)
            {
                if (!ShouldRemove(hm, false))
                    AddEnemyHpData(hm);
            }
        }

        private static bool ShouldRemove(HealthManager hm, bool printLogs)
        {
            bool removed = hm == null || hm.hp <= 0 || hm.isDead || !hm.isActiveAndEnabled || !hm.enabled;
            if (hm != null && removed && printLogs)
            {
                Bonfire.Log.LogDebug($"[EnemyHealth]: Removed enemy {hm.name}");
                Bonfire.Log.LogDebug($"hp: {hm.hp}, id={hm.GetInstanceID()}, enabled: {hm.enabled}, " +
                    $"isActive: {hm.isActiveAndEnabled}, isDead: {hm.isDead}");
            }

            return removed;
        }
    }
}