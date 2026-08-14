using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

namespace BonfireSilksong
{
    /// <summary>
    /// Logic shared between multiple stat classes.
    /// For know it also includes the few general patcher helpers e.g. enemy hp bar data stuff.
    /// </summary>
    public static class StatsShared
    {
        public static readonly MethodInfo PlayerDataInstanceGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "instance");
        public static readonly MethodInfo PlayerDataNailDamageGetter = AccessTools.PropertyGetter(
            typeof(PlayerData),
            "nailDamage");

        public static readonly MethodInfo CustomNailDamageGetterDamageEnemies = AccessTools.Method(
            typeof(StatsShared),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(DamageEnemies) });
        public static readonly MethodInfo CustomNailDamageGetterHitInstance = AccessTools.Method(
            typeof(StatsShared),
            nameof(CustomNeedleDamageGetter),
            new[] { typeof(HitInstance) });

        /// <summary>
        /// HealthManager.OnEnable (Postfix): Add enemy hp data.
        /// </summary>
        public static void AddEnemyHpData(HealthManager instance)
        {
            var gui = BonfireGUI.Settings;

            // get enemy hp data for GUI to draw their hp bars
            if (!gui.EnemyMaxHp.ContainsKey(instance))
            {
                gui.EnemyMaxHp.Add(instance, instance.hp);
                gui.EnemyIsBoss.Add(instance, BossSceneController.Instance != null);
            }
        }

        /// <summary>
        /// HeroController.Update (Postfix): Flush used enemy hp data.
        /// </summary>
        public static void FlushEnemyHPData()
        {
            var gui = BonfireGUI.Settings;
            var toRemove = new List<HealthManager>();

            // flush used enemy hp bar data
            foreach (var e in gui.EnemyMaxHp.Keys)
            {
                if (e == null || e.hp <= 0)
                    toRemove.Add(e);
            }
            foreach (var e in toRemove)
            {
                gui.EnemyMaxHp.Remove(e);
                gui.EnemyIsBoss.Remove(e);
            }
        }

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

            Bonfire.Log.LogDebug($"[StatsShared] updated needle damage={damage}");

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

            Bonfire.Log.LogDebug($"[StatsShared] updated needle damage={damage}");

            return damage;
        }

        /// <summary>
        /// HealthManager.Hit (Prefix): Handle stat damage calculations before game calls HealthManager.TakeDamage
        /// for actual health subtraction logic.
        /// </summary>
        public static void ApplyHitDamageBonuses(HitInstance hitInstance)
        {
            if (hitInstance.Source == null) return;

            string sourceName = hitInstance.Source.name;
            var tool = hitInstance.RepresentingTool;

            Bonfire.Log.LogDebug("[HealthManager.Hit] Recorded a hit => Hit source: " + sourceName);
            Bonfire.Log.LogDebug("Damage Dealt: " + hitInstance.DamageDealt);
            Bonfire.Log.LogDebug("Tool instance: " + tool?.name ?? "null");

            bool isCrit = LuckLogic.Crit;

            // == Important ==
            // 1. Multiplicative damage scaling: just use hitInstance.DamageDealt and scale it.
            // 2. Additive scaling: at this point the game has already applied most buffs. Exceptions would include 
            // Wanderer crest crit and Hunter's Crest damage amplification as game applies these during its final damage
            // calculations in HealthManager.TakeDamage.
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

                int updatedDamage = IntelligenceLogic.ApplySpellDamageBonuses(hitInstance.DamageDealt, tool);

                if (IntelligenceLogic.CheckIntelligenceMilestone_2 && isCrit)
                {
                    updatedDamage = DexterityLogic.ApplyCritDamage(updatedDamage);
                }

                hitInstance.DamageDealt = updatedDamage;
            }
            else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
            {
                // -- red and blue tools --

                hitInstance.DamageDealt = ArtificeLogic.ApplyToolDamageBonuses(hitInstance.DamageDealt, tool);

                // TODO add sawtooth circlet and Pimpillo crit
                //if (tool.name == ... && isCrit)
            }
            else if (hitInstance.IsHarpoon)
            {
                int updatedDamage = DexterityLogic.ApplyHarpoonDamageBonuses(hitInstance.DamageDealt);

                if (DexterityLogic.CheckDexterityMilestone_2 && isCrit)
                {
                    updatedDamage = DexterityLogic.ApplyCritDamage(updatedDamage);
                }

                hitInstance.DamageDealt = updatedDamage;
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

                int updatedDamage = StrengthLogic.ApplyNeedleDamageBonuses(hitInstance.DamageDealt, sourceName);

                if (isCrit)
                {
                    updatedDamage = DexterityLogic.ApplyCritDamage(updatedDamage);
                }

                hitInstance.DamageDealt = updatedDamage;
            }

            LuckLogic.Crit = false;
        }
    }
}