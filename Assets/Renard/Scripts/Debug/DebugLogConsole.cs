using System;
using System.Text;
using UnityEngine;
using UniRx;
using TMPro;

namespace Renard.Debuger
{
    /*
     * 自分自身のログが出されないように
     * MonoBehaviourCustomは使わない！
     */

    /// <summary>※Renard拡張機能</summary>
    [Serializable]
    public class DebugLogConsole : MonoBehaviour
    {
        [SerializeField] protected TMP_Text logUI = default;
        [SerializeField] protected int maxLength = 1000;

        protected StringBuilder stringBuilder { get; private set; } = new StringBuilder();

        private void Start()
        {
            DebugLogger.OutputLogSubject
                .Subscribe(CatchDebugLog)
                .AddTo(this);

            stringBuilder.Length = 0;
        }

        private void Update()
        {
            if (logUI != null && logUI.text != stringBuilder.ToString())
                logUI.text = stringBuilder.ToString();
        }

        private void CatchDebugLog(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (stringBuilder.Length + message.Length > maxLength)
                stringBuilder.Remove(0, message.Length);

            stringBuilder.AppendLine(message);
        }
    }
}
