using System;
using System.Reflection;

using HarmonyLib;

namespace BonfireSilksong
{
    /// <summary>
    /// General patch logic helpers e.g. those not tied to any specific stat implementation.
    /// </summary>
    public static class PatchHelpers
    {
        // PlayerData.instance.nailDamage
        public static readonly MethodInfo PlayerDataInstanceGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "instance");
        public static readonly MethodInfo PlayerDataNailDamageGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "nailDamage");

        public static readonly MethodInfo CustomNailDamageGetterDamageEnemies = AccessTools.Method(
            typeof(PatchHelpers),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(DamageEnemies) });
        public static readonly MethodInfo CustomNailDamageGetterHitInstance = AccessTools.Method(
            typeof(PatchHelpers),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(HitInstance) });

        /// <summary>
        /// DamageEnemies.DoDamage (Transpiler):
        /// Override normal getter behavior: strength will scale needle base damage for all sources except silk skills. 
        /// This way intelligence keeps its own identity as spell-oriented stat. Silk skills do still have their base 
        /// game scaling.
        /// </summary>
        public static int CustomNeedleDamageGetter(DamageEnemies instance)
        {
            int damage = PlayerData.instance.nailDamage;

            if (instance.RepresentingTool != null && instance.RepresentingTool.Type == ToolItemType.Skill)
            {
                return damage;
            }

            damage = StrengthLogic.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogDebug($"[PatchHelpers] getter (DamageEnemies) updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// HealthManager.DoLagHits (Transpiler): Instead of DamageEnemies, uses HitInstance.
        /// </summary>
        public static int CustomNeedleDamageGetter(HitInstance hitInstance)
        {
            int damage = PlayerData.instance.nailDamage;

            if (hitInstance.RepresentingTool != null && hitInstance.RepresentingTool.Type == ToolItemType.Skill)
            {
                return damage;
            }

            damage = StrengthLogic.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogDebug($"[PatchHelpers] getter (HitInstance) updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// HealthManager.Hit (Prefix): Handle stat damage calculations before game calls HealthManager.TakeDamage
        /// for actual health subtraction logic.
        /// Pass by references: HitInstance is a struct value type and its DamageDealt value needs to be updated.
        /// </summary>
        public static void ApplyHitDamageBonuses(ref HitInstance hitInstance)
        {
            if (hitInstance.Source == null) return;

            string sourceName = hitInstance.Source.name;
            var tool = hitInstance.RepresentingTool;

            Bonfire.Log.LogInfo("[HealthManager.Hit] Recorded a hit => Hit source: " + sourceName);
            Bonfire.Log.LogInfo("Damage Dealt: " + hitInstance.DamageDealt);
            Bonfire.Log.LogInfo("Tool instance: " + tool?.name ?? "null");

            LuckLogic.CheckIfCrit(hitInstance);
            bool isCrit = LuckLogic.Crit;

            // == Important ==
            // 1. Multiplicative damage scaling: just use hitInstance.DamageDealt and scale it.
            // 2. Additive scaling: at this point the game has already applied most if not all buffs.
            // Thus using simply 'hitInstance.DamageDealt = newValue' would not modify the base value, making any 
            // damage buffs e.g. Flintslate for needle hits, useless. Solution is to pick one of following routes: 
            // - find the base damage of damage source and patch it e.g. PlayerData.nailDamage getter for needle 
            // base damage.
            // - write a custom formula which also checks all possible buffs conditions this source can receive.
            // - apply any increments post-buff, meaning the flat +X damage would only benefit from the few remaining
            // modifiers such as the crest bonuses mentioned earlier.
            if (tool != null && tool.Type == ToolItemType.Skill)
            {
                // -- silk skills --
                // 1. The "Type == ToolItemType.Skill" is enough to target only the silk skills.
                // 2. Silk skill damage scales normally from needle upgrades, or specifically, the needle damage
                // itself. To balance strength stat: only base game scaling works, after that intelligence stat
                // is necessary for extra scaling. As scaling requires adjusting base needle damage, it's already
                // done earlier in patched Playerdata.nailDamage.

                int spellDamage = hitInstance.DamageDealt;

                spellDamage = IntelligenceLogic.ApplySpellDamageBonuses(spellDamage, tool);

                if (IntelligenceLogic.CheckIntelligenceMilestone(3) && isCrit)
                {
                    spellDamage = DexterityLogic.ApplyCritDamage(spellDamage);
                }

                hitInstance.DamageDealt = spellDamage;
            }
            else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
            {
                // -- red and blue tools --

                int toolDamage = hitInstance.DamageDealt;

                if (tool.name == "Rosary Cannon" && ArtificeLogic.CheckArtificeMilestone(4))
                {
                    toolDamage += 2;
                }

                toolDamage = ArtificeLogic.ApplyToolDamageBonuses(toolDamage, tool);

                if (IntelligenceLogic.CheckIntelligenceMilestone(3))
                {
                    toolDamage = IntelligenceLogic.ApplyClawMirrorDamageBonuses(toolDamage, tool);
                }

                if (isCrit)
                {
                    if (IntelligenceLogic.CheckWeakBindRequirement() &&
                        (tool.name == "Dazzle Bind" || tool.name == "Dazzle Bind Upgraded"))
                    {
                        // Claw Mirror

                        toolDamage = DexterityLogic.ApplyCritDamage(toolDamage);
                    }
                    else if (ArtificeLogic.CheckArtificeMilestone(1) &&
                        (tool.name == "Pimpilo" || tool.name == "Brolly Spike"))
                    {
                        // Pimpillo and Sawtooth Circlet

                        toolDamage = DexterityLogic.ApplyCritDamage(toolDamage);
                    }
                }

                if (tool.name == "Brolly Spike" &&
                    ArtificeLogic.CheckArtificeMilestone(2) &&
                    ArtificeLogic.HitEnemyWithSawtoothCirclet)
                {
                    HeroController.instance.AddSilk(1, false);
                    ArtificeLogic.HitEnemyWithSawtoothCirclet = false;
                }

                hitInstance.DamageDealt = toolDamage;
            }
            else if (sourceName == "Harpoon Damager" || sourceName == "Harpoon Dash Damager")
            {
                int clawLineDamage = hitInstance.DamageDealt;

                clawLineDamage = DexterityLogic.ApplyClawlineDamageBonuses(clawLineDamage);

                if (DexterityLogic.CheckDexterityMilestone(2) && isCrit)
                {
                    clawLineDamage = DexterityLogic.ApplyCritDamage(clawLineDamage);
                }

                hitInstance.DamageDealt = clawLineDamage;
            }
            else if (hitInstance.IsNailDamage)
            {
                // -- basic needle hits + needle strike --
                // 1. The sourceName with Slash/UpSlash/DownSlash/AltSlash can be used to identify the direction,
                // slash being the normal left/right slash, and apply extra damage bonus e.g. on downward pogo 
                // hits.
                // 2. However needle strike for different crests use various different names. For these, the
                // IsNailDamage is better identifier unless specific needle strike damage components
                // need to be scaled individually.
                // 3. IsUsingNeedleDamageMult would also apply to Silk Skills because in vanilla they do scale
                // from needle upgrade count. However after base game upgrade scaling, this becomes solely intelligence 
                // stat's responsibility.

                int needleDamage = hitInstance.DamageDealt;

                needleDamage = StrengthLogic.ApplyNeedleDamageBonuses(needleDamage, sourceName);

                if (isCrit)
                {
                    needleDamage = DexterityLogic.ApplyCritDamage(needleDamage);
                }

                hitInstance.DamageDealt = needleDamage;
            }
            else if (sourceName == "Bonfire-Taunt")
            {
                // custom damage type: handles all primary damage logic in TauntDamage.

                int tauntDamage = IntelligenceLogic.TauntDamage(Bonfire.Instance.Status.IntelligenceStat);

                if (IntelligenceLogic.CheckIntelligenceMilestone(3) && isCrit)
                {
                    tauntDamage = DexterityLogic.ApplyCritDamage(tauntDamage);
                }

                hitInstance.DamageDealt = tauntDamage;
            }
            else if (sourceName == "Bonfire-Reflect")
            {
                // custom damage type: handles all primary damage logic in ReflectDamage

                int reflectDamage = ResilienceLogic.ReflectDamage(Bonfire.Instance.Status.ResilienceStat);

                if (ResilienceLogic.IsInBlueStateAndPlasmified(BlueState.THORNED) && isCrit)
                {
                    reflectDamage = DexterityLogic.ApplyCritDamage(reflectDamage);
                }

                hitInstance.DamageDealt = reflectDamage;
            }

            // while in 'Thorned' state, all non-reflect damage is reduced by 99% but is always at least 1.
            if (ResilienceLogic.BlueMaskMode == BlueState.THORNED && sourceName != "Bonfire-Reflect")
            {
                hitInstance.DamageDealt = (int)Math.Max(Math.Round(hitInstance.DamageDealt * 0.01), 1);
            }

            DexterityLogic.ApplyCritHeal(isCrit);

            LuckLogic.Crit = false;
        }
    }
}