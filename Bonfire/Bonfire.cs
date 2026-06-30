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


        private string SavePath => Path.Combine(Paths.ConfigPath, "BonfireSilksong.json");

        // Unity's internal method hooks

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

        private void LateUpdate()
        {
            if (PlayerData.instance.atBench)
            {
                ShowCursor();

                //DebugLogs();
            }
        }

        // Bonfire's own methods begin here

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

            // log enemy drops, expensive to calculate, will reduce fps a lot!
            var enemies = Resources.FindObjectsOfTypeAll<HealthManager>();
            if (enemies.Length > 0)
            {
                foreach (var field in enemies[0].GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (field.Name.ToLower().Contains("geo") || field.Name.ToLower().Contains("drop") || field.Name.ToLower().Contains("rosary"))
                        Log.LogDebug($"{field.Name}: {field.GetValue(enemies[0])}");
                }
            }
        }

        private void Init()
        {
            if (PlayerData.instance == null) return;

            if (LevelingSystem.Instance == null)
                gameObject.AddComponent<LevelingSystem>();

            if (BonfireGUI.Instance == null)
                gameObject.AddComponent<BonfireGUI>();
        }

        // hook attached to SceneManager.sceneLoaded, called on each scene transition.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SaveSettings();
            Init();
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

        // helpers for rosary and shell shard value updates
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

        // patched methods using HarmonyPatch

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

        // enemy hp bar generation + rosary drop scaling based on luck stat
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
    }
}