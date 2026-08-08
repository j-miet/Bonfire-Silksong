using System.Collections.Generic;

using HarmonyLib;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace BonfireSilksong
{
    /// <summary>
    /// Custom cursor for gamepad devices.
    /// </summary>
    public static class CustomCursor
    {
        public const int BaseSpeed = 400;
        public const int MinSpeed = 50;
        public const int MaxSpeed = 1000;

        public static Vector2 Position = new Vector2(BonfireGUI.Width / 2f, BonfireGUI.Height / 2f);
        public static int CursorSpeed = 400;

        public static void ResetCursorStack()
        {
            Bonfire.Log.LogDebug("[CustomCursor] Resetting stack");

            _currentOffset = Vector2.zero;
            _offsetStack.Clear();
        }

        public static void ResetCursorPosition()
        {
            Bonfire.Log.LogDebug("[CustomCursor] Resetting position");
            Position = new Vector2(BonfireGUI.Width / 2f, BonfireGUI.Height / 2f);
        }

        public static void Update()
        {
            if (!PlayerData.instance.atBench && _offsetStack.Count > 0)
            {
                ResetCursorPosition();
                ResetCursorStack();
            }

            if (GameManager.instance?.inputHandler?.inputActions == null)
                return;

            if (!GameManager.instance.inputHandler.gameController.IsAttached) return;

            Bonfire.Log.LogDebug("[CustomCursor] Cursor update started");

            var device = GameManager.instance.inputHandler.inputActions.ActiveDevice;

            float x = device.RightStickX.Value;
            float y = device.RightStickY.Value;

            CursorSpeed = Bonfire.Instance.Status.CustomCursorSpeed;
            if (CursorSpeed < MinSpeed || CursorSpeed > MaxSpeed)
                CursorSpeed = Bonfire.Instance.Status.CustomCursorSpeed = BaseSpeed;

            Position.x += x * CursorSpeed * Time.unscaledDeltaTime;
            Position.y -= y * CursorSpeed * Time.unscaledDeltaTime;

            Position.x = Mathf.Clamp(Position.x, 0, BonfireGUI.Width);
            Position.y = Mathf.Clamp(Position.y, 0, BonfireGUI.Height);

            Bonfire.Log.LogDebug("Cursor update finished");
        }

        // custom components to match GUILayout's matching component behavior.
        // Because certain components calculate only their own position, each of these must be added together to
        // reflect the actual top level position where cursor lives.
        public static void BeginArea(Rect rect)
        {
            _offsetStack.Push(_currentOffset);
            _currentOffset += rect.position;

            GUILayout.BeginArea(rect);
        }

        public static void EndArea()
        {
            GUILayout.EndArea();

            _currentOffset = _offsetStack.Pop();
        }

        // -- unused, previous popup window logic uses now GUI.Box + BeginGroup/EndGroup
        public static Rect Window(
            int id,
            Rect rect,
            GUI.WindowFunction func,
            string title)
        {
            return GUI.Window(id, rect, windowId =>
            {
                _offsetStack.Push(_currentOffset);
                _currentOffset += rect.position;

                try
                {
                    func(windowId);
                }
                finally
                {
                    _currentOffset = _offsetStack.Pop();
                }
            }, title);
        }

        public static void BeginGroup(Rect rect)
        {
            _offsetStack.Push(_currentOffset);
            _currentOffset += rect.position;

            GUI.BeginGroup(rect);
        }

        public static void EndGroup()
        {
            GUI.EndGroup();

            _currentOffset = _offsetStack.Pop();
        }

        public static Vector2 BeginScrollView(Vector2 scrollPos, params GUILayoutOption[] options)
        {
            // scrollbar width and length offset:
            // Width: scrollbar width + some buffer
            // Height: in bench menu, scrollbar covers a bit over 3 button heights, buttons have h = 30f -> 90f + buffer
            // Following values have been found by testing click areas: tricky part is make it work for both overview
            // info scroll and bench teleport scroll menu.
            float scrollX = 28f;
            float scrollY = 99f;

            Vector2 offSet = new Vector2(scrollPos.x - scrollX, scrollPos.y - scrollY);

            _offsetStack.Push(_currentOffset);
            _currentOffset -= offSet; // scrolling down moves content up, have to subtract

            var device = GameManager.instance.inputHandler.inputActions.ActiveDevice;

            if (device.LeftTrigger.IsPressed)
                scrollPos.y -= CursorSpeed * Time.unscaledDeltaTime;

            if (device.RightTrigger.IsPressed)
                scrollPos.y += CursorSpeed * Time.unscaledDeltaTime;

            Vector2 newScrollPos = GUILayout.BeginScrollView(scrollPos, options);
            return newScrollPos;
        }

        public static void EndScrollView()
        {
            try
            {
                GUILayout.EndScrollView();
            }
            finally
            {
                _currentOffset = _offsetStack.Pop();
            }
        }

        public static bool Toggle(bool value, string text, params GUILayoutOption[] options)
        {
            _offsetStack.Push(_currentOffset);

            try
            {
                return CustomToggle(value, text, options);
            }
            finally
            {
                _currentOffset = _offsetStack.Pop();
            }
        }

        public static bool Button(string text, params GUILayoutOption[] options)
        {
            return CustomButton(GUILayout.Button(text, options));
        }

        public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            return CustomButton(GUILayout.Button(text, style, options));
        }


        private static readonly Stack<Vector2> _offsetStack = new Stack<Vector2>();
        private static Vector2 _currentOffset;

        // GUILayout.Button wrapper
        private static bool CustomButton(bool clicked)
        {
            if (GameManager.instance.inputHandler.gameController.IsAttached &&
                IsCursorPressed())
            {
                Bonfire.Log.LogDebug("[CustomCursor] Pressed a GUI button");
                clicked = true;
            }

            return clicked;
        }

        public static bool CustomToggle(bool value, string text, params GUILayoutOption[] options)
        {
            bool newValue = GUILayout.Toggle(value, text, options);

            if (GameManager.instance.inputHandler.gameController.IsAttached &&
                IsCursorPressed())
            {
                Bonfire.Log.LogDebug("[CustomCursor] Pressed a GUI toggle");
                newValue = !value;
            }

            return newValue;
        }

        // simulates cursor click by adding offsets to Rect then checking whether cursor position lands inside it.
        private static bool IsCursorPressed()
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.position += _currentOffset;

            var inputs = GameManager.instance.inputHandler.inputActions;

            return rect.Contains(Position) &&
                   (inputs.Jump.WasPressed ||
                    inputs.Attack.WasPressed);
        }

        // disable attack/jump bench leave behavior so they can be used for clicking GUI buttons on GamePad devices.
        [HarmonyPatch(typeof(ListenForAttack), "OnUpdate")]
        class ListenForAttack_OnUpdate
        {
            static bool Prefix()
            {
                // prevent inputs only for controllers
                var input = GameManager.instance?.inputHandler;
                if (input == null || !input.gameController.IsAttached) return true;

                if (PlayerData.instance.atBench) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForAttackV2), "OnUpdate")]
        class ListenForAttack_OnUpdateV2
        {
            static bool Prefix()
            {
                var input = GameManager.instance?.inputHandler;
                if (input == null || !input.gameController.IsAttached) return true;

                if (PlayerData.instance.atBench) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJump), "OnUpdate")]
        class ListenForJump_OnUpdate
        {
            static bool Prefix()
            {
                var input = GameManager.instance?.inputHandler;
                if (input == null || !input.gameController.IsAttached) return true;

                if (PlayerData.instance.atBench) return false;

                return true;
            }
        }

        [HarmonyPatch(typeof(ListenForJumpV2), "OnUpdate")]
        class ListenForJump_OnUpdateV2
        {
            static bool Prefix()
            {
                var input = GameManager.instance?.inputHandler;
                if (input == null || !input.gameController.IsAttached) return true;

                if (PlayerData.instance.atBench) return false;

                return true;
            }
        }
    }
}