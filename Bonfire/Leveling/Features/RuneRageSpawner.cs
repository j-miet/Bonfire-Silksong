using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Rune Rage silk skill custom spawner.
    /// </summary>
    public static class RuneRageSpawner
    {
        /// <summary>
        /// Simulates Rune Rage silk skill spawn without player animation or silk cost. Skill is cast from target
        /// GameObject.
        /// </summary>
        public static void CastRuneRage(GameObject target)
        {
            if (target == null) return;

            Bonfire.Log.LogDebug($"[RuneRageSpawner] Cast Rune Rage from {target.name}.");

            GameObject hornet = HeroController.instance.gameObject;

            var hornetFsm = PlayMakerFSM.FindFsmOnGameObject(hornet, "Silk Specials");
            if (hornetFsm == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Silk Specials' FSM on Hornet.");
                return;
            }

            GameObject runePrefab = GetPrefabAction(hornetFsm, "Blast Enemy", 0);
            if (runePrefab == null)
            {
                Bonfire.Log.LogDebug("Could not find prefab in 'Blast Enemy' state.");
                return;
            }

            Vector3 targetPos = target.transform.position;

            // Rune Rage creates normally 9 runes, but each prefab only seems to spawn up to 3, requiring 3 spawns.
            SpawnFromPool(runePrefab, targetPos, Quaternion.identity, hornet);
            SpawnFromPool(runePrefab, targetPos, Quaternion.identity, hornet);
            SpawnFromPool(runePrefab, targetPos, Quaternion.identity, hornet);
        }


        private static GameObject GetPrefabAction(PlayMakerFSM fsm, string stateName, int spawnActionIndex)
        {
            var state = fsm.Fsm.GetState(stateName);
            if (state == null) return null;

            // "Weaver Bomb Blast Cluster" is the only prefab so it's always at index 0.
            int found = 0;
            foreach (var action in state.Actions)
            {
                if (action is SpawnObjectFromGlobalPool spawnAction)
                {
                    if (found == spawnActionIndex)
                        return spawnAction.gameObject.Value;

                    found++;
                }
            }
            return null;
        }

        private static GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation,
            GameObject owner)
        {
            if (prefab == null) return null;

            PersonalObjectPool.EnsurePooledInScene(owner, prefab, 1);

            GameObject spawned = prefab.Spawn(position);
            spawned.transform.eulerAngles = rotation.eulerAngles;

            return spawned;
        }
    }
}