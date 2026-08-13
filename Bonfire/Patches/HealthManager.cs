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
                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Bonfire.Log.LogDebug("[HealthManager.Hit] Recorded a hit => Hit source: " + sourceName);
                Bonfire.Log.LogDebug("Damage Dealt: " + hitInstance.DamageDealt);
                Bonfire.Log.LogDebug("Tool instance: " + tool?.name ?? "null");

                // == Important ==
                // 1. Multiplicative damage scaling: just use hitInstance.DamageDealt and scale it
                // 2. Additive scaling: at this point the game has already applied all damage buffs if there are any. 
                // Thus using simply 'hitInstance.DamageDealt = newValue' would not modify the base value, making any 
                // damage buffs e.g. Flintslate for needle hits, useless. Solution is to pick one of following routes: 
                // - find the base damage of damage source and patch it e.g. PlayerData.nailDamage getter for needle 
                // base damage.
                // - write a custom formula which also checks all possible buffs conditions this source can receive.
                // - apply any increments post-buff, meaning the flat +X damage doesn't benefit from majority of buffs.
                // An exception would be Wanderer crest crit damage bonus as game applies this during its final damage
                // calculations in HealthManager.TakeDamage.
                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // 1. the "Type == ToolItemType.Skill" should already target only the silk skills, but in case 
                    // exact names are needed for hitInstance.RepresentingTool.name, these are:
                    //   Silk Spear, Thread Sphere, Parry, Silk Charge, Silk Bomb, Silk Boss Needle
                    // 2. Silk skill damage scales normally from needle upgrades, or specifically, the needle damage
                    // itself. To balance strength stat: only base game scaling works, after that intelligence stat
                    // is necessary for extra scaling. As scaling requires adjusting base needle damage, it's already
                    // done earlier in patched Playerdata.nailDamage.

                    hitInstance.DamageDealt = IntelligenceLogic.ApplySpellDamage(hitInstance.DamageDealt, tool);
                }
                else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
                {
                    // -- red and blue tools --

                    hitInstance.DamageDealt = ArtificeLogic.ApplyToolDamage(hitInstance.DamageDealt, tool);
                }
                else if (hitInstance.IsUsingNeedleDamageMult)
                {
                    // -- basic needle hits + needle strike --
                    // 1. The sourceName with Slash/UpSlash/DownSlash/AltSlash can be used to identify the direction,
                    // slash being the normal left/right slash, and apply extra damage bonus e.g. on downward pogo 
                    // hits.
                    // 2. However needle strike for different crests use various different names. For these, the
                    // IsUsingNeedleDamageMult is better identifier unless specific needle strike damage components
                    // need to be scaled individually.
                    // 3. IsUsingNeedleDamageMult would also apply to Silk Skills because in vanilla they do scale
                    // from needle upgrade count. However, to better balance silk skills, they require intelligence
                    // stat for scaling to avoid the issue of making strength a better scaling path for spells while
                    // still leaving all the spell-oriented secondary milestones under intelligence stat itself,
                    // requiring leveling both.
                    // -> make sure to put this hitInstance.IsUsingNeedleDamageMult else-if block after 
                    // ToolItemType.Skill check.

                    int updatedDamage = StrengthLogic.ApplyNeedleDamageBonuses(hitInstance.DamageDealt, sourceName);
                    hitInstance.DamageDealt = DexterityLogic.ApplyCritDamage(updatedDamage);
                }
            }
        }
    }
}