using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BonfireSilksong.Leveling
{
    /// <summary>
    /// Pale Nails silk skill custom spawner.
    /// </summary>
    public static class PaleNailsSpawner
    {
        /// <summary>
        /// Simulates Pale Nails silk skill spawn without player animation or silk cost.
        /// </summary>
        public static void CastPaleNails()
        {
            Bonfire.Log.LogDebug("[NeedleSpellSpawner] Cast Pale Nails.");

            GameObject hornet = HeroController.instance.gameObject;

            var hornetFsm = PlayMakerFSM.FindFsmOnGameObject(hornet, "Silk Specials");
            if (hornetFsm == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Silk Specials' FSM on Hornet.");
                return;
            }

            GameObject needlePrefab = GetPrefabAction(hornetFsm, "BossNeedle Cast", 0);
            if (needlePrefab == null)
            {
                Bonfire.Log.LogDebug("Could not find prefab in 'BossNeedle Cast' state.");
                return;
            }

            Vector3 hornetPos = hornet.transform.position;

            // spawn 3 identical GameObjects for needles
            var a = SpawnFromPool(needlePrefab, hornetPos, Quaternion.identity, hornet);
            var b = SpawnFromPool(needlePrefab, hornetPos, Quaternion.identity, hornet);
            var c = SpawnFromPool(needlePrefab, hornetPos, Quaternion.identity, hornet);

            Bonfire.Log.LogDebug($"Spawned needle objects '{a.name}'");

            // however each needle (top, middle, bottom) must call its own event.
            TriggerEvent(a, "Control", "TOP1");
            TriggerEvent(b, "Control", "MID1");
            TriggerEvent(c, "Control", "BOT1");
        }


        private static GameObject GetPrefabAction(PlayMakerFSM fsm, string stateName, int spawnActionIndex)
        {
            var state = fsm.Fsm.GetState(stateName);
            if (state == null) return null;

            // here "Hornet Finger Blade" is actually the wanted prefab so this indexing is not really necessary:
            // index 0 already matches to this. But for a more general purpose tool where index is not the first one, 
            // this could be useful and stay this way.
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

            // initialize pooled object with default position position and angle
            GameObject spawned = prefab.Spawn(position);
            spawned.transform.eulerAngles = rotation.eulerAngles;

            // this seems to be only for Act 3 enemy visuals when they get voided and thus does nothing for the player
            // projectiles.
            // BlackThreadState.HandleDamagerSpawn(owner, spawned);

            return spawned;
        }

        private static void TriggerEvent(GameObject go, string fsmName, string eventName)
        {
            if (go == null) return;

            var fsm = PlayMakerFSM.FindFsmOnGameObject(go, fsmName);
            if (fsm == null)
            {
                Bonfire.Log.LogDebug($"No FSM '{fsmName}' found on {go.name}");
                return;
            }
            fsm.Fsm.Event(eventName);
        }
    }
}