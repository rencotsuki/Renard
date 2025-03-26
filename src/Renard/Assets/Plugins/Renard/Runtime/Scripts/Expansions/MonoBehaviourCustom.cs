using System;
using UnityEngine;
using Renard.Debuger;

/// <summary>※Renard拡張機能</summary>
[Serializable]
public class MonoBehaviourCustom : MonoBehaviour
{
    public bool IsDebugLog = false;

    protected void Log(DebugerLogType logType, string methodName, string message)
    {
        if (!IsDebugLog)
        {
            if (logType == DebugerLogType.Info)
                return;
        }

        DebugLogger.Log(this.GetType(), logType, methodName, message);

        OutputLog($"[{this.GetType()}] - (<color=green>{methodName}</color>): {message}");
    }

    protected virtual void OutputLog(string message)
    {
        // 何もしない
    }
}