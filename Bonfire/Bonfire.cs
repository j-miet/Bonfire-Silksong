using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BonfireSilksong
{
    [BepInPlugin("hksilksong.mod.bonfiresilksong", "Bonfire Silksong", "0.3.0")]
    public class Bonfire : BaseUnityPlugin
    {
        public PlayerStatus Status;

        public static Bonfire Instance;
        internal static ManualLogSource Log;

        public int HitsSinceShielded { get; set; } = 0;
        public bool Crit { get; set; } = false;

        public void SaveSettings()
        {
            var json = JsonUtility.ToJson(Status, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }

        public void LoadSettings()
        {
            if (!File.Exists(SavePath))
            {
                Status = new PlayerStatus();
                return;
            }
            var json = File.ReadAllText(SavePath);
            Status = JsonUtility.FromJson<PlayerStatus>(json);
        }


        private int _critRoll = 0;
        private float _silkRegenTime = 0f;
        private float _toolRegenTime = 0f;
        private bool _blockNextHit = false;

        private ConfigEntry<KeyboardShortcut> _enemyHpBarToggleKey;
        private bool _enemyHPBarsEnabled = false;

        private string SavePath => Path.Combine(Paths.ConfigPath, "BonfireSilksong.json");

        /* Unity's internal method hooks */

        private void Awake()
        {
            Logger.LogInfo("Initializing BonfireSilksong...");

            Instance = this;
            Log = Logger;

            _enemyHpBarToggleKey = Instance.Config.Bind(
                "Hotkeys",
                "Enemy HP bar Toggle",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Toggle enemy HP bars on/off");

            LoadSettings();

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Logger.LogInfo("Bonfire initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            // TODO **Fix this** when modding API gets added or if another solution is found
            //
            // this + SilkSpool_BindCost patcher handle the silk cost reduction
            // BindCost will set the reduced value, but there's some kind of internal flag which still checks if player 
            // has at least 9 silk (or something other blocker with similar idea). This update loop will override that: 
            // it keeps forcing player's silk to value 9 in order to cheat this flag check
            if (GameManager.instance?.hero_ctrl == null)
                return;

            var hero = GameManager.instance.hero_ctrl;
            int cost = LevelingSystem.Instance.BindCost(Status.IntelligenceStat);

            if (!HeroChargeEffects.Instance.IsCharging
                && !hero.cState.casting
                && (bool)InputHandler.Instance.inputActions.Cast
                && hero.playerData.silk >= cost
                && hero.playerData.silk < 9
                && cost < 9)
            {
                hero.playerData.silk = 9;
                hero.RefreshSilk();
            }

            // hotkeys

            // enemy hp bar toggle
            if (_enemyHpBarToggleKey.Value.IsDown())
            {
                _enemyHPBarsEnabled = !_enemyHPBarsEnabled;
                Instance.Status.EnemyHealthBarsEnabled = _enemyHPBarsEnabled;
            }
        }

        private void LateUpdate()
        {
            if (PlayerData.instance.atBench)
            {
                ShowCursor();

                //DebugLogs();
            }
        }

        /* Bonfire's initialization and scene updates */

        private void Init()
        {
            if (PlayerData.instance == null) return;

            if (LevelingSystem.Instance == null)
                gameObject.AddComponent<LevelingSystem>();

            if (BonfireGUI.Instance == null)
                gameObject.AddComponent<BonfireGUI>();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            Init();
        }

        /* -- helpers -- */

        // debugging, call it inside LateUpdate (when sitting on a bench) or inside Update()
        // keep it disabled outside testing, could otherwise heavily affect game performance
        private void DebugLogs()
        {
            // log all tools
            foreach (var tool in Resources.FindObjectsOfTypeAll<ToolItem>())
            {
                Log.LogDebug($"{tool.name} ({tool.GetType().FullName})");
            }

            // log all collectable items
            foreach (var item in Resources.FindObjectsOfTypeAll<CollectableItem>())
                Logger.LogDebug(item.name);

            // log attack speed values
            var config = HeroController.instance?.Config;
            if (config == null) return;
            Log.LogDebug("Attack duration: " + config.AttackDuration);
            Log.LogDebug("Attack cooldown time: " + config.AttackCooldownTime);
            Log.LogDebug("Quick attack speed mult: " + config.QuickAttackSpeedMult);
            Log.LogDebug("Quick attack cooldown time: " + config.QuickAttackCooldownTime);

            // log enemy drops: expensive to calculate, will reduce fps a lot!
            var enemies = Resources.FindObjectsOfTypeAll<HealthManager>();
            if (enemies.Length > 0)
            {
                foreach (var field in enemies[0].GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (field.Name.ToLower().Contains("geo")
                        || field.Name.ToLower().Contains("drop")
                        || field.Name.ToLower().Contains("rosary"))
                        Log.LogDebug($"{field.Name}: {field.GetValue(enemies[0])}");
                }
            }
        }

        // display cursor when player sits on a bench in order to access leveling GUI
        private void ShowCursor()
        {
            if (HeroController.instance != null &&
                HeroController.instance.cState != null &&
                GameManager.instance != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = PlayerData.instance.atBench || GameManager.instance.isPaused;
                return;
            }

            if (GameManager.instance != null)
                Cursor.visible = GameManager.instance.isPaused;
        }

        // rosary and shell shard value updates
        private int GetRosaries(string size, HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField(size + "GeoDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object rosaries = fi.GetValue(enemy);
            return rosaries == null ? 0 : (int)rosaries;
        }

        private int GetShellShards(HealthManager enemy)
        {
            FieldInfo fi = enemy.GetType().GetField("shellShardDrops", BindingFlags.NonPublic | BindingFlags.Instance);
            object shards = fi.GetValue(enemy);
            return shards == null ? 0 : (int)shards;
        }

        // lucky dice visual effect trigger when the player blocks a hit (HealthManager_TakeDamage)
        private void LuckyShieldSetActive()
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
        // the visual effect in HealthManager_Hit (FlashGrimmflame) only applies to player. To also display effect 
        // on the hit enemy, DamageEnemies_DoDamage requires access to following internal fields via reflection
        private static readonly FieldInfo IsHeroDamageField = typeof(DamageEnemies)
            .GetField("isHeroDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo SourceIsHeroField = typeof(DamageEnemies)
            .GetField("sourceIsHero", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool IsHeroDamage(DamageEnemies instance)
        {
            bool a = (bool)(IsHeroDamageField?.GetValue(instance) ?? false);
            bool b = (bool)(SourceIsHeroField?.GetValue(instance) ?? false);
            return a || b;
        }

        /* -- Harmony patches -- */

        // shell shards replenishment
        [HarmonyPatch(typeof(CurrencyManager), "ProcessAddCurrency")]
        class CurrencyManager_ProcessAddCurrency
        {
            static void Postfix(CurrencyType type)
            {
                if (type != CurrencyType.Shard)
                    return;
                if (!Instance.Status.InfiniteShellShardsEnabled)
                    return;
                if (Instance.Status.InfiniteShardsBenchOnly && !PlayerData.instance.atBench)
                    return;

                PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
            }
        }

        [HarmonyPatch(typeof(CurrencyManager), "ProcessTakeCurrency")]
        class CurrencyManager_ProcessTakeCurrency
        {
            static void Postfix(CurrencyType type)
            {
                if (type != CurrencyType.Shard)
                    return;
                if (!Instance.Status.InfiniteShellShardsEnabled)
                    return;
                if (Instance.Status.InfiniteShardsBenchOnly && !PlayerData.instance.atBench)
                    return;

                PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
            }
        }

        // enemy hp bar generation and rosary/shell drop scaling
        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        class HealthManager_OnEnable
        {
            static void Postfix(HealthManager __instance)
            {
                if (__instance != null)
                {
                    var ls = LevelingSystem.Instance;
                    var i = Instance;
                    var s = i.Status;
                    var gui = BonfireGUI.Instance;

                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }

                    Log.LogDebug("[Vanilla] rosary and shell shard values:");
                    Log.LogDebug($"small={i.GetRosaries("small", __instance)}");
                    Log.LogDebug($"medium={i.GetRosaries("medium", __instance)}");
                    Log.LogDebug($"large={i.GetRosaries("large", __instance)}");
                    Log.LogDebug($"shards={i.GetShellShards(__instance)}");

                    Log.LogDebug("[Bonfire] rosary and shell shard values:");
                    Log.LogDebug($"small={ls.IncreaseRosaries(i.GetRosaries("small", __instance), s.LuckStat)}");
                    Log.LogDebug($"medium={ls.IncreaseRosaries(i.GetRosaries("medium", __instance), s.LuckStat)}");
                    Log.LogDebug($"large={ls.IncreaseRosaries(i.GetRosaries("large", __instance), s.LuckStat)}");
                    Log.LogDebug($"shards={ls.IncreaseRosaries(i.GetShellShards(__instance), s.LuckStat)}");

                    __instance.SetGeoSmall(ls.IncreaseRosaries(i.GetRosaries("small", __instance), s.LuckStat));
                    __instance.SetGeoMedium(ls.IncreaseRosaries(i.GetRosaries("medium", __instance), s.LuckStat));
                    __instance.SetGeoLarge(ls.IncreaseRosaries(i.GetRosaries("large", __instance), s.LuckStat));

                    // also scale shell shards using same formula (if player doesn't want to use infinite shell shards)
                    __instance.SetShellShards(ls.IncreaseRosaries(i.GetShellShards(__instance), s.LuckStat));
                }
            }
        }

        // update lifeblood masks
        //
        // TODO **Fix this** when modding API gets added or if another solution is found
        //
        // lifeblood/plasmium masks work internally just fine, but the visual is weird: it always displays one extra
        // mask worth of damage on first hit only, then works normally. For example:
        // - player has 6 blue masks from resiliency stat
        // - player takes a hit dealing 2 masks of damage
        // - visual goes from 6 -> 2 - 1 = 3, but internally blue masks go from 6 -> 4 as expected
        // - afterwards everything works as expected, only just visual displays {current_blue_hp - 1}
        // So taking any damage will subtract the same amount, but of course visual will always display that one
        // extra offset of damage. So eventually if you reach 1 blue mask, the hp bar displays none but you can still
        // absorb a single damage hit before actual masks are removed
        // => no idea how to fix this for now. I've tried lots of different things, but result is always the same:
        // - either visual offset by 1, or 
        // - no blue masks are displayed at all,
        //   but in both cases, internal hp tracking works just fine so it's just a visual mismatch, nothing else
        [HarmonyPatch(typeof(HeroController), "UpdateBlueHealth")]
        class HeroController_UpdateBlueHealth
        {
            static void Postfix(HeroController __instance)
            {
                var ls = LevelingSystem.Instance;
                var s = Instance.Status;
                var pd = PlayerData.instance;

                pd.healthBlue = ls.ExtraMasks(s.ResilienceStat);
                EventRegister.SendEvent(EventRegisterEvents.UpdateBlueHealth);
            }
        }

        // binding cost reduction
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
                int cost = LevelingSystem.Instance.BindCost(Instance.Status.IntelligenceStat);

                Log.LogDebug("Bind cost: " + cost);

                __result = cost;
            }
        }

        [HarmonyPatch(typeof(HeroController), "TakeSilk", typeof(int), typeof(SilkSpool.SilkTakeSource))]
        class HeroController_TakeSilk
        {
            static void Prefix(ref int amount)
            {
                // default bind cost is 9
                if (amount == 9)
                {
                    amount = LevelingSystem.Instance.BindCost(Instance.Status.IntelligenceStat);
                }
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

                float attackSpeed = LevelingSystem.Instance.AttackSpeed(Instance.Status.DexterityStat);

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

                float attackSpeed = LevelingSystem.Instance.AttackSpeed(Instance.Status.DexterityStat);

                typeof(HeroController)
                    .GetField("attackDuration", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(__instance, config.AttackDuration / attackSpeed);
            }
        }

        // damage blocking based on resiliency stat
        [HarmonyPatch(typeof(HeroController), "TakeDamage")]
        class HealthManager_TakeDamage
        {
            static void Prefix(ref int damageAmount, HazardType hazardType)
            {
                Log.LogDebug("Took damage from hazard type: " + hazardType);

                if (hazardType != HazardType.ENEMY) return; // only block enemy attacks

                var s = Instance.Status;
                if (s.ResilienceStat <= 1) return;

                int hitsSinceShielded = Instance.HitsSinceShielded;
                if (hitsSinceShielded > 7)
                    hitsSinceShielded = 7;

                float num = Random.Range(1, 100);
                float iframes = LevelingSystem.Instance.BlockChance(s.ResilienceStat);
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

                Log.LogDebug($"Range value {num} <= {multiplier} * {iframes}");
                if (multiplier > 0 && num <= multiplier * iframes)
                {
                    Instance.HitsSinceShielded = 0;
                    Instance.LuckyShieldSetActive();
                    Instance._blockNextHit = true;
                    Log.LogDebug("Blocked a hit!");
                }
                else
                {
                    Instance.HitsSinceShielded++;
                }
            }

            static void Postfix()
            {
                // safety feature to ensure that block flag is set to 'false' after TakeDamage and its internal 
                // TakeHealth call. Otherwise flag could get stuck on 'true', making player invulnerable
                Instance._blockNextHit = false;
            }
        }

        [HarmonyPatch(typeof(PlayerData), "TakeHealth")]
        class PlayerData_TakeHealth
        {
            static void Prefix(ref int amount)
            {
                if (Instance.Status.ResilienceStat <= 1) return;
                if (Instance._blockNextHit)
                {
                    amount = 0;
                    Instance._blockNextHit = false;
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

                var s = Instance.Status;
                int extraSilk = LevelingSystem.Instance.ExtraSilk(s.WisdomStat, 1) - 1;
                if (extraSilk > 0)
                    HeroController.instance.AddSilk(extraSilk, heroEffect: false);
            }
        }

        // clean enemy hp bars, handle crit roll and passive silk/tool regen from wisdom/artifice stat respectively
        [HarmonyPatch(typeof(HeroController), "Update")]
        class HeroController_Update
        {
            static void Postfix(HeroController __instance)
            {
                var gui = BonfireGUI.Instance;
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
                    Instance._critRoll = Random.Range(1, 100);

                if (HeroController.instance == null || PlayerData.instance == null)
                    return;

                try
                {
                    var i = Instance;
                    var ls = LevelingSystem.Instance;
                    float dt = Time.deltaTime;

                    // passive silk regen
                    if (i._silkRegenTime > 0)
                    {
                        i._silkRegenTime -= dt;
                    }
                    else
                    {
                        Log.LogDebug("Checking silk regen conditions");
                        int silkRegenInterval = ls.SilkRegenInterval(Instance.Status.WisdomStat);
                        if (silkRegenInterval <= 0)
                        {
                            i._silkRegenTime = 1f;
                            return;
                        }

                        i._silkRegenTime = silkRegenInterval;
                        HeroController.instance.AddSilk(1, heroEffect: false);

                        Log.LogDebug("Regenerated 1 silk");
                    }
                }
                catch { }
            }
        }

        // handle crit roll (prefix) and crit effect when enemy gets hit (postfix)
        // DamageEnemies_DoDamage calls patched HealthManager_Hit internally so logic goes:
        // - Prefix rolls crit chance
        // - DoDamage calls Hit at some point
        // - Hit checks and handles player's perspective by adding damage and displaying visual effect on player
        // - Postfix adds the effect on enemy that got hit
        [HarmonyPatch(typeof(DamageEnemies), "DoDamage", new[] { typeof(GameObject), typeof(bool) })]
        internal static class DamageEnemies_DoDamage
        {
            static void Prefix(DamageEnemies __instance, GameObject target)
            {
                if (target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                var s = Instance.Status;
                var ls = LevelingSystem.Instance;

                int critChance = ls.CritChance(s.LuckStat);
                if (critChance < 1) // must have at least 1% crit chance
                {
                    Instance.Crit = false;
                    return;
                }

                int critRoll = Instance._critRoll;

                Log.LogDebug($"Crit chance: {critChance}. Rolled {critRoll}.");

                Instance.Crit = critRoll <= critChance;
            }

            static void Postfix(DamageEnemies __instance, GameObject target, bool __result)
            {
                if (!__result || target == null || target.layer != 11) return;
                if (!IsHeroDamage(__instance)) return;

                if (Instance.Crit)
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

        // silk skill, needle and needle strike damage
        [HarmonyPatch(typeof(HealthManager), "Hit")]
        class HealthManager_Hit
        {
            static void Prefix(ref HitInstance hitInstance)
            {
                var s = Instance.Status;
                var ls = LevelingSystem.Instance;

                if (hitInstance.Source == null) return;

                string sourceName = hitInstance.Source.name;
                var tool = hitInstance.RepresentingTool;

                Log.LogDebug("Recorded a hit => Hit source: " + sourceName);
                Log.LogDebug("Tool instance: " + tool?.name ?? "null");

                if (tool != null && tool.Type == ToolItemType.Skill)
                {
                    // -- silk skills --
                    // the "Type == ToolItemType.Skill" should already target only the silk skills, but in case exact 
                    // names are needed for hitInstance.RepresentingTool.name, these are:
                    //   Silk Spear, Thread Sphere, Parry, Silk Charge, Silk Bomb, Silk Boss Needle
                    Log.LogDebug($"[Vanilla] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt =
                        LevelingSystem.Instance.SpellDamage(hitInstance.DamageDealt, s.IntelligenceStat);

                    Log.LogDebug($"[Bonfire] Silk Skill: {tool}. Damage: {hitInstance.DamageDealt}");
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
                    Log.LogDebug($"[Vanilla] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    hitInstance.DamageDealt = ls.NeedleDamage(s.StrengthStat);

                    Log.LogDebug($"[Bonfire] Damage for {sourceName} = {hitInstance.DamageDealt}");

                    if (Instance.Crit)
                    {
                        hitInstance.DamageDealt = ls.CritDamage(s.DexterityStat, hitInstance.DamageDealt);

                        Log.LogDebug($"[Crit] Damage for {sourceName} = {hitInstance.DamageDealt}");

                        HeroController.instance.GetComponent<SpriteFlash>().FlashGrimmflame();
                        Instance.Crit = false;
                    }
                }
            }
        }
    }
}