using System.IO;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Leveling;
using BonfireSilksong.Settings;
using BonfireSilksong.Teleport;
using System.Reflection;

namespace BonfireSilksong
{
    /// <summary>
    /// Main class for Bonfire-Silksong mod.
    /// </summary>
    [BepInPlugin(ModID, ModName, ModVersion)]
    public class Bonfire : BaseUnityPlugin
    {
        public const string ModID = "jeooh.silksong.bonfire-silksong";
        public const string ModName = "Bonfire-Silksong";
        public const string ModVersion = "1.0.2";
        public const string LocalizationFile = "en.loc";

        public static Bonfire Instance;
        internal static ManualLogSource Log = BepInEx.Logging.Logger.CreateLogSource(ModName);

        public PlayerStatus Status;
        public bool SessionInitialized = false;

        public float SessionInitTimer { get; private set; }

        /// <summary>
        /// Check if game is in main menu or non-gameplay scene. Should be called at the start of each Unity callback
        /// to prevent mod logic from running when it's unnecessary.
        /// </summary>
        public static bool IsNotGamePlayState()
        {
            if (GameManager.instance == null ||
                GameManager.instance.GameState == GameState.MAIN_MENU ||
                !GameManager.instance.IsGameplayScene())
            {
                return true;
            }

            return false;
        }

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
            if (GameManager.instance == null) return;

            if (GameManager.instance.inputHandler.gameController.IsAttached &&
                !Instance.Status.DisplayGameCursorWithGamepad)
            {
                Cursor.visible = false;
                return;
            }

            if (HeroController.instance?.cState != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = PlayerData.instance.atBench || GameManager.instance.isPaused;

                return;
            }

            Cursor.visible = GameManager.instance.isPaused;
        }


        // _firstSave guard flag: it stays true until GameManager.instance != null and its profileID field has value 
        // greater than 1 which corresponds to a valid save slot (1-4). And every time the user quits to main menu, it 
        // flips back  to true upon detecting invalid profileID. Then, when user continues from another save slot, it 
        // prevents SaveSettings from using previous save's PlayerStatus data to override data for current slot.
        private bool _firstSave = true;

        private bool _hotkeysInitialized = false;
        private ConfigEntry<KeyboardShortcut> _enemyHpBarHotkey;
        private bool _enemyHPBarsEnabled;
        private ConfigEntry<KeyboardShortcut> _damageOverlayHotkey;
        private bool _damageOverlayEnabled;

        private readonly KeyboardShortcut _debugTeleport = new KeyboardShortcut(KeyCode.F2, KeyCode.LeftControl);
        private readonly KeyboardShortcut _debugShowLogs = new KeyboardShortcut(KeyCode.F3, KeyCode.LeftControl);
        private readonly KeyboardShortcut _debugShowExtendedLogs = new KeyboardShortcut(
            KeyCode.F4, KeyCode.LeftControl);

        private bool _invClosedCheck = false;
        private bool _satDown = false;

        // -- Unity callbacks -- //

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Localization.Loc = new Localization();
            Localization.Loc.Load(Path.Combine(pluginDir, LocalizationFile));

            Log.LogInfo("Initializing Bonfire-Silksong...");

            _enemyHpBarHotkey = Instance.Config.Bind(
                "Hotkeys",
                "Enemy HP bar Toggle",
                new KeyboardShortcut(KeyCode.H, KeyCode.LeftControl),
                "Toggle enemy HP bars on/off");

            _damageOverlayHotkey = Instance.Config.Bind(
                "Hotkeys",
                "Damage info Toggle",
                new KeyboardShortcut(KeyCode.L, KeyCode.LeftControl),
                "Toggle damage info panel on/off");

            LoadSettings();
            _firstSave = true;

            Harmony.CreateAndPatchAll(typeof(Bonfire).Assembly);

            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneLoaded += PaleNailsSpawner.OnSceneLoaded;
            SceneManager.sceneLoaded += RangeIndicator.OnSceneLoaded;

