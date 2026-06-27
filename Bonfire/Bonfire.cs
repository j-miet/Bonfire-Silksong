using System.IO;

using BepInEx;
using GlobalSettings;
using HarmonyLib;
using UnityEngine;

namespace Bonfire
{
    [BepInPlugin("hk_silksong.mod.bonfire", "Bonfire Silksong", "0.1.0")]
    public class Bonfire : BaseUnityPlugin
    {
        public static Bonfire Instance;
        public PlayerStatus Status;

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

        private void Awake()
        {
            Logger.LogInfo("Awake bonfire");
            Instance = this;

            LoadSettings();

            // TODO add LevelingSystem here later
            gameObject.AddComponent<BonfireGUI>();

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            Logger.LogInfo("Bonfire mod initialized");
        }

        private void Update()
        {
            if (PlayerData.instance.atBench && Status.InfiniteShellShardsEnabled && Status.InfiniteShardsBenchOnly)
                PlayerData.instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);

            SaveSettings();
        }

        private void LateUpdate()
        {
            if (PlayerData.instance.atBench)
            {
                ShowCursor();
            }
        }

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

        // patched methods

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerData), "AddShards")]
        private static void AddShards_PostFix(PlayerData __instance, int amount)
        {
            if (!Instance.Status.InfiniteShellShardsEnabled)
                return;

            if (Instance.Status.InfiniteShardsBenchOnly)
            {
                if (!PlayerData.instance.atBench)
                    return;
            }

            __instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerData), "TakeShards")]
        private static void TakeShards_PostFix(PlayerData __instance, int amount)
        {
            if (!Instance.Status.InfiniteShellShardsEnabled)
                return;

            if (Instance.Status.InfiniteShardsBenchOnly)
            {
                if (!PlayerData.instance.atBench)
                    return;
            }

            __instance.ShellShards = Gameplay.GetCurrencyCap(CurrencyType.Shard);
        }

        [HarmonyPatch(typeof(HealthManager), "OnEnable")]
        class HealthManager_OnEnable
        {
            static void Postfix(HealthManager __instance)
            {
                if (__instance != null)
                {
                    var gui = BonfireGUI.Instance;
                    if (!gui.EnemyMaxHp.ContainsKey(__instance))
                    {
                        gui.EnemyMaxHp.Add(__instance, __instance.hp);
                        gui.EnemyIsBoss.Add(__instance, BossSceneController.Instance != null);
                    }
                }
            }
        }

    }
}