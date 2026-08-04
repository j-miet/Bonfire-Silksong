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
        public static SettingsGUI Settings;
        public static TeleportGUI Teleport;
        public static LevelingGUI Leveling;

        // all GUI resolution logic should use these instead of Screen.width/Screen.height EXCEPT the scaling itself
        // inside OnGUI callback. This way GUI.Matrix can properly apply its scaling and keep user interfaces 
        // functional on lower/higher resolutions.
        // It's important that GUI is designed on 1920x1080 resolution: everything so far has been done
        // under this assumption and changing it now would a lot of code rewriting.
        public static float Width => 1920f;
        public static float Height => 1080f;

        private bool _benchCheck = false;
        private const bool _applyScaleMatrix = true; // should always stay true unless scaling causes issues

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
            if (_applyScaleMatrix)
            {
                // this must use Screen.width and Screen.height and keep 1920x1080 resolution as base
                float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            }

            if (Bonfire.CheckIfNoGameManager())
            {
                GUI.matrix = oldMatrix;
                return;
            }

            GUIStyles.EnsureFonts();

            if (Teleport.FastTravelDisplay)
            {
                Bonfire.Instance.ShowCursor();
                Teleport.DrawBenchFastTravelUI();
            }

            // then check if game state is playing, paused or in inventory
            // if none of them is true, avoid GUI generation to prevent fps drop in main menu
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

                GUI.enabled = true;
                GUIStyles.EnsureStyles();

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
                LevelingSystem.Instance.ResetPendingLevelUp();
                _benchCheck = false;
            }

            GUI.matrix = oldMatrix;
        }

        private void Update()
        {
            if (Bonfire.CheckIfNoGameManager()) return;

            // bench teleport: check if user presses "Inventory", "Quick Map", "Map" or menu cancel keys, and if so, 
            // close fast travel menu
            if (Teleport.FastTravelDisplay)
            {
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

            Leveling.DrawOtherUIBeginArena();
            GUILayout.Label("Other", GUIStyles.LevelHeaderStyle);

            GUILayout.Space(5);
            Settings.DrawExtraSettingsButton();

            GUILayout.Space(5);
            Teleport.DrawBenchFastTravelButton();

            GUILayout.Space(5);
            GUILayout.EndArea();
        }
    }
}