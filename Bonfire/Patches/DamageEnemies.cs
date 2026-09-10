using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;
using UnityEngine;

using BonfireSilksong.Leveling;

namespace BonfireSilksong.Patches
{
    internal class DamageEnemies_Patch
    {
        // Override needle damage getter with custom behavior. This allows blocking strength damage scaling for
        // specific damage sources.
        //
        // public bool DoDamage(GameObject target, bool isFirstHit)
        [HarmonyPatch(typeof(DamageEnemies), "DoDamage", new[] {
            typeof(GameObject),
            typeof(bool)
        })]
        class DamageEnemies_DoDamage
        {
            static IEnumerable<CodeInstruction> Transpiler(
                IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    // idea: find PlayerData.instance.nailDamage instruction. Here both instance and nailDamage are
                    // getter properties. Find the index where instance gets called and nailDamage right after it.
                    // Then instead of original code: insert first method argument which corresponds to 'this' i.e. the
                    // DamageEnemies instance, and follow it with custom getter implementation call. Thus, this gives
                    // exact signature CustomNeedleDamageGetter(DamageEnemies instance). Finally, move index manually 
                    // forward one step: 2 instructions were added so this skips the yielding of original 
                    // nailDamage getter.
                    // It's also important to preserve labels: these work as sort of references to rest of method 
                    // code: if something calls the PlayerData.instance.nailDamage again during DoDamage execution, it 
                    // needs to do it through the new custom getter. Otherwise the old method is used instead which 
                    // is now no longer part of instructions, causing transpiler to fail and also crash the mod 
                    // patching (unless this  exception is caught, but better let this happen and fix it).
                    if (i + 1 < codes.Count &&
                        codes[i].Calls(LevelingPatch.PlayerDataInstanceGetter) &&
                        codes[i + 1].Calls(LevelingPatch.PlayerDataNailDamageGetter))
                    {
                        Bonfire.Log.LogInfo(
                            "[DamageEnemies.DoDamage] Transpiler: found PlayerData.instance.nailDamage");

                        var first = new CodeInstruction(OpCodes.Ldarg_0);
                        first.labels.AddRange(codes[i].labels);

                        var second = new CodeInstruction(
                            OpCodes.Call,
                            LevelingPatch.CustomNailDamageGetterDamageEnemies);
                        second.labels.AddRange(codes[i + 1].labels);

                        yield return first;
                        yield return second;

                        i++;
                        continue;
                    }

                    yield return codes[i];
                }
            }

            static void Postfix(GameObject target)
            {
                LuckStat.PlayCritVisualOnEnemyHit(target);
            }
        }
    }
}