using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// For spawning game's own prefab effects via GameObjects. Common ones can be found in 
    /// - HeroController.instance
    /// - GlobalSettings.GamePlay
    /// - GlobalSettings.Effects
    /// but these are found under many other namespaces/classes and tend to include words 'prefab' or 'effect' in them.
    /// 
    /// There are more, but these are not directly gameplay related (UI/menu prefabs) or private fields.
    /// Also most of the prefabs don't seem to cause any effect or are impractical to use: they hurt player, activate
    /// other undesired and gameplay-modifying events, can leave lingering effects etc.
    /// </summary>
    public static class SpawnPrefab
    {
        /// <summary>
        /// Spawns a temporal visual prefab on player.
        /// </summary>
        public static void SpawnHero(GameObject prefab, float destroyTimer = 3f)
        {
            var player = HeroController.instance;

            var effect = Object.Instantiate(
                prefab,
                player.transform.position,
                player.transform.rotation
            );

            //effect.transform.SetScaleX(0.5f);
            //effect.transform.SetScaleY(0.5f);

            Object.Destroy(effect, destroyTimer);
        }

        /// <summary>
        /// Spawns a temporal visual prefab on target GameObject.
        /// </summary>
        public static void Spawn(GameObject target, GameObject prefab, float destroyTimer = 3f)
        {
            var effect = Object.Instantiate(
                prefab,
                target.transform.position,
                target.transform.rotation
            );

            effect.transform.SetScaleX(0.5f);
            effect.transform.SetScaleY(0.5f);

            Object.Destroy(effect, destroyTimer);
        }
    }
}