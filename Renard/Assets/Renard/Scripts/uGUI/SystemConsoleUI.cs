using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public static class SystemConsoleHandler
{
    private static Renard.SystemConsoleUI console => Renard.SystemConsoleUI.Singleton;

    public static bool IsOpenSystemWindow
        => console != null ? console.IsOpenSystemWindow : false;

    public static void CloseSystemWindow()
        => console?.CloseSystemWindow();

    public static void OpenSystemWindow(string title, string message, Action onClose = null)
        => console?.OpenSystemWindow(title, message, null, string.Empty, onClose, string.Empty, true);

    public static void OpenSystemWindow(string title, string message, Action onDone, string doneLabel = "Done", bool frameOutButton = false)
        => console?.OpenSystemWindow(title, message, onDone, doneLabel, null, string.Empty, frameOutButton);

    public static void OpenSystemWindow(string title, string message, Action onDone, Action onCancel, bool frameOutButton = false)
        => console?.OpenSystemWindow(title, message, onDone, "Done", onCancel, "Cancel", frameOutButton);

    public static void OpenSystemWindow(string title, string message, Action onDone, string doneLabel, Action onCancel, string cancelLabel, bool frameOutButton = false)
        => console?.OpenSystemWindow(title, message, onDone, doneLabel, onCancel, cancelLabel, frameOutButton);
}

namespace Renard
{
    public class SystemConsoleUI : SingletonMonoBehaviourCustom<SystemConsoleUI>
    {
        [Header("Canvas")]
        [SerializeField] private Canvas _canvas = default;
        [SerializeField] private CanvasScaler _canvasScaler = default;
        [SerializeField] private GraphicRaycaster _graphicRaycaster = default;
        [Header("EventSystem")]
        [SerializeField] private EventSystem _eventSystem = default;
        [SerializeField] private InputSystemUIInputModule _inputSystemUIInputModule = default;
        [Header("UI")]
        [SerializeField] protected GameObject root = default;
        [Header("SystemWindow")]
        [SerializeField] protected CanvasGroup canvasGroupSystemWindow = default;
        [SerializeField] protected Button systemWindowFrameOutButton = default;
        [SerializeField] protected TMP_Text systemWindowTitle = default;
        [SerializeField] protected TMP_Text systemWindowMessage = default;
        [SerializeField] protected Button systemWindowDoneButton = default;
        [SerializeField] protected TMP_Text systemWindowDoneLabel = default;
        [SerializeField] protected Button systemWindowCancelButton = default;
        [SerializeField] protected TMP_Text systemWindowCancelLabel = default;

        protected override void Initialized()
        {
            base.Initialized();

            DontDestroyOnLoad(this);

            SetupSystemWindow();
        }

        #region SystemWindow

        [HideInInspector] private bool _isOpenSystemWindow = false;
        public bool IsOpenSystemWindow
        {
            get => _isOpenSystemWindow;
            protected set
            {
                _isOpenSystemWindow = value;

                if (canvasGroupSystemWindow != null)
                {
                    canvasGroupSystemWindow.alpha = _isOpenSystemWindow ? 1f : 0f;
                    canvasGroupSystemWindow.blocksRaycasts = _isOpenSystemWindow;
                }
            }
        }

        private Action _systemWindowDoneAction = null;
        private Action _systemWindowCancelAction = null;

        private void SetupSystemWindow()
        {
            if (systemWindowFrameOutButton != null)
                systemWindowFrameOutButton.onClick.AddListener(OnClickSystemWindowCancel);

            if (systemWindowDoneButton != null)
                systemWindowDoneButton.onClick.AddListener(OnClickSystemWindowDone);

            if (systemWindowCancelButton != null)
                systemWindowCancelButton.onClick.AddListener(OnClickSystemWindowCancel);

            CloseSystemWindow();
        }

        public void CloseSystemWindow()
        {
            IsOpenSystemWindow = false;
        }

        private void OnClickSystemWindowDone()
        {
            if (_systemWindowDoneAction != null)
                _systemWindowDoneAction();

            CloseSystemWindow();
        }

        private void OnClickSystemWindowCancel()
        {
            if (_systemWindowCancelAction != null)
                _systemWindowCancelAction();

            CloseSystemWindow();
        }

        public void OpenSystemWindow(string title, string message, Action onDone, string doneLabel, Action onCancel, string cancelLabel, bool frameOutButton)
        {
            if (systemWindowTitle != null)
                systemWindowTitle.text = title;

            if (systemWindowMessage != null)
                systemWindowMessage.text = message;

            _systemWindowDoneAction = onDone;

            if (systemWindowDoneButton != null)
                systemWindowDoneButton.gameObject.SetActive(!string.IsNullOrEmpty(doneLabel));

            if (systemWindowDoneLabel != null)
                systemWindowDoneLabel.text = message;

            _systemWindowCancelAction = onCancel;

            if (systemWindowCancelButton != null)
                systemWindowCancelButton.gameObject.SetActive(!string.IsNullOrEmpty(cancelLabel));

            if (systemWindowCancelLabel != null)
                systemWindowCancelLabel.text = message;

            if (systemWindowFrameOutButton != null)
                systemWindowFrameOutButton.interactable = frameOutButton;

            IsOpenSystemWindow = true;
        }

        #endregion
    }
}
