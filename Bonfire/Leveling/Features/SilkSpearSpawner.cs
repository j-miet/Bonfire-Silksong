using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BonfireSilksong.Leveling
{
    /// <summary>
    /// Silk spear silk skill custom spawner.
    /// </summary>
    public static class SilkSpearSpawner
    {
        /// <summary>
        /// Casts silk spear skill without cost or locking player into animation.
        /// </summary>
        public static void CastSilkSpear()
        {
            Bonfire.Log.LogDebug($"[SilkSpearSpawner] Cast Silk Spear.");

            GameObject hornet = HeroController.instance.gameObject;

            // - Silk Spear spawning -

            // FindDeepChild should always fire here because 'Needle Throw' is not a direct child, initial find can 
            // still stay for now
            Transform needleThrow = hornet.transform.Find("Needle Throw")
                ?? FindDeepChild(hornet.transform, "Needle Throw");

            if (needleThrow == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Needle Throw' object.");
            }

            GameObject silkSpear = needleThrow.gameObject;

            // reset pattern to put FSM in active state.
            silkSpear.SetActive(false);
            silkSpear.SetActive(true);

            float facing = hornet.transform.localScale.x >= 0 ? 1f : -1f; // Hornet facing direction
            float speed = 30f; // not the 100% accurate projectile speed, but also easy to adjust here.

            var rb = silkSpear.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = new Vector2(speed * facing, 0f);

            // - Flash prefab -
            var hornetFsm = PlayMakerFSM.FindFsmOnGameObject(hornet, "Silk Specials");
            if (hornetFsm == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Silk Specials' FSM on Hornet.");
                return;
            }

            GameObject spearPrefab = GetPrefabAction(hornetFsm, "Start Throw", 0);
            if (spearPrefab == null)
            {
                Bonfire.Log.LogDebug("Could not find prefab in 'Start Throw' state.");
                return;
            }

            Vector3 hornetPos = hornet.transform.position;

            SpawnFromPool(spearPrefab, hornetPos, Quaternion.identity, hornet);
        }


        private static Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;

                var found = FindDeepChild(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static GameObject GetPrefabAction(PlayMakerFSM fsm, string stateName, int spawnActionIndex)
        {
            var state = fsm.Fsm.GetState(stateName);
            if (state == null) return null;

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