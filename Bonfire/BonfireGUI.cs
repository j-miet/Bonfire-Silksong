using System;

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

        private Texture2D _cursorTexture;

        private void Awake()
        {
            Instance = this;
            Settings = new SettingsGUI();
            Teleport = new TeleportGUI();
            Leveling = new LevelingGUI();

            _cursorTexture = CreateCursorTexture();
        }

        private void OnGUI()
        {
            try
            {
                Matrix4x4 oldMatrix = GUI.matrix;
                if (Bonfire.IsNotGamePlayState()) return;

                CustomCursor.ResetCursorStack();
                CustomCursor.Update();

                if (_applyScaleMatrix)
                {
                    // this must use Screen.width and Screen.height and keep 1920x1080 resolution as base
                    float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f);
                    GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                }

                GUIStyles.EnsureFonts();
                GUI.enabled = true;

                if (Teleport.FastTravelDisplay)
                {
                    Bonfire.Instance.ShowCursor();

                    Teleport.DrawBenchFastTravelUI();

                    DrawCustomCursor();

                    GUI.matrix = oldMatrix;

                    return;
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

                    GUIStyles.EnsureStyles();

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
                    Settings.PendingCrestUnlock = false;
                    Settings.PendingSettingsWindow = false;
                    Settings.PendingStatusReset = false;
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
                    DrawCustomCursor();
                }

                Settings.DrawEnemyHealthBars();

                GUI.matrix = oldMatrix;
            }
            catch (Exception ex)
            {
                Bonfire.Log.LogError(ex);
            }
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

        // draws a white disc with black center
        private static Texture2D CreateCursorTexture()
        {
            const int size = 16;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[size * size];
            Color32 clear = new Color32(0, 0, 0, 0);
            Vector2 center = new Vector2(7.5f, 7.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    Color32 c;

                    if (d <= 1.5f)
                        c = Color.black;
                    else if (d <= 4.0f)
                        c = Color.white;
                    else
                        c = clear;

                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return tex;
        }

        private void DrawCustomCursor()
        {
            if (!GameManager.instance.inputHandler.gameController.IsAttached) return;

            int size = 20;
            GUI.color = Color.white;
            GUI.DrawTexture(
                new Rect(CustomCursor.Position.x - size, CustomCursor.Position.y - size, 2 * size, 2 * size),
                _cursorTexture);
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