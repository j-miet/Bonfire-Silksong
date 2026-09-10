using GlobalEnums;
using HutongGames.PlayMaker;
using UnityEngine;

using BonfireSilksong.GUIComponents;
using BonfireSilksong.Leveling;
using BonfireSilksong.Leveling.UI;
using BonfireSilksong.Settings;
using BonfireSilksong.Teleport;

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

        /// <summary>
        /// Verifies no top-level windows are open/requesting to be opened.
        /// </summary>
        public static bool NoWindowsPending
        {
            get
            {
                return !(SettingsGUI.PendingSettingsWindow ||
                    SettingsGUI.PendingCrestUnlock ||
                    SettingsGUI.PendingStatusReset ||
                    TeleportGUI.PendingFastTravelUnlockWindow ||
                    InfoWindow.PendingInfoWindow);
            }
        }


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

                GUI.enabled = NoWindowsPending;
                DrawBenchUI();
                GUI.enabled = true;

                Settings.DrawExtraSettingsUI();
                Settings.DrawCrestUnlockUI();
                Settings.DrawPlayerStatusResetUI();

                InfoWindow.Display();
            }
            else
            {
                // don't reset PendingFastTravelUnlockWindow here: it will disable itself when closed via button
                // press. 
                SettingsGUI.PendingCrestUnlock = false;
                SettingsGUI.PendingSettingsWindow = false;
                SettingsGUI.PendingStatusReset = false;
                InfoWindow.PendingInfoWindow = false;
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

            if (Bonfire.Instance.Status.ShowDamageOverlay)
                DamageOverlay.Display(Bonfire.Instance.Status.DamageOverlayX, Bonfire.Instance.Status.DamageOverlayY);

            RangeIndicator.UpdateFading();

            GUI.matrix = oldMatrix;
        }

        private void Update()
        {
            if (Bonfire.IsNotGamePlayState()) return;

            var inputActions = GameManager.instance?.inputHandler?.inputActions;

            // bench teleport -> close fast travel menu if user presses one the following inputs:
            // "Inventory", "Quick Map", "Map", "QuickCast" ('Spell/Tool' in-game) or menu cancel (Escape on keyboard)
            if (Teleport.FastTravelDisplay)
            {
                if (inputActions == null) return;

                if (inputActions.OpenInventoryMap.WasPressed ||
                    inputActions.QuickMap.WasPressed ||
                    inputActions.OpenInventory.WasPressed ||
                    inputActions.MenuCancel.WasPressed ||
                    inputActions.QuickCast.WasPressed)
                {
                    Teleport.CloseFastTravelMenu();
                }
                else if (inputActions.DreamNail.WasPressed)
                {
                    // teleport to selected location with "Needolin" hotkey
                    Bonfire.Instance.StartCoroutine(Teleport.DoTeleport());
                }
            }

            // close the uppermost pending window using "QuickCast" input.
            if (inputActions.QuickCast.WasPressed && PlayerData.instance.atBench)
            {
                if (SettingsGUI.PendingCrestUnlock || SettingsGUI.PendingStatusReset)
                {
                    SettingsGUI.PendingCrestUnlock = false;
                    SettingsGUI.PendingStatusReset = false;
                    SettingsGUI.PendingSettingsWindow = true;
                }
                else if (SettingsGUI.PendingSettingsWindow) SettingsGUI.PendingSettingsWindow = false;
                else if (InfoWindow.PendingInfoWindow) InfoWindow.PendingInfoWindow = false;
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