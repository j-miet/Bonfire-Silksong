using System.Reflection;

using UnityEngine;

namespace BonfireSilksong.Leveling
{
    public static class VoltvesselBolaSpawner
    {
        public static void SpawnBola(GameObject target)
        {
            if (target == null) return;

            ToolItem t = ToolItemManager.GetToolByName("Lightning Rod");
            if (t == null)
            {
                Bonfire.Log.LogDebug("Tool 'Lightning Rod' not found.");
                return;
            }

            // offState = spear, onState = bola. Each is wrapped into a private class 'State' which then finally gives
            // the UsageOptions.ThrowPrefab, the GameObject itself.
            object onState = _onStateField.GetValue(t);
            ToolItem.UsageOptions usageOptions = (ToolItem.UsageOptions)_usageField.GetValue(onState);
            GameObject bolaPrefab = usageOptions.ThrowPrefab;

            if (bolaPrefab == null)
            {
                Bonfire.Log.LogDebug("Bola ThrowPrefab was null.");
                return;
            }

            GameObject hornet = HeroController.instance.gameObject;
            Vector3 spawnPos = target.transform.position;

            PersonalObjectPool.EnsurePooledInScene(hornet, bolaPrefab, 1);
            GameObject spawned = bolaPrefab.Spawn(spawnPos);

            Bonfire.Log.LogDebug($"Spawned: {spawned?.name}, active={spawned?.activeInHierarchy}");
        }

        private static readonly FieldInfo _onStateField = typeof(ToolItemToggleState)
            .GetField("onState", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo _usageField = typeof(ToolItemToggleState)
            .GetNestedType("State", BindingFlags.NonPublic | BindingFlags.Public)
            .GetField("Usage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }
}