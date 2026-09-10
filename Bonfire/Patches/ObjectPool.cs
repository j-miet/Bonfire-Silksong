using HarmonyLib;
using UnityEngine;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class ObjectPool_Patch
    {
        // public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation
        //      bool stealActiveSpawned)
        [HarmonyPatch(typeof(ObjectPool), "Spawn", new[] {
            typeof(GameObject),
            typeof(Transform),
            typeof(Vector3),
            typeof(Quaternion),
            typeof(bool)
        })]
        class ObjectPool_Spawn
        {
            static void Postfix(GameObject __result)
            {
                ArtificeStat.ScaleSawtoothCircletRange(__result);
            }
        }
    }
}