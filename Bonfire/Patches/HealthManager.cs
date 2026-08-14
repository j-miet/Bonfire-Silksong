using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

namespace BonfireSilksong
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
                    StatsShared.AddEnemyHpData(__instance);

                    LuckLogic.UpdateCurrencyDrops(__instance);
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
                        codes[i].Calls(StatsShared.PlayerDataInstanceGetter) &&
                        codes[i + 1].Calls(StatsShared.PlayerDataNailDamageGetter))
                    {
                        Bonfire.Log.LogInfo(
                            "[HealthManager.DoLagHits] Transpiler: found PlayerData.instance.nailDamage");

                        var first = new CodeInstruction(OpCodes.Ldarg_2);
                        first.labels.AddRange(codes[i].labels);

                        var second = new CodeInstruction(
                            OpCodes.Call,
                            StatsShared.CustomNailDamageGetterHitInstance);
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

        // public IHitResponder.HitResponse Hit(HitInstance hitInstance)
        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(HitInstance hitInstance)
            {
                StatsShared.ApplyHitDamageBonuses(hitInstance);
            }
        }
    }
}