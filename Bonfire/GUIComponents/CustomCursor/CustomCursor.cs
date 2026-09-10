using System.Collections.Generic;

using UnityEngine;

namespace BonfireSilksong.GUIComponents
{
    /// <summary>
    /// Custom cursor for gamepad devices.
    /// </summary>
    public static class CustomCursor
    {
        public const int BaseSpeed = 400;
        public const int MinSpeed = 50;
        public const int MaxSpeed = 1000;
        public const int CursorSize = 20;

        public static Vector2 Position = new Vector2(BonfireGUI.Width / 2f, BonfireGUI.Height / 2f);
        public static int CursorSpeed = 400;

        public static void DrawCustomCursor()
        {
            if (!GameManager.instance.inputHandler.gameController.IsAttached) return;

            if (_cursorTexture == null)
                _cursorTexture = CreateCursorTexture();

            GUI.color = Color.white;
            GUI.DrawTexture(
                new Rect(Position.x - CursorSize, Position.y - CursorSize, 2 * CursorSize, 2 * CursorSize),
                _cursorTexture);
        }

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
            var device = GameManager.instance.inputHandler.inputActions.ActiveDevice;

            if (device.LeftTrigger.IsPressed)
                scrollPos.y -= CursorSpeed * Time.unscaledDeltaTime;

            if (device.RightTrigger.IsPressed)
                scrollPos.y += CursorSpeed * Time.unscaledDeltaTime;

            Vector2 newScrollPos = GUILayout.BeginScrollView(scrollPos, options);

            // adding an invisible marker at the start of scroll wheel helps with detecting its start position
            GUILayout.Label(GUIContent.none, GUILayout.Width(0), GUILayout.Height(0));
            Rect marker = GUILayoutUtility.GetLastRect();

            // offset: scroll shift content by -scrollPos (scrolling down moves content up) from start position
            Vector2 offSet = marker.position - scrollPos;

            _offsetStack.Push(_currentOffset);
            _currentOffset += offSet;

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

        /// <summary>
        /// Custom variant for BeginScrollView, use this for teleport bench menu. End component is still the
        /// CustomCursor.EndScrollView.
        /// </summary>
        public static Vector2 BeginScrollViewTeleportMenu(Vector2 scrollPos, params GUILayoutOption[] options)
        {
            // scrollbar width and length offset:
            // Width: scrollbar width + some buffer
            // Height: in bench menu, scrollbar covers a bit over 3 button heights, buttons have h = 30f -> 90f + buffer
            // Following values have been found by testing click areas.
            float scrollX = 28f;
            float scrollY = 99f;

            var device = GameManager.instance.inputHandler.inputActions.ActiveDevice;

            if (device.LeftTrigger.IsPressed)
                scrollPos.y -= CursorSpeed * Time.unscaledDeltaTime;

            if (device.RightTrigger.IsPressed)
                scrollPos.y += CursorSpeed * Time.unscaledDeltaTime;

            Vector2 newScrollPos = GUILayout.BeginScrollView(scrollPos, options);

            Vector2 offSet = new Vector2(scrollX - scrollPos.x, scrollY - scrollPos.y);

            _offsetStack.Push(_currentOffset);
            _currentOffset += offSet;

            return newScrollPos;
        }

        public static bool Toggle(bool value, string text, params GUILayoutOption[] options)
        {
            return CustomToggle(GUILayout.Toggle(value, text, options));
        }

        public static bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options)
        {
            return CustomToggle(GUILayout.Toggle(value, text, style, options));
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

        public static bool CustomToggle(bool clicked)
        {
            bool newValue = clicked;

            _offsetStack.Push(_currentOffset);

            try
            {
                if (GameManager.instance.inputHandler.gameController.IsAttached &&
                    IsCursorPressed())
                {
                    Bonfire.Log.LogDebug("[CustomCursor] Pressed a GUI toggle");
                    newValue = !newValue;
                }

                return newValue;
            }
            finally
            {
                _currentOffset = _offsetStack.Pop();
            }
        }


        private const int CursorTextureSize = 16;

        private static Texture2D _cursorTexture;

        // draws a white disc with black center
        private static Texture2D CreateCursorTexture()
        {
            Texture2D tex = new Texture2D(CursorTextureSize, CursorTextureSize, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Color32[] pixels = new Color32[CursorTextureSize * CursorTextureSize];
            Color32 clear = new Color32(0, 0, 0, 0);
            Vector2 center = new Vector2(7.5f, 7.5f);

            for (int y = 0; y < CursorTextureSize; y++)
            {
                for (int x = 0; x < CursorTextureSize; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    Color32 c;

                    if (d <= 1.5f)
                        c = Color.black;
                    else if (d <= 4.0f)
                        c = Color.white;
                    else
                        c = clear;

                    pixels[y * CursorTextureSize + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return tex;
        }

        // simulates cursor click by adding offsets to Rect then checking whether cursor position lands inside it.
        private static bool IsCursorPressed()
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            rect.position += _currentOffset;

            var inputs = GameManager.instance.inputHandler.inputActions;

            return rect.Contains(Position) && (inputs.Jump.WasPressed || inputs.Attack.WasPressed);
        }
    }
}