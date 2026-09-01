using System;
using System.Reflection;

using GlobalSettings;
using HarmonyLib;

namespace BonfireSilksong
{
    /// <summary>
    /// General leveling system logic which needs to be injected into game code e.g. those not tied to any specific 
    /// stat implementation.
    /// </summary>
    public static class LevelingPatch
    {
        public static bool Taunted = false;
        public static int TotalSilkCache = 0;

        // PlayerData.instance.nailDamage
        public static readonly MethodInfo PlayerDataInstanceGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "instance");
        public static readonly MethodInfo PlayerDataNailDamageGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "nailDamage");

        public static readonly MethodInfo CustomNailDamageGetterDamageEnemies = AccessTools.Method(
            typeof(LevelingPatch),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(DamageEnemies) });
        public static readonly MethodInfo CustomNailDamageGetterHitInstance = AccessTools.Method(
            typeof(LevelingPatch),
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

            if (instance.RepresentingTool?.Type == ToolItemType.Skill)
            {
                return damage;
            }

            if (!ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
                damage = StrengthStat.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogDebug($"[PatchHelpers] getter (DamageEnemies) updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// HealthManager.DoLagHits (Transpiler): Instead of DamageEnemies, uses HitInstance.
        /// </summary>
        public static int CustomNeedleDamageGetter(HitInstance hitInstance)
        {
            int damage = PlayerData.instance.nailDamage;

            if (hitInstance.RepresentingTool?.Type == ToolItemType.Skill)
            {
                return damage;
            }

            damage = StrengthStat.UpdateNeedleBaseDamage(damage);

            Bonfire.Log.LogDebug($"[PatchHelpers] getter (HitInstance) updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// PlayerData.SilkSkillCost (Postfix): Same as original, except if player is in any Resilience blue state, not
        /// being at max blue masks should also prevent the Egg of Flealia silk cost reduction.
        /// </summary>
        public static int CustomSilkSkillCostGetter()
        {
            if (Gameplay.FleaCharmTool.IsEquippedHud)
            {
                var pd = PlayerData.instance;
                int maxBlueHp = ResilienceStat.BlueMasks(Bonfire.Instance.Status.ResilienceStat);

                // during plasmification, game uses normal masks to track hp
                // in any blue state, but not plasmified -> use blue masks instead
                // otherwise use the default normal mask tracking
                if ((ResilienceStat.IsInAnyBlueStateAndPlasmified() && pd.health == pd.CurrentMaxHealth) ||
                    (ResilienceStat.IsInAnyBlueState() && pd.healthBlue == maxBlueHp) ||
                    (!ResilienceStat.IsInAnyBlueState() && pd.health == pd.CurrentMaxHealth))
                {
                    return 3;
                }
            }

            return 4;
        }

        /// <summary>
        /// HeroController.TakeSilk (Prefix): Refund taunt cost under specific conditions. Importantly: cost check
        /// is always called before the SilkTaunted/RingTaunted methods in HeroController, meaning this one should
        /// check the validity whereas these two taunt methods should handle the toggling afterwards. 
        /// </summary>
        public static bool CheckTauntCostRefund(int cost)
        {
            // PlayerData's silk has not been updated to 'silk - cost' yet so cache it here
            TotalSilkCache = PlayerData.instance.silk;

            bool refunded = false;

            if (Taunted && cost == 1)
            {
                if (IntelligenceStat.AuraOrBurstEnabled)
                {
                    Bonfire.Log.LogDebug("[PatchHelpers] Refunded Taunt Cost for Aura/Burst");
                    refunded = true;
                }
            }

            Taunted = false;

            return refunded;
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

            Bonfire.Log.LogDebug("[PatchHelpers] Recorded a hit => Hit source: " + sourceName);
            Bonfire.Log.LogDebug("- Damage Dealt: " + hitInstance.DamageDealt);
            Bonfire.Log.LogDebug("- Tool instance: " + tool?.name ?? "null");

            int vanillaDamage = hitInstance.DamageDealt;

            LuckStat.CheckIfCrit(hitInstance);
            bool isCrit = LuckStat.Crit;

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
            if (tool?.Type == ToolItemType.Skill)
            {
                int spellDamage = hitInstance.DamageDealt;

                if (WisdomStat.IsSilkNailsEnabled && tool.name == "Silk Boss Needle")
                {
                    // Silk Nails: summon a weaker version of Pale Nails automatically when an enemy is nearby.
                    // This also disables use of Pale Nails so it's important to separate damage buff checks.
                    spellDamage = WisdomStat.SilkNailsDamage(Bonfire.Instance.Status.WisdomStat);
                }
                else
                {
                    // -- silk skills --
                    // 1. The "Type == ToolItemType.Skill" is enough to target only the silk skills.
                    // 2. Silk skill damage scales normally from needle upgrades, or specifically, the needle damage
                    // itself. To balance strength stat: only base game scaling works, after that intelligence stat
                    // is necessary for extra scaling. As scaling requires adjusting base needle damage, it's already
                    // done earlier in patched Playerdata.nailDamage.

                    spellDamage = IntelligenceStat.ApplySpellDamageBonuses(spellDamage, tool);

                    if (IntelligenceStat.CheckIntelligenceMilestone(3) && isCrit)
                    {
                        spellDamage = DexterityStat.ApplyCritDamage(spellDamage);
                    }
                }

                hitInstance.DamageDealt = spellDamage;
            }
            else if (tool != null &&
                (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue) &&
                tool.name != "Silk Nails")
            {
                int toolDamage = hitInstance.DamageDealt;

                if (WisdomStat.IsCogflyWarriorEnabled && tool.name == "Cogwork Flier")
                {
                    // Cogfly Warrior: cogfly which has Wisdom scaling and thus uses neither the original tool's damage
                    // nor tool bonuses.
                    toolDamage = WisdomStat.CogflyWarriorDamage(Bonfire.Instance.Status.WisdomStat);
                }
                else
                {
                    // -- red and blue tools --

                    if (tool.name == "Rosary Cannon" && ArtificeStat.CheckArtificeMilestone(3))
                    {
                        toolDamage += 2;
                    }

                    toolDamage = ArtificeStat.ApplyToolDamageBonuses(toolDamage, tool);

                    if (IntelligenceStat.CheckIntelligenceMilestone(3))
                    {
                        toolDamage = IntelligenceStat.ApplyClawMirrorDamageBonuses(toolDamage, tool);
                    }

                    if (isCrit)
                    {
                        if (IntelligenceStat.CheckWeakBindRequirement() &&
                            (tool.name == "Dazzle Bind" || tool.name == "Dazzle Bind Upgraded"))
                        {
                            // Claw Mirror

                            toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                        }
                        else if (ArtificeStat.CheckArtificeMilestone(1) &&
                            (tool.name == "Pimpilo" || tool.name == "Brolly Spike"))
                        {
                            // Pimpillo and Sawtooth Circlet

                            toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                        }
                        else if (ArtificeStat.CheckArtificeMilestone(2) && ArtificeStat.IsSilkshotTool(tool))
                        {
                            // Silkshot

                            toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                        }
                    }

                    if (tool.name == "Brolly Spike" &&
                        ArtificeStat.CheckArtificeMilestone(2) &&
                        ArtificeStat.HitEnemyWithSawtoothCirclet)
                    {
                        HeroController.instance.AddSilk(1, false);
                        ArtificeStat.HitEnemyWithSawtoothCirclet = false;
                    }
                }

                hitInstance.DamageDealt = toolDamage;
            }
            else if (sourceName == "Harpoon Damager" || sourceName == "Harpoon Dash Damager")
            {
                int clawLineDamage = hitInstance.DamageDealt;

                clawLineDamage = DexterityStat.ApplyClawlineDamageBonuses(clawLineDamage);

                if (DexterityStat.CheckDexterityMilestone(2) && isCrit)
                {
                    clawLineDamage = DexterityStat.ApplyCritDamage(clawLineDamage);
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
                // from needle upgrade count. However after base game upgrade scaling, Intelligence handles the rest.

                int needleDamage = hitInstance.DamageDealt;

                needleDamage = StrengthStat.ApplyNeedleDamageBonuses(needleDamage, sourceName);

                if (isCrit)
                {
                    needleDamage = DexterityStat.ApplyCritDamage(needleDamage);
                }

                hitInstance.DamageDealt = needleDamage;
            }
            else if (sourceName == "Bonfire-WeaverAura")
            {
                // custom damage type: handles all primary damage logic in WeaverAuraDamage

                int auraDamage = IntelligenceStat.WeaverAuraDamage(Bonfire.Instance.Status.IntelligenceStat);

                if (IntelligenceStat.CheckIntelligenceMilestone(3) && isCrit)
                {
                    auraDamage = DexterityStat.ApplyCritDamage(auraDamage);
                }

                hitInstance.DamageDealt = auraDamage;
            }
            else if (sourceName == "Bonfire-Reflect")
            {
                // custom damage type: handles all primary damage logic in ReflectDamage

                int reflectDamage = ResilienceStat.ReflectDamage(Bonfire.Instance.Status.ResilienceStat);

                if (ResilienceStat.IsInBlueStateAndPlasmified(BlueState.REFLECT) && isCrit)
                {
                    reflectDamage = DexterityStat.ApplyCritDamage(reflectDamage);
                }

                hitInstance.DamageDealt = reflectDamage;
            }

            // -- additional checks after hitInstance's own logic has been applied --

            if (ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
            {
                hitInstance.DamageDealt = ResilienceStat.ApplyGlassCannonDamageBonus(hitInstance.DamageDealt);
            }

            // while in reflect state, all non-reflect damage is reduced by 99% but is always at least 1.
            // Only exception is the Memory Crystal blue tool which fits the 'sacrifice hp to deal damage' theme.
            if (ResilienceStat.IsInBlueState(BlueState.REFLECT) && sourceName != "Bonfire-Reflect")
            {
                if (tool?.name != "Revenge Crystal")
                {
                    hitInstance.DamageDealt = (int)Math.Max(Math.Round(hitInstance.DamageDealt * 0.01), 1);
                    Bonfire.Log.LogDebug($"Reflect state enabled, damage reduced to {hitInstance.DamageDealt}");
                }
            }

            DamageOverlay.UpdateData(sourceName, tool?.name, vanillaDamage, hitInstance.DamageDealt);

            Bonfire.Log.LogDebug("=> Modified Damage Output: " + hitInstance.DamageDealt);

            DexterityStat.ApplyCritHeal(isCrit);
            LuckStat.Crit = false;
        }
    }
}