using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

namespace BonfireSilksong
{
    internal class DamageEnemies_Patch
    {
        [HarmonyPatch(typeof(DamageEnemies), "DoDamage", new[] { typeof(GameObject), typeof(bool) })]
        class DamageEnemies_DoDamage
        {
            static void Prefix(DamageEnemies __instance, GameObject target)
            {
                LuckLogic.CheckIfCrit(__instance, target);
            }

            static void Postfix(DamageEnemies __instance, GameObject target, bool __result)
            {
                LuckLogic.PlayCritVisualOnEnemyHit(__instance, target, __result);
            }
        }
    }
}