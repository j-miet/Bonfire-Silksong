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
        public static LevelingGUI Leveling;
        public static SettingsGUI Settings;
        public static TeleportGUI Teleport;

        // all GUI resolution logic should use these instead of Screen.width/Screen.height EXCEPT the scaling itself
        // inside OnGUI callback. This way GUI.Matrix can properly apply its scaling and keep user interfaces 
        // functional on lower/higher resolutions.
        // It's important that GUI is designed on 1920x1080 resolution: everything so far has been done
        // under this assumption and changing it now would a lot of code rewriting.
        public static float Width => 1920f;
        public static float Height => 1080f;


        private bool _benchCheck = false;

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

        private void Awake()
        {
            Instance = this;
            Settings = new SettingsGUI();
            Teleport = new TeleportGUI();
            Leveling = new LevelingGUI();
        }

        private void OnGUI()
        {
            Matrix4x4 oldMatrix = GUI.matrix;
            if (Bonfire.IsNotGamePlayState()) return;

            CustomCursor.ResetCursorStack();
            CustomCursor.Update();

            // apply scaling to adjust GUI size to current resolution
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));

            GUIStyles.EnsureFonts();
            GUIStyles.EnsureStyles();
            Settings.EnsureHpBarColors();

            GUI.enabled = true;

            if (Teleport.FastTravelDisplay)
            {
                Bonfire.Instance.ShowCursor();

                Teleport.DrawBenchFastTravelUI();

                CustomCursor.DrawCustomCursor();

                GUI.matrix = oldMatrix;

                return;
            }

            // check if game state is playing, paused or in inventory
            // if none of them is true, avoid GUI rendering to prevent fps drop in main menu
            if (!(GameManager.instance.GameState == GameState.PLAYING ||
                  GameManager.instance.GameState == GameState.PAUSED) ||
                InInventory())
            {
                GUI.matrix = oldMatrix;
                return;
            }

            if (PlayerData.instance.atBench && !GameManager.instance.isPaused)
            {
                _benchCheck = true;

                GUI.backgroundColor = Color.white;
                GUI.contentColor = Color.white;
                GUI.color = Color.white;

                DrawBenchUI();

                Teleport.FastTravelUnlockNotification();
                Settings.DrawExtraSettingsUI();
                Settings.DrawCrestUnlockUI();
                Settings.DrawPlayerStatusResetUI();
            }
            else
            {
                SettingsGUI.PendingCrestUnlock = false;
                SettingsGUI.PendingSettingsWindow = false;
                SettingsGUI.PendingStatusReset = false;
            }

            if (!PlayerData.instance.atBench && _benchCheck)
            {
                CustomCursor.ResetCursorPosition();

                LevelingSystem.Instance.ResetPendingLevelUp();

                _benchCheck = false;
            }

            // cursor visual when controller is plugged in
            if (GameManager.instance.inputHandler.gameController.IsAttached &&
                !HeroController.instance.IsPaused() &&
                PlayerData.instance.atBench)
            {
                CustomCursor.DrawCustomCursor();
            }

            Settings.DrawEnemyHealthBars();

            RangeIndicator.UpdateFading();

            GUI.matrix = oldMatrix;
        }

        private void Update()
        {
            if (Bonfire.IsNotGamePlayState()) return;

            // bench teleport: check if user presses "Inventory", "Quick Map", "Map" or menu cancel keys, and if so, 
            // close fast travel menu
            if (Teleport.FastTravelDisplay)
            {
                var inputActions = GameManager.instance?.inputHandler?.inputActions;
                if (inputActions == null) return;

                if (inputActions.OpenInventoryMap.WasPressed ||
                    inputActions.QuickMap.WasPressed ||
                    inputActions.OpenInventory.WasPressed ||
                    inputActions.MenuCancel.WasPressed)
                {
                    Teleport.CloseFastTravelMenu();
                }
            }
        }

        private void DrawBenchUI()
        {
            Leveling.DrawLevelingUI();

            Leveling.DrawOtherUIBeginArea();
            GUILayout.Label("Other", GUIStyles.LevelHeaderStyle);

            GUILayout.Space(5);
            Settings.DrawExtraSettingsButton();

            GUILayout.Space(5);
            Teleport.DrawBenchFastTravelButton();

            GUILayout.Space(5);
            CustomCursor.EndArea(); // close DrawOtherUIBeginArea component
        }
    }
}