            Log.LogInfo("Bonfire-Silksong initialized");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded -= PaleNailsSpawner.OnSceneLoaded;
            SceneManager.sceneLoaded -= RangeIndicator.OnSceneLoaded;
        }

        private void Update()
        {
            LevelingSystem.ResetInitFlags();
            CheckSessionInit();

            if (IsNotGamePlayState()) return;

            IntelligenceStat.UpdateBindFSMValues();

            AtBenchCheck();

            CheckConfigHotkeys();
            CheckDebugHotkeys();
        }

        private void LateUpdate()
        {
            if (IsNotGamePlayState()) return;

            var pd = PlayerData.instance;
            var gm = GameManager.instance;
            if (pd == null || gm == null) return;

            if (pd.atBench)
            {
                if (!pd.isInventoryOpen && !gm.isPaused)
                {
                    ShowCursor();

                    // this check refreshes blue masks after inventory/pause menu is closed, otherwise they get wiped
                    if (_invClosedCheck)
                    {
                        BlueHealthUpdate.CallUpdateBlueHP();

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

            SettingsPatch.RegisterCrestChapelScenes(GameManager.instance?.GetSceneNameString());

            LevelingSystem.Instance.RefreshRelicsDeposited();
            LevelingSystem.MilestoneBonus = ResilienceStat.GetGlassCannonMilestoneBonus(Status.ResilienceStat);

            WisdomStat.UpdateCogflyWarriorSpawnLimit();
        }

        // -- other --

        // a safety check to ensure game has had enough time to load in the save file.
        // Current use case is to prevent access to bench fast travel button before game allows the player to stand
        // up from bench. Issue is that when game map is accessed before this loading period, it seems mess with 
        // position calculations, leaving the player in a falling animation state after map is closed. So the fix is
        // to simulate this period manually.
        // Pausing the game prevents timer from increasing because game hud will also stop rendering during pause and 
        // hud being fully generated is a good indicator.
        // - Init flag has to be reset after entering main menu again so that this check may run each time.
        // - If a better flag will be found from game code itself, that'd nice. Until then this will suffice.
        private void CheckSessionInit()
        {
            if (SessionInitialized || GameManager.instance?.GameState == GameState.MAIN_MENU)
            {
                SessionInitTimer = 10f; // adjust this if the wait period feels too short/long
                return;
            }

            SessionInitTimer -= Time.deltaTime;
            if (SessionInitTimer > 0f)
            {
                return;
            }

            SessionInitialized = true;
        }

        // update when player sits down on a bench and flip it only after standing up.
        // Used for blue hp updates where an additional flag needs to cancel ongoing update loop if player ends up
        // sitting down again while it's still processing
        private void AtBenchCheck()
        {
            if (PlayerData.instance == null) return;

            if (PlayerData.instance.atBench && !_satDown)
            {
                Log.LogDebug("[Bonfire] Sat down on a bench");

                // Debug.VisitedScenes();
                // Log.LogDebug("----------------------");
                // Debug.GetAllScenes();
                // Log.LogDebug("----------------------");
                // Debug.GetAllBenchScenes();
                // Log.LogDebug("----------------------");
                // Debug.GetHornetAllFsmStateAction();
                // Log.LogDebug("-----------------------");
                // Debug.HornetFSMVariables();
                // Log.LogDebug("-----------------------");

                _satDown = true;
                BlueHealthUpdate.SatDownAtBench = true;

                SaveSettings();

                IntelligenceStat.DisableWeaverAura();
                LevelingPatch.Taunted = false;

                // this causes BlueHpCheckInvOrMenu check to be ignored on first blue hp update call which happens
                // right after loading into a save.
                if (!BlueHealthUpdate.BlueHpInit)
                {
                    BlueHealthUpdate.SatDownAtBench = false;
                }
            }
            else if (!PlayerData.instance.atBench && _satDown)
            {
                _satDown = false;
                BlueHealthUpdate.SatDownAtBench = false;

                WisdomStat.UpdateToolEquippedState();
            }
        }

        private void CheckConfigHotkeys()
        {
            if (!_hotkeysInitialized)
            {
                _enemyHPBarsEnabled = Instance.Status.EnemyHealthBarsEnabled;
                _damageOverlayEnabled = Instance.Status.ShowDamageOverlay;
                _hotkeysInitialized = true;
            }

            if (_enemyHpBarHotkey.Value.IsDown())
            {
                _enemyHPBarsEnabled = !_enemyHPBarsEnabled;
                Instance.Status.EnemyHealthBarsEnabled = _enemyHPBarsEnabled;
            }

            if (_damageOverlayHotkey.Value.IsDown())
            {
                _damageOverlayEnabled = !_damageOverlayEnabled;
                Instance.Status.ShowDamageOverlay = _damageOverlayEnabled;
            }
        }

        private void CheckDebugHotkeys()
        {
            var gl = BonfireGUI.GUILogger;
            if (gl == null) return;

            if (Status.DebugTeleport && _debugTeleport.IsDown())
            {
                var pd = PlayerData.instance;
                pd.tempRespawnScene = "Tut_03";
                pd.tempRespawnMarker = "RestBench";
                pd.tempRespawnType = 1;
                BenchTeleport.TeleportToBench();
            }

            if (Status.DebugLogs)
            {
                if (_debugShowLogs.IsDown())
                {
                    if (gl.DisplayLogs)
                    {
                        gl.DisableLogsDisplay();
                    }
                    else
                    {
                        gl.SetLogsDisplay();
                    }
                }
                else if (_debugShowExtendedLogs.IsDown())
                {
                    if (gl.DisplayExtendedLogs)
                    {
                        gl.DisableLogsDisplay();
                    }
                    else
                    {
                        gl.SetExtendedLogsDisplay();
                    }
                }
            }
            else
            {
                if (gl.DisplayLogs || gl.DisplayExtendedLogs)
                {
                    gl.Dispose();
                    gl.DisableLogsDisplay();
                }
            }
        }
    }
}