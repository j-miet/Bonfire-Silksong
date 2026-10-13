using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Enemy HealthManager tracking.
    /// </summary>
    public static class EnemyHealth
    {
        // Cogwork Dancers set hp temporarily to 99999f on phase transitions, this sentinel value is slightly lower
        // than that and should prevent any temporary values from being added to hp tracking pool. Also useful for
        // GUI to stop re-rendering hp bars when this happens.
        public const float HpResetSentinel = 90000f;

        // stores initial hp when HealthManager is loaded (which might not end up being the even the actual full hp 
        // value) and serves as a general enemy HealthManager tracker for active enemies on scene.
        public static readonly Dictionary<HealthManager, float> EnemyMaxHp = new Dictionary<HealthManager, float>();
        // this tracks enemy base currency values
        public static readonly Dictionary<HealthManager, (int small, int medium, int large, int shards)>
            EnemyCurrencyBaseValues = new Dictionary<HealthManager, (int small, int medium, int large, int shards)>();

        public static bool IsEnabled(HealthManager hm)
        {
            if (hm == null) return false;

            var collider = _enemyColliders.TryGetValue(hm, out var c) ? c : null;
            if (collider != null && collider.enabled) return true;

            var renderer = _enemyRenderers.TryGetValue(hm, out var r) ? r : null;
            if (renderer != null && renderer.enabled) return true;

            return false;
        }

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Add enemy hp data.
        /// </summary>
        public static void AddEnemyHpData(HealthManager instance)
        {
            if (instance == null)
                return;

            if (EnemyMaxHp.ContainsKey(instance))
                return;

            float initialHp = instance.hp;

            if (initialHp >= HpResetSentinel)
                return;

            bool isBoss = BossSceneController.IsBossScene;
            EnemyMaxHp.Add(instance, initialHp);

            if (instance.GetComponentInChildren<Collider2D>() is Collider2D c)
                _enemyColliders.Add(instance, c);
            if (instance.GetComponentInChildren<Renderer>() is Renderer r)
                _enemyRenderers.Add(instance, r);

            Bonfire.Log.LogDebug(
                $"[EnemyHealth] Added {instance.name}, id={instance.GetInstanceID()}, hp={initialHp}, boss={isBoss}");
        }

        /// <summary>
        /// HeroController.Update (Postfix): Update hp by removing stale references and updating hp values if current
        /// ones have mismatches between current and actual values.
        /// </summary>
        public static void UpdateHealthValues(float dt)
        {
            _toRemove.Clear();
            _enemyTemp.Clear();

            // snapshot the keys because UpdateHealthData() can modify EnemyMaxHp while this loop is running.
            _enemyTemp.AddRange(EnemyMaxHp.Keys);

            foreach (var e in _enemyTemp)
            {
                if (ShouldRemove(e, true))
                {
                    _toRemove.Add(e);
                    continue;
                }

                UpdateMaxHPData(e);
            }

            foreach (var e in _toRemove)
            {
                EnemyMaxHp.Remove(e);
                EnemyCurrencyBaseValues.Remove(e);
                _enemyColliders.Remove(e);
                _enemyRenderers.Remove(e);
            }

            // if enemies respawn, they are probably recycled from object pool. Therefore OnEnable and consequently
            // AddEnemyHpData are never called. This check performs rescan to add repooled entities. 
            _checkTimer -= dt;
            if (_checkTimer <= 0f)
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
            EnemyCurrencyBaseValues.Clear();
            _enemyColliders.Clear();
            _enemyRenderers.Clear();

            _checkTimer = 0f;

            Bonfire.Log.LogDebug("[EnemyHealth] Reset all enemy hp data");
        }


        private static readonly Dictionary<HealthManager, Collider2D> _enemyColliders =
            new Dictionary<HealthManager, Collider2D>();
        private static readonly Dictionary<HealthManager, Renderer> _enemyRenderers =
            new Dictionary<HealthManager, Renderer>();
        private static readonly List<HealthManager> _toRemove = new List<HealthManager>();
        private static readonly List<HealthManager> _enemyTemp = new List<HealthManager>();
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
            if (hm == null)
                return true;

            bool removed = hm.isDead || !hm.isActiveAndEnabled || !hm.enabled || hm.hp <= 0;

            if (removed && printLogs)
            {
                Bonfire.Log.LogDebug($"[EnemyHealth]: Removed enemy {hm.name}");
                Bonfire.Log.LogDebug($"hp: {hm.hp}, id={hm.GetInstanceID()}, enabled: {hm.enabled}, " +
                    $"isActive: {hm.isActiveAndEnabled}, isDead: {hm.isDead}");
            }

            return removed;
        }

        private static void UpdateMaxHPData(HealthManager hm)
        {
            if (!EnemyMaxHp.TryGetValue(hm, out float maxHp))
                return;

            float currentHp = hm.hp;

            if (currentHp >= HpResetSentinel)
                return;

            if (currentHp > maxHp)
            {
                EnemyMaxHp[hm] = currentHp;
                Bonfire.Log.LogDebug($"[EnemyHealth] Update max hp for {hm.name}: {maxHp} -> {currentHp}");
            }
        }
    }
}