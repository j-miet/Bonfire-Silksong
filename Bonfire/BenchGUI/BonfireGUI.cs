using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Creates and updates GUI elements.
    /// </summary>
    public class BonfireGUI : MonoBehaviour
    {
        public static BonfireGUI Instance;
        public static Settings Settings;
        public static Teleport Teleport;
        public static Leveling Leveling;


        private bool _benchCheck = false;

        private void Awake()
        {
            Instance = this;
            Settings = new Settings();
            Teleport = new Teleport();
            Leveling = new Leveling();
        }

        private void OnGUI()
        {
            if (GameManager.instance == null && PlayerData.instance == null) return;

            Styles.EnsureFonts();

            if (Teleport.FastTravelDisplay)
            {
                Bonfire.Instance.ShowCursor();
                Teleport.DrawBenchFastTravelUI();
            }

            // then check if game state is playing, paused or in inventory
            // if none of them is true, avoid running to prevent lag in main menu
            if (!(GameManager.instance.GameState == GameState.PLAYING ||
                  GameManager.instance.GameState == GameState.PAUSED) ||
                InInventory())
                return;

            if (PlayerData.instance.atBench && !GameManager.instance.isPaused)
            {
                _benchCheck = true;

                GUI.enabled = true;
                Styles.EnsureStyles();

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.color = Color.white;

                DrawBenchUI();
            }

            Teleport.FastTravelUnlockNotification();
            Settings.DrawExtraSettingsUI();
            Settings.DrawCrestUnlockUI();
            Settings.DrawPlayerStatusResetUI();

            Settings.DrawEnemyHealthBars();  // hp bar display must not be tied to bench check

            if (!PlayerData.instance.atBench && _benchCheck)
            {
                Leveling.ResetPendingLevelUp();
                _benchCheck = false;
            }
        }

        private void Update()
        {
            // -- bench teleport --
            // check if user presses "Inventory", "Quick Map", "Map" or menu cancel keys, and if so, closes travel menu
            if (!Teleport.FastTravelDisplay) return;

            var inputActions = GameManager.instance?.inputHandler?.inputActions;
            if (inputActions == null) return;

            if (inputActions.OpenInventoryMap.WasPressed ||
                inputActions.QuickMap.WasPressed ||
                inputActions.OpenInventory.WasPressed ||
                inputActions.MenuCancel)
            {
                Teleport.CloseFastTravelMenu();
            }
        }

        // check if game is in inventory screen.
        private static bool InInventory()
        {
            GameObject go = GameObject.FindGameObjectWithTag("Inventory Top");
            if (go == null) return false;

            PlayMakerFSM fsm = FSMUtility.LocateFSM(go, "Inventory Control");
            if (fsm == null) return false;

            FsmBool open = fsm.FsmVariables.GetFsmBool("Open");
            return open != null && open.Value;
        }

        private void DrawBenchUI()
        {
            Leveling.DrawLevelingUI();
            GUILayout.Space(10);
            GUILayout.Label("Other", Styles.LevelHeaderStyle);

            GUILayout.Space(5);
            Settings.DrawExtraSettingsButton();

            GUILayout.Space(5);
            Teleport.DrawBenchFastTravelButton();

            GUILayout.Space(5);
            GUILayout.EndArea();
        }
    }
}