using System.Collections.Generic;
using System.Reflection;

using GlobalEnums;
using HarmonyLib;
using TeamCherry.SharedUtils;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Harmony patching for Leveling system. Should act internally and only target patched methods, meaning everything 
    /// becomes "private static" (class itself can't be static because it needs to derive from MonoBehavior).
    /// Blue health logic is still kept in LevelingSystem because it has to provide public interfaces, which the
    /// HeroController_UpdateBlueHealth patch in this class also uses.
    /// </summary>
    public class LevelingPatches : MonoBehaviour
    {
        private static int _critRoll = 0;
        private static bool _crit = false;
        private static float _silkRegenTime = 0f;
        private static float _toolRegenTime = 0f;
        private static int _hitsSinceShielded = 0;
        private static bool _blockNextHit = false;

        // rosary and shell shard drop quantities from enemies
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

        // lucky dice visual effect trigger when the player blocks a hit (HealthManager_TakeDamage)
        private static void LuckyShieldSetActive()
        {
            var hc = HeroController.instance;

            var effect = (GameObject)typeof(HeroController)
                .GetField("spawnedLuckyDiceShieldEffect", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(hc);

            if (effect != null)
            {
                effect.SetActive(false);
                effect.SetActive(true); // toggle off/on to retrigger the effect, matching vanilla LuckyDice behavior
            }
        }

        // crit visuals
        // the visual effect in HealthManager_Hit (FlashGrimmflame) only applies to the player. To also display effect 
        // on the enemy, DamageEnemies_DoDamage needs to explicitly check if damage was inflicted by player.
        private static bool IsHeroDamage(DamageEnemies instance)
        {
            bool a = (bool)(typeof(DamageEnemies)
                .GetField("isHeroDamage", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) ?? false);
            bool b = (bool)(typeof(DamageEnemies)
                .GetField("sourceIsHero", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) ?? false);
            return a || b;
        }


        // force config data saving when charms are updated
        // as leveling up and respeccing needs these, it's important to call CharmUpdate inside them
        [HarmonyPatch(typeof(HeroController), "CharmUpdate")]
        class HeroController_CharmUpdate
        {
            static void Postfix()
            {
                LevelingSystem.Instance.RefreshRelicsDeposited();
                Bonfire.Instance.SaveSettings();
            }
        }

        // rosary/shell drop scaling from luck stat. Also fills enemy hp bar data for bench GUI.
        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        class HealthManager_OnEnable
        {
            static void Postfix(HealthManager __instance)
            {
                if (__instance != null)
                {
                    var ls = LevelingSystem.Instance;
                    var i = Bonfire.Instance;
                    var s = i.Status;
                    var gui = BonfireGUI.Settings;

                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }

                    Bonfire.Log.LogDebug("[LevelingSystem] <Vanilla> rosary and shell shard values:");
                    Bonfire.Log.LogDebug($"small={GetRosaries("small", __instance)}");
                    Bonfire.Log.LogDebug($"medium={GetRosaries("medium", __instance)}");
                    Bonfire.Log.LogDebug($"large={GetRosaries("large", __instance)}");
                    Bonfire.Log.LogDebug($"shards={GetShellShards(__instance)}");

                    Bonfire.Log.LogDebug("[LevelingSystem] <Modded> rosary and shell shard values:");
                    Bonfire.Log.LogDebug(
                        $"small={LevelingSystem.IncreaseRosaries(GetRosaries("small", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug(
                        $"medium={LevelingSystem.IncreaseRosaries(GetRosaries("medium", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug(
                        $"large={LevelingSystem.IncreaseRosaries(GetRosaries("large", __instance), s.LuckStat)}");
                    Bonfire.Log.LogDebug(
                        $"shards={LevelingSystem.IncreaseRosaries(GetShellShards(__instance), s.LuckStat)}");

                    __instance.SetGeoSmall(
                        LevelingSystem.IncreaseRosaries(GetRosaries("small", __instance), s.LuckStat));
                    __instance.SetGeoMedium(
                        LevelingSystem.IncreaseRosaries(GetRosaries("medium", __instance), s.LuckStat));
                    __instance.SetGeoLarge(
                        LevelingSystem.IncreaseRosaries(GetRosaries("large", __instance), s.LuckStat));

                    __instance.SetShellShards(
                        LevelingSystem.IncreaseRosaries(GetShellShards(__instance), s.LuckStat));
                }
            }
        }

        // update lifeblood masks when sitting down on a bench
        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix()
            {
                if (!LevelingSystem.BlueHpInit)
                {
                    LevelingSystem.BlueHpInit = true;
                    return;
                }

                LevelingSystem.UpdateBlueHP();
            }
        }

        // binding cost reduction (visually match the FSM variable value by updating silk spool)
        [HarmonyPatch(typeof(SilkSpool), "BindCost", MethodType.Getter)]
        class SilkSpool_BindCost_Getter
        {
            static void Postfix(ref float __result)
            {
                if (PlayerData.instance.IsAnyCursed) return;

                // in HK, soul container has range 0-99, focus costs 33 soul
                // in HK:SS, silk container has range 0-9, with all upgrades reaching 0-18, binding costs 3 silk
                // In Hk, range is then divided by 33, in silksong by 9
                // => Silksong scaling is not as smooth, but ramps a bit harder on high intelligence
                int cost = LevelingSystem.BindCost(Bonfire.Instance.Status.IntelligenceStat);

                __result = cost;
            }
        }

        // needle attack speed
        [HarmonyPatch(typeof(HeroController), "DidAttack")]
        class HeroController_DidAttack
        {
            static void Postfix(HeroController __instance)
            {
                var config = __instance.Config;
                if (config == null) return;

                float attackSpeed = LevelingSystem.AttackSpeed(Bonfire.Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attack_cooldown", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackCooldownTime / attackSpeed);
            }
        }

        [HarmonyPatch(typeof(HeroController), "Attack")]
        class HeroController_Attack
        {
            static void Postfix(HeroController __instance)
            {
                var config = __instance.Config;
                if (config == null) return;

                float attackSpeed = LevelingSystem.AttackSpeed(Bonfire.Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackDuration / attackSpeed);
            }
        }

        // damage blocking based on Resilience stat
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(HeroController __instance, HazardType hazardType)
            {
                if (__instance.cState.evading || __instance.cState.invulnerable) return;

                Bonfire.Log.LogDebug("[LevelingSystem] Took damage from hazard type: " + hazardType);

                if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

                var s = Bonfire.Instance.Status;
                if (s.ResilienceStat <= 0) return;

                int hitsSinceShielded = _hitsSinceShielded;
                if (hitsSinceShielded > 7)
                    hitsSinceShielded = 7;

                float num = Random.Range(1, 100);
                float blockChance = LevelingSystem.BlockChance(s.ResilienceStat);
                float multiplier = 0f;

                switch (hitsSinceShielded)
                {
                    case 1: multiplier = 10f; break;
                    case 2: multiplier = 20f; break;
                    case 3: multiplier = 30f; break;
                    case 4: multiplier = 50f; break;
                    case 5: multiplier = 70f; break;
                    case 6: multiplier = 80f; break;
                    case 7: multiplier = 90f; break;
                }

                Bonfire.Log.LogDebug($"Range value {num} <= {multiplier} * {blockChance}");
                if (multiplier > 0 && num <= multiplier * blockChance)
                {
                    _hitsSinceShielded = 0;
                    LuckyShieldSetActive();
                    _blockNextHit = true;
                    Bonfire.Log.LogDebug("Blocked a hit!");
                }
                else
                {
                    _hitsSinceShielded++;
                }
            }

            static void Postfix()
            {
                // safety feature to ensure that block flag is set to 'false' after TakeDamage and its internal 
                // TakeHealth call. Otherwise flag could get stuck on 'true', making player invulnerable
                _blockNextHit = false;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
        class PlayerData_TakeHealth
        {
            static void Prefix(ref int amount)
            {
                if (Bonfire.Instance.Status.ResilienceStat <= 1) return;
                if (_blockNextHit)
                {
                    amount = 0;
                    _blockNextHit = false;
                }
            }
        }

        // extra silk gain when hitting enemies with needle
        [HarmonyPatch(typeof(HeroController), "SilkGain", new[] { typeof(HitInstance) })]
        class HeroController_SilkGain
        {
            static void Postfix(HitInstance hitInstance)
            {
                if (hitInstance.SilkGeneration == HitSilkGeneration.None) return;
                if (hitInstance.SilkGeneration == HitSilkGeneration.FirstHit && !hitInstance.IsFirstHit) return;
                if (PlayerData.instance.atBench) return;

                int extraSilk = LevelingSystem.ExtraSilk(Bonfire.Instance.Status.WisdomStat, 1) - 1;
                if (extraSilk > 0)
                    HeroController.instance.AddSilk(extraSilk, heroEffect: false);
            }
        }

        // clean enemy hp bars, handle crit roll and passive silk/tool regen from wisdom/artifice stat respectively
        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Postfix()
            {
                Bonfire.Log.LogDebug("[LevelingSystem] Update hp data, check silk + tool regen");
                var gui = BonfireGUI.Settings;
                var toRemove = new List<HealthManager>();

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

                if (GameManager.instance.inputHandler.inputActions.Attack.WasPressed == true)
                    _critRoll = UnityEngine.Random.Range(1, 100);

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                try
                {
                    float dt = Time.deltaTime;

                    // passive silk regen
                    if (_silkRegenTime > 0)
                    {
                        _silkRegenTime -= dt;
                    }
                    else
                    {
                        Bonfire.Log.LogDebug("Checking silk regen conditions");
                        int silkRegenInterval = LevelingSystem.SilkRegenInterval(Bonfire.Instance.Status.WisdomStat);
                        if (silkRegenInterval <= 0)
                        {
                            _silkRegenTime = 1f;
                            return;
                        }

                        _silkRegenTime = silkRegenInterval;
                        HeroController.instance.AddSilk(1, heroEffect: false);

                        Bonfire.Log.LogDebug("Regenerated 1 silk");
                    }

                    // passive tool regen: this will replenish charges for ALL red tools currently equipped
                    if (_toolRegenTime > 0)
                    {
                        _toolRegenTime -= dt;
                    }
                    else
                    {
                        Bonfire.Log.LogDebug("Checking tool regen conditions");
                        int toolRegenInterval = LevelingSystem.ToolRegenInterval(Bonfire.Instance.Status.ArtificeStat);
                        if (toolRegenInterval <= 0)
                        {
                            _toolRegenTime = 1f;
                            return;
                        }

                        _toolRegenTime = toolRegenInterval;
                        foreach (var tool in Resources.FindObjectsOfTypeAll<ToolItem>())
                        {
                            if (tool.IsEquipped && tool.IsAutoReplenished() && tool.CanReload())
                                tool.CollectFree(1);
                            // tool.ReloadSingle() would do the same, but costs shards
                        }

                        Bonfire.Log.LogDebug("Replenished 1 charge for all red tools");
                    }
                }
                catch { }
            }
        }

        // Wisdom stat: remove Wispfire Lantern cost and reduce spawn & attack delay after stat level is high enough.
        // First check if player has 0 silk: if yes, add one silk to spawn a wisp. Then refund this in TakeSilk
        // method.
        // This will naturally work as a weaker Weavelight tool: every time the game attempts to spawn a wisp and 
        // player has no silk, it will one for free assuming they have enough points in Wisdom.
        [HarmonyPatch(typeof(HeroWispLantern), "SpawnWispsRoutine")]
        class HeroWispLantern_SpawnWispsRoutine
        {
            static void Prefix()
            {
                var hc = HeroController.instance;

                if (LevelingSystem.CheckWisdomMilestone_1 && hc.playerData.silk <= 0)
                    hc.AddSilk(1, heroEffect: false);
            }
        }

        [HarmonyPatch(typeof(HeroController), "TakeSilk", new[] { typeof(int), typeof(SilkSpool.SilkTakeSource) })]
        class HeroController_TakeSilk
        {
            static void Postfix(HeroController __instance, int amount, SilkSpool.SilkTakeSource source)
            {
                if (source == SilkSpool.SilkTakeSource.Wisp && LevelingSystem.CheckWisdomMilestone_1)
                {
                    __instance.AddSilk(amount, heroEffect: false);
                    Bonfire.Log.LogDebug("[LevelingSystem] Refunded silk after Wispfire Lantern use");
                }
            }
        }

        [HarmonyPatch(typeof(HeroWispLantern), "OnSceneLoaded")]
        class HeroWispLantern_OnSceneLoaded
        {
            static void Prefix(HeroWispLantern __instance)
            {
                FieldInfo WispSpawnStartDelay = typeof(HeroWispLantern)
                    .GetField("wispSpawnStartDelay", BindingFlags.Instance | BindingFlags.NonPublic);

                FieldInfo WispSpawnCooldown = typeof(HeroWispLantern)
                    .GetField("wispSpawnCooldown", BindingFlags.Instance | BindingFlags.NonPublic);

                MinMaxFloat mmfDelay = (MinMaxFloat)WispSpawnStartDelay?.GetValue(__instance);
                MinMaxFloat mmfCd = (MinMaxFloat)WispSpawnCooldown?.GetValue(__instance);

                Bonfire.Log.LogDebug("[LevelingSystem] Wispfire Lantern:");
                Bonfire.Log.LogDebug("- Delay start: " + mmfDelay.Start);
                Bonfire.Log.LogDebug("- Delay End " + mmfDelay.End);
                Bonfire.Log.LogDebug("- Cd start: " + mmfCd.Start);
                Bonfire.Log.LogDebug("- Cd End: " + mmfCd.End);

                // start & end use same value: delay default is 0.3f, cooldown default is 4f (4 seconds)
                float delayNew = 0.3f;
                float cdNew = 4f;

                if (LevelingSystem.CheckWisdomMilestone_2)
                {
                    delayNew = 0.1f;
                    cdNew = 1f;
                }
                else if (LevelingSystem.CheckWisdomMilestone_1)
                {
                    delayNew = 0.15f;
                    cdNew = 2f;
                }

                mmfDelay.Start = mmfDelay.End = delayNew;
                mmfCd.Start = mmfCd.End = cdNew;

                WispSpawnStartDelay.SetValue(__instance, mmfDelay);
                WispSpawnCooldown.SetValue(__instance, mmfCd);
            }
        }

        // handle crit roll (prefix) and crit effect when enemy gets hit (postfix)
        // DamageEnemies_DoDamage calls patched HealthManager_Hit internally so logic goes:
        // - Prefix rolls crit chance
        // - DoDamage calls Hit at some point
        // - Hit checks and handles player's perspective by adding damage and displaying visual effect on player
        // - Postfix adds the effect on enemy that got hit
        [HarmonyPatch(typeof(DamageEnemies), "DoDamage", new[] { typeof(GameObject), typeof(bool) })]
        class DamageEnemies_DoDamage
        {
            static void Prefix(DamageEnemies __instance, GameObject target)
            {
                if (target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                int critChance = LevelingSystem.CritChance(Bonfire.Instance.Status.LuckStat);
                if (critChance < 1) // must have at least 1% crit chance
                {
                    _crit = false;
                    return;
                }

                int critRoll = _critRoll;

                Bonfire.Log.LogDebug($"[LevelingSystem] Crit chance: {critChance}. Rolled {critRoll}.");

                _crit = critRoll <= critChance;
            }

            static void Postfix(DamageEnemies __instance, GameObject target, bool __result)
            {
                if (!__result || target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                if (_crit)
                {
                    // effect comes from prefab value, these can have both visual and sound included. 
                    // Prefer one with no audio + only mild visuals; you will be hearing/seeing this effect a lot if
                    // you level up luck stat
                    var prefab = HeroController.instance.takeHitBlackHealthNullifyPrefab;
                    prefab.transform.SetScaleX(0.5f);
                    prefab.transform.SetScaleY(0.5f);
                    Instantiate(prefab, target.transform.position, target.transform.rotation);
                }
            }
        }

        // silk skill, tool and needle base damage
        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(ref HitInstance hitInstance)
            {
                var s = Bonfire.Instance.Status;

                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Bonfire.Log.LogDebug("[LevelingSystem] Recorded a hit => Hit source: " + sourceName);
                Bonfire.Log.LogDebug("Tool instance: " + tool?.name ?? "null");

                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // the "Type == ToolItemType.Skill" should already target only the silk skills, but in case exact 
                    // names are needed for hitInstance.RepresentingTool.name, these are:
                    //   Silk Spear, Thread Sphere, Parry, Silk Charge, Silk Bomb, Silk Boss Needle
                    Bonfire.Log.LogDebug($"<Vanilla> Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = LevelingSystem.SpellDamage(hitInstance.DamageDealt, s.IntelligenceStat);

                    Bonfire.Log.LogDebug($"<Modded> Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");
                }
                else if (tool != null && (tool.Type == ToolItemType.Red || tool.Type == ToolItemType.Blue))
                {
                    // -- red and blue tools --
                    Bonfire.Log.LogDebug($"<Vanilla> Tool: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = LevelingSystem.ToolDamage(hitInstance.DamageDealt, s.ArtificeStat);

                    Bonfire.Log.LogDebug($"<Modded> Tool: {tool}. Damage: {hitInstance.DamageDealt}");
                }
                else if (sourceName.Contains("Slash") || sourceName == "Needle Damage")
                {
                    // -- basic needle hits + needle strike --
                    // 1. "Slash" can be used here, it includes Slash, UpSlash, DownSlash, and AltSlash + Charge Slash 
                    // variants for different crests (these are simply "Charge Slash <crest_name>") which should 
                    // be enough. 
                    // 2. Hollow Knight's mod used "lash", but this would also include something like "Splash Surface" 
                    // which gets recorded if player hits water surface
                    // 3. Finally, the "Needle Damage" check is probably not needed, but there's no harm in including it
                    Bonfire.Log.LogDebug($"<Vanilla> Damage for {sourceName} = {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = LevelingSystem.NeedleDamage(s.StrengthStat);

                    Bonfire.Log.LogDebug($"<Bonfire> Damage for {sourceName} = {hitInstance.DamageDealt}");

                    if (_crit)
                    {
                        hitInstance.DamageDealt = LevelingSystem.CritDamage(s.DexterityStat, hitInstance.DamageDealt);

                        Bonfire.Log.LogDebug($"Crit Damage for {sourceName} = {hitInstance.DamageDealt}");

                        HeroController.instance.GetComponent<SpriteFlash>().FlashGrimmflame();
                        _crit = false;
                    }
                }
            }
        }
    }
}