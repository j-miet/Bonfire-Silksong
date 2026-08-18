using System.Reflection;

using HarmonyLib;

namespace BonfireSilksong
{
    /// <summary>
    /// General patch logic helpers e.g. those not tied to any specific stat implementation.
    /// </summary>
    public static class PatchHelpers
    {
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

            Bonfire.Log.LogDebug("[HealthManager.Hit] Recorded a hit => Hit source: " + sourceName);
            Bonfire.Log.LogDebug("Damage Dealt: " + hitInstance.DamageDealt);
            Bonfire.Log.LogDebug("Tool instance: " + tool?.name ?? "null");

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

                int spellDamage = IntelligenceLogic.ApplySpellDamageBonuses(hitInstance.DamageDealt, tool);

                if (IntelligenceLogic.CheckIntelligenceMilestone_3 && isCrit)
                {
                    spellDamage = DexterityLogic.ApplyCritDamage(spellDamage);
                }

                hitInstance.DamageDealt = spellDamage;
            }
            else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
            {
                // -- red and blue tools --

                int toolDamage = ArtificeLogic.ApplyToolDamageBonuses(hitInstance.DamageDealt, tool);

                if (IntelligenceLogic.CheckIntelligenceMilestone_3)
                {
                    toolDamage = IntelligenceLogic.ApplyClawMirrorDamageBonuses(toolDamage, tool);
                }

                if (isCrit)
                {
                    if (ArtificeLogic.CheckArtificeMilestone_2 &&
                        (tool.name == "Pimpilo" || tool.name == "Brolly Spike"))
                    {
                        // TODO: need to test this
                        // Pimpillo and Sawtooth Circlet

                        toolDamage = DexterityLogic.ApplyCritDamage(toolDamage);
                    }
                    else if (IntelligenceLogic.CheckWeakBindRequirement() &&
                        (tool.name == "Dazzle Bind" || tool.name == "Dazzle Bind Upgraded"))
                    {
                        // Claw Mirror 

                        toolDamage = DexterityLogic.ApplyCritDamage(toolDamage);
                    }
                }

                hitInstance.DamageDealt = toolDamage;
            }
            else if (sourceName == "Harpoon Damager" || sourceName == "Harpoon Dash Damager")
            {
                int clawLineDamage = DexterityLogic.ApplyClawlineDamageBonuses(hitInstance.DamageDealt);

                if (DexterityLogic.CheckDexterityMilestone_2 && isCrit)
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
                // from needle upgrade count. However, to better balance silk skills, they require intelligence
                // stat for scaling to avoid the issue of making strength a better scaling path for spells while
                // still leaving all the spell-oriented milestones under intelligence stat itself, requiring to
                // level both.

                int needleDamage = StrengthLogic.ApplyNeedleDamageBonuses(hitInstance.DamageDealt, sourceName);

                if (isCrit)
                {
                    needleDamage = DexterityLogic.ApplyCritDamage(needleDamage);
                }

                hitInstance.DamageDealt = needleDamage;
            }
            else if (sourceName == "Bonfire-Taunt")
            {
                int tauntDamage = IntelligenceLogic.TauntDamage(Bonfire.Instance.Status.IntelligenceStat);

                if (IntelligenceLogic.CheckIntelligenceMilestone_3 && isCrit)
                {
                    tauntDamage = DexterityLogic.ApplyCritDamage(tauntDamage);
                }

                hitInstance.DamageDealt = tauntDamage;
            }

            DexterityLogic.ApplyCritHeal(isCrit);

            LuckLogic.Crit = false;
        }
    }
}