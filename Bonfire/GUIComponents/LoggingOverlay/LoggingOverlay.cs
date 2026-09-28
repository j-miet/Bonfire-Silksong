using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx.Logging;
using UnityEngine;

namespace BonfireSilksong.GUIComponents
{
    public class LoggingOverlay : ILogListener
    {
        public const int MaxMessages = 30;
        public const int MaxMessagesExtended = 2000;

        public bool DisplayLogs { get; private set; } = false;
        public bool DisplayExtendedLogs { get; private set; } = false;

        public void SetLogsDisplay()
        {
            DisplayLogs = true;
            DisplayExtendedLogs = false;
        }

        public void SetExtendedLogsDisplay()
        {
            DisplayExtendedLogs = true;
            DisplayLogs = false;
        }

        public void DisableLogsDisplay()
        {
            DisplayLogs = false;
            DisplayExtendedLogs = false;
        }

        // log message loader, attach this to BepInEx logger listener in MonoBehavior.Awake callback
        public void LogEvent(object sender, LogEventArgs eventArgs)
        {
            if (!Bonfire.Instance.Status.DebugMode)
                return;

            // remove this block if general logging is needed
            if (eventArgs.Source == null || eventArgs.Source.SourceName != Bonfire.ModName)
                return;

            if (!(eventArgs.Level.HasFlag(LogLevel.Debug) ||
                eventArgs.Level.HasFlag(LogLevel.Info) ||
                eventArgs.Level.HasFlag(LogLevel.Error)))
            {
                return;
            }

            string message;
            if (DisplayExtendedLogs)
            {
                message = eventArgs.Data?.ToString()?.Replace("\r\n", " ").Replace("\n", " ") ?? "";
            }
            else
            {
                message = eventArgs.Data?.ToString() ?? "";
            }

            lock (_pendingMessages)
            {
                _pendingMessages.Enqueue(message);

                while (_pendingMessages.Count > MaxMessagesExtended)
                    _pendingMessages.Dequeue();
            }
        }

        public void Update()
        {
            lock (_pendingMessages)
            {
                while (_pendingMessages.Count > 0)
                    _messages.Enqueue(_pendingMessages.Dequeue());
            }

            while (_messages.Count > MaxMessagesExtended)
                _messages.Dequeue();

            if (DisplayExtendedLogs)
                return;

            _logBuilder.Clear();

            int skip = Mathf.Max(0, _messages.Count - MaxMessages);
            foreach (string message in _messages.Skip(skip))
                _logBuilder.AppendLine(message);

            _logText = _logBuilder.ToString();
        }

        public void Dispose()
        {
            lock (_pendingMessages)
            {
                _pendingMessages.Clear();
            }

            _messages.Clear();
            _logBuilder.Clear();
            _logText = "";
        }

        public void Display()
        {
            if (!_colorsInitialized)
            {
                _textColor = new Color(1f, 1f, 1f);
                _bgColor = new Color(0.2f, 0.2f, 0.2f);
                _borderColor = new Color(0.5f, 0.5f, 0.5f);

                _colorsInitialized = true;
            }

            float bgXPos = BonfireGUI.Width - Width - BoxOffsetDoubled;
            float xPos = BonfireGUI.Width - Width - BoxOffset;

            GUI.color = _borderColor;
            GUI.DrawTexture(new Rect(bgXPos, 0, Width + BoxOffsetDoubled, Height + BoxOffsetDoubled),
                Texture2D.whiteTexture);

            GUI.color = _bgColor;
            GUI.DrawTexture(new Rect(xPos, BoxOffset, Width, Height), Texture2D.whiteTexture);

            GUI.color = _textColor;
            GUILayout.BeginArea(new Rect(xPos, BoxOffset, Width, Height));

            if (!DisplayExtendedLogs)
            {
                GUILayout.BeginVertical();

                GUILayout.Label($"# Logs --- Displays up to {MaxMessages} Info/Debug/Error messages.");
                GUILayout.Label(_logText);

                GUILayout.EndVertical();
                GUILayout.EndArea();

                return;
            }

            GUILayout.Label($"# Extended Logs --- Displays up to {MaxMessagesExtended} Info/Debug/Error messages.");

            float viewHeight = Height - TitleHeight;
            Rect scrollViewRect = new Rect(0, TitleHeight, Width, viewHeight);

            float contentHeight = _messages.Count * LineHeight;
            float maxScrollY = Mathf.Max(0f, contentHeight - viewHeight);

            Rect contentRect = new Rect(0, 0, Width - ScrollbarWidth, contentHeight);

            if (_followBottom)
                _loggingWindowScrollPos.y = maxScrollY;

            _loggingWindowScrollPos = GUI.BeginScrollView(scrollViewRect, _loggingWindowScrollPos, contentRect);

            int firstVisibleLine = Mathf.Max(0, Mathf.FloorToInt(_loggingWindowScrollPos.y / LineHeight) - 1);
            // +2 accounts for one line above and below the message area
            int visibleLineCount = Mathf.CeilToInt(viewHeight / LineHeight) + 2;
            int lastVisibleLine = Mathf.Min(_messages.Count, firstVisibleLine + visibleLineCount);

            for (int i = firstVisibleLine; i < lastVisibleLine; i++)
            {
                GUI.Label(new Rect(0, i * LineHeight, Width - ScrollbarWidth, LineHeight), _messages.ElementAt(i));
            }

            GUI.EndScrollView();

            _followBottom = _loggingWindowScrollPos.y >= maxScrollY - BottomScrollThreshold;

            GUILayout.EndArea();
        }


        private const float Width = 700f;
        private const float Height = 500f;
        private const float BoxOffset = 5f;
        private const float BoxOffsetDoubled = BoxOffset * 2;
        private const float TitleHeight = 45f;
        private const float LineHeight = 20f;
        private const float ScrollbarWidth = 30f;
        private const float BottomScrollThreshold = 40f;

        private readonly Queue<string> _pendingMessages = new Queue<string>();
        private readonly Queue<string> _messages = new Queue<string>();
        private readonly StringBuilder _logBuilder = new StringBuilder();

        private bool _colorsInitialized = false;
        private Color _textColor;
        private Color _bgColor;
        private Color _borderColor;

        private Vector2 _loggingWindowScrollPos;
        private string _logText = "";
        private bool _followBottom = false;
    }
}