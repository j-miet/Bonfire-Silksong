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

            if (!ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
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
        /// HealthManager.OnEnable (Postfix): Update enemy rosary and shell shard drop count.
        /// </summary>
        public static void UpdateCurrencyDrops(HealthManager instance)
        {
            var s = Bonfire.Instance.Status;

            int small = GetRosaries("small", instance);
            int medium = GetRosaries("medium", instance);
            int large = GetRosaries("large", instance);
            int shards = GetShellShards(instance);

            Bonfire.Log.LogDebug("[LuckStat] Check currency values");
            Bonfire.Log.LogDebug("<Vanilla> rosary and shell shard values:");
            Bonfire.Log.LogDebug($"small={small}");
            Bonfire.Log.LogDebug($"medium={medium}");
            Bonfire.Log.LogDebug($"large={large}");
            Bonfire.Log.LogDebug($"shards={shards}");

            int baseRosaryBonus = GlobalStat.IncreaseCurrencyBaseDrops(s.LuckStat);

            if (baseRosaryBonus > 0)
            {
                int modulo = baseRosaryBonus % GlobalStat.ExtraRosaryThreshold;
                if (modulo == 0)
                {
                    medium += baseRosaryBonus / GlobalStat.ExtraRosaryThreshold;
                }
                else
                {
                    medium += baseRosaryBonus / GlobalStat.ExtraRosaryThreshold;
                    small += modulo;
                }

                shards += baseRosaryBonus;
            }

            int luck = s.LuckStat;
            if (luck > 0)
            {
                small = LuckStat.IncreaseRosaryCount(small, luck);
                medium = LuckStat.IncreaseRosaryCount(medium, luck);
                large = LuckStat.IncreaseRosaryCount(large, luck);
                shards = LuckStat.IncreaseRosaryCount(shards, luck);
            }

            if (GlobalStat.CheckGlobalMilestone(GlobalMilestone.CURRENCYMULTIPLIER))
            {
                small = GlobalStat.ApplyCurrencyMultiplier(small);
                medium = GlobalStat.ApplyCurrencyMultiplier(medium);
                large = GlobalStat.ApplyCurrencyMultiplier(large);
                shards = GlobalStat.ApplyCurrencyMultiplier(shards);
            }

            Bonfire.Log.LogDebug("<Modded> rosary and shell shard values:");
            Bonfire.Log.LogDebug($"Base value bonus: {baseRosaryBonus}");
            Bonfire.Log.LogDebug($"small={small}");
            Bonfire.Log.LogDebug($"medium={medium}");
            Bonfire.Log.LogDebug($"large={large}");
            Bonfire.Log.LogDebug($"shards={shards}");

            // SetGeo: these determine how many rosary beads of each type are dropped, not the value of that type. 
            // Their actual currency values are: small/shell shard = 1, medium = 5, large = 15.
            instance.SetGeoSmall(small);
            instance.SetGeoMedium(medium);
            instance.SetGeoLarge(large);
            instance.SetShellShards(shards);
        }

        /// <summary>
        /// HealthManager.Hit (Prefix): Handle stat damage calculations before game calls HealthManager.TakeDamage
        /// for actual health subtraction logic.
        /// Pass by references: HitInstance is a struct value type and its DamageDealt value needs to be updated.
        /// </summary>
        public static void ApplyHitBonuses(HealthManager hm, ref HitInstance hit)
        {
            if (!hit.IsHeroDamage || hit.Source == null) return;

            string sourceName = hit.Source.name;
            var tool = hit.RepresentingTool;
            int vanillaDamage = hit.DamageDealt;

            // override name for damage sources which still use the default hitInstance e.g. Silk Nails uses Pale Nails
            // silk skill underneath; this makes it easier to handle damage calculations + differentiate sources when
            // debugging/using damage display feature.
            // Custom damage instances already define the source earlier e.g. "Bonfire-Reflect" so they're already ok.
            // To further identify mod damage sources, their names should begin with 'Bonfire-' prefix.
            if (tool != null)
            {
                if (WisdomStat.IsSilkNailsEnabled && tool.name == "Silk Boss Needle")
                    sourceName = "Bonfire-SilkNails";

                if (WisdomStat.IsCogflyWarriorEnabled && tool.name == "Cogwork Flier")
                    sourceName = "Bonfire-CogflyWarrior";
            }

            Bonfire.Log.LogDebug("[PatchHelpers] Recorded a hit => Hit source: " + sourceName);
            Bonfire.Log.LogDebug("- Damage Dealt: " + vanillaDamage);
            Bonfire.Log.LogDebug("- Tool instance: " + tool?.name ?? "null");

            LuckStat.CheckIfCrit(hit);
            bool isCrit = LuckStat.Crit;

            // == Important ==
            // 1. Multiplicative damage scaling: just use hitInstance.DamageDealt and scale it.
            // 2. Additive scaling: at this point the game has already applied most if not all buffs.
            // Thus, using simply 'hit.DamageDealt = newValue' would not modify the base value, making any 
            // damage buffs e.g. Flintslate for needle hits, useless. Solution is to pick one of following routes: 
            // - find the base damage of damage source and patch it e.g. PlayerData.nailDamage getter for needle 
            // base damage.
            // - write a custom formula which also checks all possible buffs conditions this source can receive.
            // - apply any increments post-buff, meaning the flat +X damage would only benefit from the few remaining
            // modifiers such as the crest bonuses mentioned earlier.
            if (sourceName == "Bonfire-WeaverAura")
            {
                // Custom Weaver Aura damage source

                int auraDamage = IntelligenceStat.WeaverAuraDamage(Bonfire.Instance.Status.IntelligenceStat);

                if (IntelligenceStat.CheckIntelligenceMilestone(3) && isCrit)
                {
                    auraDamage = DexterityStat.ApplyCritDamage(auraDamage);
                }

                hit.DamageDealt = auraDamage;
            }
            else if (sourceName == "Bonfire-Reflect")
            {
                // Custom Reflect damage source

                int reflectDamage = ResilienceStat.ReflectDamage(Bonfire.Instance.Status.ResilienceStat);

                if (ResilienceStat.IsInBlueStateAndPlasmified(BlueState.REFLECT) && isCrit)
                {
                    reflectDamage = DexterityStat.ApplyCritDamage(reflectDamage);
                }

                hit.DamageDealt = reflectDamage;
            }
            else if (sourceName == "Bonfire-SilkNails")
            {
                // Silk Nails: summon a weaker version of Pale Nails automatically when an enemy is nearby.
                // This also disables use of Pale Nails so it's important to separate damage buff checks.
                hit.DamageDealt = WisdomStat.SilkNailsDamage(Bonfire.Instance.Status.WisdomStat);
            }
            else if (sourceName == "Bonfire-CogflyWarrior")
            {
                // Cogfly Warrior: cogfly which has Wisdom scaling and thus uses neither the original tool's damage
                // nor tool bonuses.
                hit.DamageDealt = WisdomStat.CogflyWarriorDamage(Bonfire.Instance.Status.WisdomStat);
            }
            else if (tool?.Type == ToolItemType.Skill)
            {
                // -- silk skills --
                // 1. The "Type == ToolItemType.Skill" is enough to target only the silk skills.
                // 2. Silk skill damage scales normally from needle upgrades, or specifically, the needle damage
                // itself. To balance strength stat: only base game scaling works, after that intelligence stat
                // is necessary for extra scaling. As scaling requires adjusting base needle damage, it's already
                // done earlier in patched Playerdata.nailDamage.

                int spellDamage = hit.DamageDealt;

                spellDamage = IntelligenceStat.ApplySpellDamageBonuses(spellDamage, tool);

                if (IntelligenceStat.CheckIntelligenceMilestone(3) && isCrit)
                {
                    spellDamage = DexterityStat.ApplyCritDamage(spellDamage);
                }

                IntelligenceStat.CastVoltvesselBolaOnHit(hm, in hit);

                hit.DamageDealt = spellDamage;
            }
            else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
            {
                // -- red and blue tools --

                string tName = tool.name;

                int toolDamage = hit.DamageDealt;

                // pre-bonus (applied before Artifice multiplier)
                if (tName == "Rosary Cannon" && ArtificeStat.CheckArtificeMilestone(3))
                {
                    toolDamage += 2;
                }

                // apply primary percentage multiplier
                toolDamage = ArtificeStat.ApplyToolDamageBonuses(toolDamage, tool);

                // post-bonus (applied after Artifice multiplier)
                if (IntelligenceStat.CheckIntelligenceMilestone(3))
                {
                    toolDamage = IntelligenceStat.ApplyClawMirrorDamageBonuses(toolDamage, tool);
                }

                if (IntelligenceStat.CheckWeakBindRequirement() && tName == "Lightning Rod")
                {
                    // custom Voltvessels tool proc in Weak Bind: uses its own flat scaling

                    toolDamage = IntelligenceStat.BolaDamage(Bonfire.Instance.Status.IntelligenceStat);
                }

                if (isCrit)
                {
                    if (IntelligenceStat.CheckWeakBindRequirement() &&
                        (tName == "Dazzle Bind" || tool.name == "Dazzle Bind Upgraded"))
                    {
                        // Claw Mirror

                        toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                    }
                    else if (ArtificeStat.CheckArtificeMilestone(1) &&
                        (tName == "Pimpilo" || tName == "Brolly Spike"))
                    {
                        // Pimpillo and Sawtooth Circlet

                        toolDamage = DexterityStat.ApplyCritDamage(toolDamage);

                        ArtificeStat.CastRuneRageOnHit(hm, in hit);
                    }
                    else if (ArtificeStat.CheckArtificeMilestone(2) && ArtificeStat.IsSilkshotTool(tool))
                    {
                        // Silkshot

                        toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                    }
                    else if (LuckStat.IsPinwielderEnabled &&
                        (tName == "Straight Pin" || tName == "Tri Pin" || tName == "Harpoon"))
                    {
                        // Straight Pin, Threefold Pin and Longpin
                        if (tName == "Tri Pin")
                        {
                            if (LuckStat.CheckLuckMilestone(4))
                            {
                                toolDamage += 30;
                            }
                            else if (LuckStat.CheckLuckMilestone(2))
                            {
                                toolDamage += 8;
                            }
                        }
                        else if (tool.name == "Straight Pin")
                        {
                            if (LuckStat.CheckLuckMilestone(2))
                                HeroController.instance.AddSilk(1, false);

                            if (LuckStat.CheckLuckMilestone(3))
                                toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                        }

                        if (tName == "Tri Pin" || tName == "Harpoon")
                            toolDamage = DexterityStat.ApplyCritDamage(toolDamage);
                    }
                }

                LuckStat.CastThreadStormOnHit(hm, in hit, isCrit);

                if (tool.name == "Brolly Spike" &&
                    ArtificeStat.CheckArtificeMilestone(2) &&
                    ArtificeStat.HitEnemyWithSawtoothCirclet)
                {
                    HeroController.instance.AddSilk(1, false);
                    ArtificeStat.HitEnemyWithSawtoothCirclet = false;
                }

                hit.DamageDealt = toolDamage;
            }
            else if (sourceName == "Harpoon Damager" || sourceName == "Harpoon Dash Damager")
            {
                // Clawline

                int clawLineDamage = hit.DamageDealt;

                clawLineDamage = DexterityStat.ApplyClawlineDamageBonuses(clawLineDamage);

                if (DexterityStat.CheckDexterityMilestone(2) && isCrit)
                {
                    clawLineDamage = DexterityStat.ApplyCritDamage(clawLineDamage);
                }

                hit.DamageDealt = clawLineDamage;
            }
            else if (hit.IsNailDamage)
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

                int needleDamage = hit.DamageDealt;

                needleDamage = StrengthStat.ApplyNeedleDamageBonuses(needleDamage, sourceName);

                if (isCrit)
                {
                    needleDamage = DexterityStat.ApplyCritDamage(needleDamage);
                }

                hit.DamageDealt = needleDamage;
            }

            // -- additional checks after hitInstance's own logic has been applied --

            if (ResilienceStat.IsInBlueState(BlueState.GLASSCANNON))
            {
                hit.DamageDealt = ResilienceStat.ApplyGlassCannonDamageBonus(hit.DamageDealt);
            }

            if (LuckStat.IsPinwielderEnabled && tool?.name == "Straight Pin")
            {
                hit.DamageDealt = (int)Math.Max(Math.Round(hit.DamageDealt * 0.25), 1);
                Bonfire.Log.LogDebug(
                    $"Pinwielder state enabled, Straight Pin damage reduced to {hit.DamageDealt}");
            }

            // while in reflect state, all non-reflect damage is reduced by 99% but is always at least 1.
            // Only exception is the Memory Crystal blue tool which fits the 'sacrifice hp to deal damage' theme.
            if (ResilienceStat.IsInBlueState(BlueState.REFLECT) && sourceName != "Bonfire-Reflect")
            {
                if (tool?.name != "Revenge Crystal")
                {
                    // round to ceiling value, Straight Pin has 5/8/11/14/17 damage scaling which becomes 2/2/3/4/5.
                    hit.DamageDealt = (int)Math.Max(Math.Ceiling(hit.DamageDealt * 0.01), 1);
                    Bonfire.Log.LogDebug($"Reflect state enabled, damage reduced to {hit.DamageDealt}");
                }
            }

            WisdomStat.GetExtraSilkOnHit();

            DexterityStat.ApplyCritHeal(isCrit);
            LuckStat.Crit = false;

            DamageOverlay.UpdateData(sourceName, tool?.name, vanillaDamage, hit.DamageDealt);
            Bonfire.Log.LogDebug("=> Modified Damage Output: " + hit.DamageDealt);
        }


        // helpers for getting current rosary and shell shard values
        private static int GetRosaries(string size, HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField(size + "GeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object rosaries = fi.GetValue(enemy);
            return rosaries == null ? 0 : (int)rosaries;
        }

        private static int GetShellShards(HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField("shellShardDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object shards = fi.GetValue(enemy);
            return shards == null ? 0 : (int)shards;
        }
    }
}