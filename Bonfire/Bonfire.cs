using System.IO;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BonfireSilksong
{
    /// <summary>
    /// Main class for Bonfire-Silksong mod
    /// </summary>
    [BepInPlugin("hksilksong.mod.bonfire-silksong", "Bonfire-Silksong", "0.7.0")]
    public class Bonfire : BaseUnityPlugin
    {
        public PlayerStatus Status;

        public static Bonfire Instance;
        internal static ManualLogSource Log;

        public void SaveSettings()
        {
            if (GameManager.instance == null) return;

            int saveProfile = GameManager.instance.profileID;
            Log.LogDebug("[Bonfire] SaveSettings: save slot " + saveProfile);

            if (saveProfile < 1)
            {
                _firstSave = true;
                return;
            }

            if (_firstSave)
            {
                LoadSettings();
                _firstSave = false;
                return;
            }

            var json = JsonUtility.ToJson(Status, prettyPrint: true);
            File.WriteAllText(SavePath(), json);
        }

        public void LoadSettings()
        {
            if (GameManager.instance == null)
            {
                Status = new PlayerStatus();
                return;
            }

            int saveProfile = GameManager.instance.profileID;
            Log.LogDebug("[Bonfire] LoadSettings: save slot " + saveProfile);

            if (saveProfile < 1)
            {
                _firstSave = true;
                Status = new PlayerStatus();
                return;
            }

            if (!File.Exists(SavePath()))
            {
                Status = new PlayerStatus();
                return;
            }

            var json = File.ReadAllText(SavePath());
            Status = JsonUtility.FromJson<PlayerStatus>(json);
        }

        /// <summary>
        /// Display cursor when player sits on a bench in order to access leveling GUI.
        /// Can be used in enabling cursor outside menu screens, but also in map menu screen for fast travel.
        /// If called in Update callbacks, use LateUpdate instead or game will still hide the cursor.
        /// </summary>
        public void ShowCursor()
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

        /// <summary>
        /// When saving and exiting, sometimes the game inputs become unresponsive for several seconds in main menu 
        /// before resuming normally. Checking if GameManager is null/game is still in gameplay scene/game is in main 
        /// menu screen then preventing update loops might help a bit (maybe makes it less frequent?) but doesn't fix 
        /// this entirely.
        /// </summary>
        public static bool CheckIfNoGameManager()
        {
            if (GameManager.instance.GameState == GameState.MAIN_MENU ||
                GameManager.instance == null ||
                !GameManager.instance.IsGameplayScene())
            {
                Log.LogDebug("[Bonfire] GameManager instance null or in gameplay scene, or GameState is main menu");
                return true;
            }

            return false;
        }


        // _firstSave guard flag: it stays true until GameManager.instance != null and its profileID field has value 
        // greater than 1 which corresponds to a valid save slot (1-4). And every time the user quits to main menu, it 
        // flips back  to true upon detecting invalid profileID. Then, when user continues from another save slot, it 
        // prevents SaveSettings from using previous save's PlayerStatus data to override data for current slot.
        private bool _firstSave = true;

        private ConfigEntry<KeyboardShortcut> _enemyHpBarToggleHotkey;
        private bool _enemyHPBarsEnabled = false;

        private bool _invClosedCheck = false;
        private bool _satDown = false;

        // -- Unity callbacks -- //

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo("Initializing Bonfire-Silksong...");

            _enemyHpBarToggleHotkey = Instance.Config.Bind(
                "Hotkeys",
                "Enemy HP bar Toggle",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Toggle enemy HP bars on/off");

            LoadSettings();
            _firstSave = true;

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            SceneManager.sceneLoaded += OnSceneLoaded;

            Log.LogInfo("Bonfire-Silksong initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            LevelingSystem.ResetInitFlags();

            if (CheckIfNoGameManager()) return;

            LevelingSystem.UpdateBindCostFSM();

            // update when player sits down on a bench and flip it only after standing up
            // useful for blue hp updates where an additional flag needs to cancel previous update if player should
            // sit down again while previous loop is still processing
            if (PlayerData.instance.atBench && !_satDown)
            {
                Log.LogDebug("[Bonfire] Bench sat down check");
                _satDown = true;
                LevelingSystem.SatDownAtBench = true;

                // this causes BlueHpCheckInvOrMenu check to be ignored on first blue hp update call which happens
                // right after loading into a save.
                if (!LevelingSystem.BlueHpInit)
                {
                    LevelingSystem.SatDownAtBench = false;
                }
            }
            else if (!PlayerData.instance.atBench)
            {
                _satDown = false;
                LevelingSystem.SatDownAtBench = false;
            }

            // enemy hp bar toggle hotkey
            if (_enemyHpBarToggleHotkey.Value.IsDown())
            {
                _enemyHPBarsEnabled = !_enemyHPBarsEnabled;
                Instance.Status.EnemyHealthBarsEnabled = _enemyHPBarsEnabled;
            }
        }

        private void LateUpdate()
        {
            if (CheckIfNoGameManager()) return;

            if (PlayerData.instance == null) return;

            var pd = PlayerData.instance;

            if (pd.atBench)
            {
                if (!pd.isInventoryOpen && !GameManager.instance.isPaused)
                {
                    ShowCursor();

                    // this check refreshes blue masks after inventory is closed, otherwise they get wiped
                    if (_invClosedCheck)
                    {
                        LevelingSystem.UpdateBlueHP();

                        _invClosedCheck = false;
                    }
                }
                else
                    _invClosedCheck = true;
            }
        }

        // -- mod initialization and scene updates -- //

        private string SavePath()
        {
            int saveProfile = GameManager.instance.profileID;
            return Path.Combine(Paths.ConfigPath, $"Bonfire-Silksong_Save{saveProfile}.json");
        }

        // if class uses Unity callbacks e.g. Awake, Update, OnGUI etc. they must be added here.
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
            LevelingSystem.Instance.RefreshRelicsObtained();

            //Log.LogDebug("[Bonfire] Current scene: " + GameManager.instance.GetSceneNameString());

            //Debug.VisitedScenes();
        }

    }
}