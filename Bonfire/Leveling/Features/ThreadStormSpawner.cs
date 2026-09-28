using System.Collections;

using UnityEngine;

namespace BonfireSilksong.Leveling
{
    /// <summary>
    /// Thread Storm silk skill custom spawner. Limited to only 1 spawner at a time: another one cannot spawn until
    /// first one finished and gets discarded.
    /// </summary>
    public static class ThreadStormSpawner
    {
        /// <summary>
        /// Spawns Thread Storm effect on target object for specified duration.
        /// </summary>
        public static void CastThreadStorm(GameObject target, float effectDuration)
        {
            if (target == null || _isActive) return;

            Bonfire.Log.LogDebug($"[RuneRageSpawner] Cast Thread Storm from {target.name}.");

            GameObject hornet = HeroController.instance.gameObject;

            var hornetFsm = PlayMakerFSM.FindFsmOnGameObject(hornet, "Silk Specials");
            if (hornetFsm == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Silk Specials' FSM on Hornet.");
                return;
            }

            var sphereBallVar = hornetFsm.Fsm.Variables.FindFsmGameObject("Sphere Ball");
            GameObject sphereBall = sphereBallVar?.Value;

            if (sphereBall == null)
            {
                Bonfire.Log.LogDebug("Could not find 'Sphere Ball' GameObject.");
                return;
            }

            // store original data before custom override; these are needed after spell ends to revert back to default
            // behavior.
            Transform originalParent = sphereBall.transform.parent;
            Vector3 originalLocalPos = sphereBall.transform.localPosition;
            Quaternion originalLocalRot = sphereBall.transform.localRotation;

            sphereBall.SetActive(false);

            // thread storm is always cast relative to Hornet's position. Its transform needs to be temporarily detached
            // to allow free positioning on target enemy..
            sphereBall.transform.SetParent(null, worldPositionStays: false);
            sphereBall.transform.position = target.transform.position;

            sphereBall.SetActive(true);
            _isActive = true;

            float sphereDuration = effectDuration;
            // required FSM variables: 'Timer' doesn't seem to have specific impact even if default value of 0.5f is
            // changed, 'Sphere Timer' however controls the actual length.
            hornetFsm.Fsm.Variables.FindFsmBool("Is Sphere Active").Value = true;
            hornetFsm.Fsm.Variables.FindFsmFloat("Sphere Timer").Value = sphereDuration;
            //hornetFsm.Fsm.Variables.FindFsmFloat("Timer").Value = 0.5f;

            Bonfire.Instance.StartCoroutine(
                EndThreadStormAfter(hornetFsm, sphereBall, sphereDuration, originalParent, originalLocalPos,
                    originalLocalRot));
        }

        private static bool _isActive = false;


        private static IEnumerator EndThreadStormAfter(PlayMakerFSM hornetFsm, GameObject sphereBall, float duration,
            Transform originalParent, Vector3 originalLocalPos, Quaternion originalLocalRot)
        {
            yield return new WaitForSeconds(duration);

            hornetFsm.Fsm.Variables.FindFsmBool("Is Sphere Active").Value = false;
            sphereBall.SetActive(false);

            // reattach position transform and update back to original position/rotation, otherwise normal use of 
            // thread storm becomes bugged and spawns no projectile
            sphereBall.transform.SetParent(originalParent, worldPositionStays: false);
            sphereBall.transform.localPosition = originalLocalPos;
            sphereBall.transform.localRotation = originalLocalRot;

            _isActive = false;

            Bonfire.Log.LogDebug("[ThreadStormSpawner] Thread Storm ended.");
        }
    }
}