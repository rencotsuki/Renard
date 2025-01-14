#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0436

using System;
using UnityEngine;
using Renard.Debuger;

/// <summary>※Renard拡張機能</summary>
[Serializable]
public class MonoBehaviourCustom : MonoBehaviour
{
    protected bool isDebugLog = false;

    protected void Log(DebugerLogType logType, string methodName, string message)
    {
        if (!isDebugLog)
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