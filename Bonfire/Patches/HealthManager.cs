using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class HealthManager_Patch
    {
        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        class HealthManager_OnEnable
        {
            static void Postfix(HealthManager __instance)
            {
                if (__instance != null)
                {
                    EnemyHealth.AddEnemyHpData(__instance);

                    LevelingPatch.UpdateCurrencyDrops(__instance);
                }
            }
        }

        // This inserts custom getter into needle damage DoT effect calculation i.e. DoTs will also scale from 
        // leveling Strength stat.
        //
        // public void DoLagHits(LagHitOptions options, HitInstance hitInstance)
        [HarmonyPatch(typeof(HealthManager), "DoLagHits")]
        class HealthManager_DoLagHits
        {
            static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    if (i + 1 < codes.Count &&
                        codes[i].Calls(LevelingPatch.PlayerDataInstanceGetter) &&
                        codes[i + 1].Calls(LevelingPatch.PlayerDataNailDamageGetter))
                    {
                        Bonfire.Log.LogInfo(
                            "[HealthManager.DoLagHits] Transpiler: found PlayerData.instance.nailDamage");

                        var first = new CodeInstruction(OpCodes.Ldarg_2);
                        first.labels.AddRange(codes[i].labels);

                        var second = new CodeInstruction(
                            OpCodes.Call,
                            LevelingPatch.CustomNailDamageGetterHitInstance);
                        second.labels.AddRange(codes[i + 1].labels);

                        yield return first;
                        yield return second;

                        i++;
                        continue;
                    }

                    yield return codes[i];
                }
            }
        }

        // Use ref here because HitInstance is a struct (value type) and its damage value won't update otherwise.
        //
        // public IHitResponder.HitResponse Hit(HitInstance hitInstance)
        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(HealthManager __instance, ref HitInstance hitInstance)
            {
                LevelingPatch.ApplyHitBonuses(__instance, ref hitInstance);
            }
        }
    }
}