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
                    var gui = BonfireGUI.Settings;

                    // get enemy hp data for GUI to draw their hp bars
                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }

                    LuckLogic.UpdateCurrencyDrops(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(HitInstance hitInstance)
            {
                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Bonfire.Log.LogInfo("[HealthManager.Hit] Recorded a hit => Hit source: " + sourceName);
                Bonfire.Log.LogInfo("Damage Dealt: " + hitInstance.DamageDealt);
                Bonfire.Log.LogInfo("Tool instance: " + tool?.name ?? "null");

                // == Important ==
                // - percentage damage scaling: just use hitInstance.DamageDealt and scale it
                // - flat scaling: at this point the game has already applied all damage buffs if there are any. Thus
                // using simply hitInstance.DamageDealt = newValue would not modify the base value, making any damage
                // buffs e.g. Flintslate for needle hits, useless. Solution is to pick one of following routes: 
                // - find the base damage of damage source and patch it e.g. PlayerData.nailDamage getter for needle 
                // base damage
                // - write a custom formula which also checks all possible buffs conditions this source can receive.
                // - apply any increments post-buff, meaning the flat +X damage doesn't benefit from any buffs.
                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // the "Type == ToolItemType.Skill" should already target only the silk skills.

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