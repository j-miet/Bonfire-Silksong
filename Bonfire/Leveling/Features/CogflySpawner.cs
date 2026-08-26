using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Cogfly tool custom spawner.
    /// </summary>
    public static class CogflySpawner
    {
        /// <summary>
        /// Spawns Cogflies until max limit is hit. Will automatically replace inactive objects.
        /// </summary>
        public static void SpawnCogflies()
        {
            _activeCogflies.RemoveAll(c => c == null || !c.activeInHierarchy);

            if (_activeCogflies.Count >= _maxCogflies) return;

            for (int i = 0; i < _maxCogflies; i++)
                SpawnCogfly();
        }

        /// <summary>
        /// Despawns all custom cogflies.
        /// </summary>
        public static void DespawnAllCogflies()
        {
            if (_activeCogflies.Count <= 0) return;

            foreach (var cogfly in _activeCogflies)
            {
                if (cogfly == null || !cogfly.activeInHierarchy)
                    continue;

                var hatchling = cogfly.GetComponent<ClockworkHatchling>();
                if (hatchling != null)
                {
                    hatchling.ClearEffects(true); // pass 'unequipped = true' to play animation + sound
                }
                else
                {
                    cogfly.Recycle(); // fallback
                }
            }

            _activeCogflies.Clear();
        }

        /// <summary>
        /// Update custom Cogfly spawn limit (default is 1).
        /// </summary>
        public static void SetCogflySpawnLimit(int limit = 1)
        {
            if (limit < CogflyDefaultMaxValue)
            {
                _maxCogflies = CogflyDefaultMaxValue;
            }
            else
            {
                _maxCogflies = limit;
            }

            Bonfire.Log.LogInfo($"[CogflySpawner] Max cogflies set to {_maxCogflies}.");
        }


        private const int CogflyDefaultMaxValue = 1;

        private static readonly List<GameObject> _activeCogflies = new List<GameObject>();
        private static int _maxCogflies = CogflyDefaultMaxValue;

        private static readonly FieldInfo _usageOptionsField = typeof(ToolItemBasic)
            .GetField("usageOptions", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _throwPrefabField = typeof(ToolItem.UsageOptions)
            .GetField("ThrowPrefab", BindingFlags.Instance | BindingFlags.Public);

        private static bool SpawnCogfly()
        {
            Bonfire.Log.LogInfo($"[CogflySpawner] Spawning Cogfly.");

            if (_activeCogflies.Count >= _maxCogflies)
            {
                Bonfire.Log.LogInfo($"Max cogflies ({_maxCogflies}) reached, cannot to spawn another.");
                return false;
            }

            ToolItem t = ToolItemManager.GetToolByName("Cogwork Flier");
            if (t == null)
            {
                Bonfire.Log.LogInfo("Tool 'Cogwork Flier' not found.");
                return false;
            }

            ToolItem.UsageOptions usageOptions = (ToolItem.UsageOptions)_usageOptionsField.GetValue(t);
            GameObject cogflyPrefab = (GameObject)_throwPrefabField.GetValue(usageOptions);

            if (cogflyPrefab == null)
            {
                Bonfire.Log.LogInfo("ThrowPrefab was null.");
                return false;
            }

            GameObject hornet = HeroController.instance.gameObject;
            Vector3 spawnPos = hornet.transform.position;

            PersonalObjectPool.EnsurePooledInScene(hornet, cogflyPrefab, 1);
            GameObject spawned = cogflyPrefab.Spawn(spawnPos);

            _activeCogflies.Add(spawned);

            return true;
        }
    }
}