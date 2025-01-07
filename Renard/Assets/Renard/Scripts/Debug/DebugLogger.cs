using System;
using System.Text;
using UnityEngine;
using UniRx;

public enum LogMode
{
    All,
    JustErrors,
};

public enum DebugerLogType
{
    Info,
    Warning,
    Error,
}

namespace Renard.Debuger
{
    public class DebugLogger
    {
#if UNITY_EDITOR
        private static LogMode _logMode => IsLogModeAll ? LogMode.All : LogMode.JustErrors;
#else
        private static LogMode _logMode => LogMode.JustErrors;
#endif

        public static Subject<string> OutputLogSubject { get; private set; } = new Subject<string>();

        private static StringBuilder _stringBuilder = new StringBuilder();

        public static void Log(Type classType, DebugerLogType logType, string methodName, string message)
        {
            if (string.IsNullOrEmpty(methodName) && string.IsNullOrEmpty(message))
                return;

            _stringBuilder.Length = 0;

            _stringBuilder.Append("[");
            _stringBuilder.Append(classType != null ? classType.Name : "class null");
            _stringBuilder.Append("]");
            _stringBuilder.Append(" - ");
            _stringBuilder.Append(logType == DebugerLogType.Error ? "(<color=red>" : "(<color=green>");
            _stringBuilder.Append(string.IsNullOrEmpty(methodName) ? "method null" : methodName);
            _stringBuilder.Append("</color>)");
            _stringBuilder.Append(": ");
            _stringBuilder.Append(message);

            if (logType == DebugerLogType.Error)
            {
                Debug.LogError(_stringBuilder.ToString());
            }
            else if (_logMode == LogMode.All)
            {
                Debug.Log(_stringBuilder.ToString());
            }

            OutputLogSubject?.OnNext(_stringBuilder.ToString());
        }

#if UNITY_EDITOR

        private static int _isLogModeAll = -1;
        private const string _debugLoggerMode = "DebugLoggerMode";

        protected static bool IsLogModeAll
        {
            get
            {
                if (_isLogModeAll == -1)
                    _isLogModeAll = UnityEditor.EditorPrefs.GetBool(_debugLoggerMode, true) ? 1 : 0;
                return _isLogModeAll != 0;
            }
            set
            {
                int newValue = value ? 1 : 0;
                if (newValue != _isLogModeAll)
                {
                    _isLogModeAll = newValue;
                    UnityEditor.EditorPrefs.SetBool(_debugLoggerMode, value);
                }
            }
        }

        [UnityEditor.MenuItem("Renard/LogMode/All", false, 2)]
        [UnityEditor.MenuItem("Renard/LogMode/JustErrors", false, 2)]
        public static void ToggleLogMode()
        {
            IsLogModeAll = !IsLogModeAll;
        }

        [UnityEditor.MenuItem("Renard/LogMode/All", true, 2)]
        [UnityEditor.MenuItem("Renard/LogMode/JustErrors", true, 2)]
        public static bool ToggleLogModeValidate()
        {
            UnityEditor.Menu.SetChecked("Renard/LogMode/All", IsLogModeAll);
            UnityEditor.Menu.SetChecked("Renard/LogMode/JustErrors", !IsLogModeAll);
            return true;
        }

#endif
    }
}
