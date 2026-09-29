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
        public static LoggingOverlay GUILogger;

        // all GUI resolution logic should use these instead of Screen.width/Screen.height EXCEPT the scaling itself
        // inside OnGUI callback. This way GUI.Matrix can properly apply its scaling and keep user interfaces 
        // functional on lower/higher resolutions.
        // It's important that GUI is designed on 1920x1080 resolution: everything so far has been done
        // under this assumption and changing it now would a lot of code rewriting.
        public static float Width => 1920f;
        public static float Height => 1080f;
        // these can be used for manual scaling e.g. if GUI elements pull transform data from the game, it won't get
        // scaled by GUI.Matrix and has to be multiplier by these.
        public static float ScaleX => Width / Screen.width;
        public static float ScaleY => Height / Screen.height;

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
                    HelpWindow.PendingHelpWindow);
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

            GUILogger = new LoggingOverlay();
            BepInEx.Logging.Logger.Listeners.Add(GUILogger);

            InitializeRangeIndicators();
        }

        private void OnDestroy()
        {
            BepInEx.Logging.Logger.Listeners.Remove(GUILogger);
            GUILogger.Dispose();
        }

        private void OnGUI()
        {
            var s = Bonfire.Instance.Status;

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

            if (TeleportGUI.FastTravelDisplay)
            {
                Teleport.DrawBenchFastTravelUI();

                Bonfire.Instance.ShowCursor();
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

                HelpWindow.Display();
            }
            else
            {
                // don't reset PendingFastTravelUnlockWindow here: it will disable itself when closed via button
                // press. 
                SettingsGUI.PendingCrestUnlock = false;
                SettingsGUI.PendingSettingsWindow = false;
                SettingsGUI.PendingStatusReset = false;
                HelpWindow.PendingHelpWindow = false;
            }

            if (!PlayerData.instance.atBench && _benchCheck)
            {
                CustomCursor.ResetCursorPosition();

                LevelingSystem.Instance.ResetPendingLevelUp();

                _benchCheck = false;
            }

            RangeIndicator.UpdateFading();

            Settings.DrawEnemyHealthBars();

            if (s.ShowDamageOverlay)
            {
                DamageOverlay.Display(s.DamageOverlayX, s.DamageOverlayY);
            }

            // debug log window can just render on top of everything else except custom cursor
            if (s.DebugMode && (GUILogger.DisplayLogs || GUILogger.DisplayExtendedLogs))
            {
                GUILogger.Update();
                GUILogger.Display();
            }

            // cursor visual when controller is plugged in
            if (GameManager.instance.inputHandler.gameController.IsAttached &&
                !HeroController.instance.IsPaused() &&
                PlayerData.instance.atBench)
            {
                CustomCursor.DrawCustomCursor();
            }

            GUI.matrix = oldMatrix;
        }

        private void Update()
        {
            if (Bonfire.IsNotGamePlayState()) return;

            // -- GUI hotkeys --

            var inputActions = GameManager.instance?.inputHandler?.inputActions;

            // bench teleport -> close fast travel menu if user presses one the following inputs:
            // "Inventory", 
            // "Quick Map", 
            // "Map", 
            // "QuickCast" ('Spell/Tool' in-game)
            // "Cast" ('Bind' in-game) 
            // menu cancel (Escape on keyboard)
            if (TeleportGUI.FastTravelDisplay)
            {
                if (inputActions == null) return;

                if (inputActions.OpenInventoryMap.WasPressed ||
                    inputActions.QuickMap.WasPressed ||
                    inputActions.OpenInventory.WasPressed ||
                    inputActions.MenuCancel.WasPressed ||
                    inputActions.QuickCast.WasPressed ||
                    inputActions.Cast.WasPressed)
                {
                    Teleport.CloseFastTravelMenu();
                }
                else if (inputActions.DreamNail.WasPressed)
                {
                    // teleport to selected location with "Needolin" hotkey
                    Bonfire.Instance.StartCoroutine(Teleport.DoTeleport());
                }
            }

            // close the uppermost pending window using "QuickCast" or "Cast" input.
            if ((inputActions.QuickCast.WasPressed || inputActions.Cast.WasPressed) && PlayerData.instance.atBench)
            {
                if (SettingsGUI.PendingCrestUnlock || SettingsGUI.PendingStatusReset)
                {
                    SettingsGUI.PendingCrestUnlock = false;
                    SettingsGUI.PendingStatusReset = false;
                    SettingsGUI.PendingSettingsWindow = true;
                }
                else if (SettingsGUI.PendingSettingsWindow)
                {
                    Bonfire.Instance.SaveSettings();
                    SettingsGUI.PendingSettingsWindow = false;
                }
                else if (HelpWindow.PendingHelpWindow) HelpWindow.PendingHelpWindow = false;
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

        private void InitializeRangeIndicators()
        {
            // register all different indicators here. Instances allow multiple different indicators to be displayed at
            // once. Outside the name identifier they all operate similarly and use Display method for range & color
            // customization.
            RangeIndicator.Add(new RangeIndicator("reflect"));
            RangeIndicator.Add(new RangeIndicator("reflectSecondary"));
            RangeIndicator.Add(new RangeIndicator("weaverAura"));
        }
    }
}