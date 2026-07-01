using System.Collections.Generic;
using System.IO;
using System.Reflection;

using BepInEx;
using BepInEx.Logging;
using GlobalEnums;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BonfireSilksong
{
    [BepInPlugin("hksilksong.mod.bonfiresilksong", "Bonfire Silksong", "0.2.0")]
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


        private int _critRoll;
        private float silkRegenTime;

        private string SavePath => Path.Combine(Paths.ConfigPath, "BonfireSilksong.json");

        /* Unity's internal method hooks */

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            LoadSettings();

            Logger.LogInfo("Initializing Bonfire");

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Logger.LogInfo("Bonfire initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // TODO not working 100% as intended; see README "Known issues" section for bind cost reduction
        //
        // this + SilkSpool_BindCost patcher handle the silk cost reduction
        // BindCost will set the reduced value, but there's some kind of internal flag which still checks if player has
        // at least 9 silk (or something other blocker with similar idea). This update loop will override that: it
        // keeps forcing player's silk to value 9 in order to cheat this flag check
        private void Update()
        {
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

        // clean enemy hp bars, handle crit roll and passive silk regen from wisdom stat
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
                    int wisInterval = ls.SilkRegenInterval(Instance.Status.WisdomStat);

                    if (i.silkRegenTime > 0)
                    {
                        i.silkRegenTime -= dt;
                    }
                    else
                    {
                        Log.LogDebug("Recovering Silk!");
                        i.silkRegenTime = ls.SilkRegenInterval(Instance.Status.WisdomStat);
                        HeroController.instance.AddSilk(1, heroEffect: false);
                    }
                }
                catch { }
            }
        }
    }
}