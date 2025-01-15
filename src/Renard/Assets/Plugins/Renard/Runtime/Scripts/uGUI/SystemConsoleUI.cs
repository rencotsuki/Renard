using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

public static class SystemConsoleHandler
{
    public const string DefaultLabelDone = "Done";
    public const string DefaultLabelCancel = "Cancel";

    public static readonly Color DefaultColorFrame = new Color(24f / 255f, 24f / 255f, 24f / 255f, 1f);
    public static readonly Color DefaultColorBorder = Color.white;
    public static readonly Color DefaultColorText = Color.white;
    public static readonly Color DefaultColorLabel = Color.white;

    public static Renard.SystemConsoleUI SystemWindow => Renard.SystemConsoleUI.Singleton;
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
        [SerializeField] protected Image systemWindowFrame = default;
        [SerializeField] protected Image systemWindowBorder = default;
        [SerializeField] protected TMP_Text systemWindowTitle = default;
        [SerializeField] protected TMP_Text systemWindowMessage = default;
        [SerializeField] protected Button systemWindowDoneButton = default;
        [SerializeField] protected TMP_Text systemWindowDoneLabel = default;
        [SerializeField] protected Button systemWindowCancelButton = default;
        [SerializeField] protected TMP_Text systemWindowCancelLabel = default;

        private Vector2 referenceResolutionSize = new Vector2(750f, 1334f);

        protected override void Initialized()
        {
            base.Initialized();

            DontDestroyOnLoad(this);

            if (_canvasScaler != null)
            {
                if (_canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    if (Screen.width >= Screen.height)
                    {
                        referenceResolutionSize.x = _canvasScaler.referenceResolution.y;
                        referenceResolutionSize.y = _canvasScaler.referenceResolution.x;
                    }
                    else
                    {
                        referenceResolutionSize = _canvasScaler.referenceResolution;
                    }

                    _canvasScaler.referenceResolution = referenceResolutionSize;
                    _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                }
            }

            SetupSystemWindow();
        }

        #region SystemWindow

        [HideInInspector] private bool _isOpenSystemWindow = false;

        /// <summary></summary>
        public bool IsOpenWindow
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

        private bool _doneClose = false;
        private Action _systemWindowDoneAction = null;
        private Action _systemWindowCancelAction = null;
        private Action _systemWindowCloseAction = null;

        private void SetupSystemWindow()
        {
            if (systemWindowFrameOutButton != null)
                systemWindowFrameOutButton.onClick.AddListener(OnClickSystemWindowCancel);

            if (systemWindowDoneButton != null)
                systemWindowDoneButton.onClick.AddListener(OnClickSystemWindowDone);

            if (systemWindowCancelButton != null)
                systemWindowCancelButton.onClick.AddListener(OnClickSystemWindowCancel);

            Close();
        }

        private void OnClickSystemWindowDone()
        {
            if (_systemWindowDoneAction != null)
                _systemWindowDoneAction();

            if (_doneClose)
                Close();
        }

        private void OnClickSystemWindowCancel()
        {
            if (_systemWindowCancelAction != null)
                _systemWindowCancelAction();

            Close();
        }

        private void ClearWindowOption()
        {
            _doneClose = false;

            _systemWindowDoneAction = null;

            if (systemWindowDoneButton != null)
                systemWindowDoneButton.gameObject.SetActive(false);

            _systemWindowCancelAction = null;

            if (systemWindowCancelButton != null)
                systemWindowCancelButton.gameObject.SetActive(false);

            _systemWindowCloseAction = null;

            SetWindowColor(SystemConsoleHandler.DefaultColorFrame,
                           SystemConsoleHandler.DefaultColorBorder,
                           SystemConsoleHandler.DefaultColorText);
        }

        /// <summary>ウィンドウを閉じる</summary>
        public void Close()
        {
            if (_systemWindowCloseAction != null)
                _systemWindowCloseAction();

            IsOpenWindow = false;
            ClearWindowOption();
        }

        /// <summary>ウィンドウを開く</summary>
        public void Show()
        {
            IsOpenWindow = true;
        }

        /// <summary>メッセージ設定</summary>
        public SystemConsoleUI SetMessage(string title, string message, bool frameOutButton = false)
        {
            if (systemWindowTitle != null)
                systemWindowTitle.text = title;

            if (systemWindowMessage != null)
                systemWindowMessage.text = message;

            if (systemWindowFrameOutButton != null)
                systemWindowFrameOutButton.interactable = frameOutButton;

            return this;
        }

        /// <summary>確定操作</summary>
        public SystemConsoleUI OnActionDone(Action onAction, string label = SystemConsoleHandler.DefaultLabelDone, bool actionClose = true)
            => OnActionDone(onAction, label, SystemConsoleHandler.DefaultColorLabel, actionClose);
        /// <summary>確定操作</summary>
        public SystemConsoleUI OnActionDone(Action onAction, string label, Color labelColor, bool actionClose)
        {
            _systemWindowDoneAction = onAction;

            if (systemWindowDoneButton != null && !systemWindowDoneButton.gameObject.activeSelf)
                systemWindowDoneButton.gameObject.SetActive(true);

            if (systemWindowDoneLabel != null)
            {
                systemWindowDoneLabel.text = label;
                systemWindowDoneLabel.color = labelColor;
            }

            _doneClose = actionClose;

            return this;
        }

        /// <summary>不確定操作</summary>
        public SystemConsoleUI OnActionCancel(Action onAction, string label = SystemConsoleHandler.DefaultLabelCancel)
            => OnActionCancel(onAction, label, SystemConsoleHandler.DefaultColorLabel);
        /// <summary>不確定操作</summary>
        public SystemConsoleUI OnActionCancel(Action onAction, string label, Color labelColor)
        {
            _systemWindowCancelAction = onAction;

            if (systemWindowCancelButton != null && systemWindowCancelButton.gameObject.activeSelf != !string.IsNullOrEmpty(label))
                systemWindowCancelButton.gameObject.SetActive(!string.IsNullOrEmpty(label));

            if (systemWindowCancelLabel != null)
            {
                systemWindowCancelLabel.text = label;
                systemWindowCancelLabel.color = labelColor;
            }

            return this;
        }

        /// <summary>閉じる操作</summary>
        public SystemConsoleUI OnActionClose(Action onAction)
        {
            _systemWindowCloseAction = onAction;
            return this;
        }

        /// <summary>色設定</summary>
        public SystemConsoleUI SetWindowColor(Color textColor)
            => SetWindowColor(SystemConsoleHandler.DefaultColorFrame, SystemConsoleHandler.DefaultColorBorder, textColor);
        /// <summary>色設定</summary>
        public SystemConsoleUI SetWindowColor(Color frame, Color border)
            => SetWindowColor(frame, border, SystemConsoleHandler.DefaultColorText);
        /// <summary>色設定</summary>
        public SystemConsoleUI SetWindowColor(Color frame, Color border, Color textColor)
        {
            if (systemWindowFrame != null)
                systemWindowFrame.color = frame;

            if (systemWindowBorder != null)
                systemWindowBorder.color = border;

            if (systemWindowTitle != null)
                systemWindowTitle.color = textColor;

            if (systemWindowMessage != null)
                systemWindowMessage.color = textColor;

            return this;
        }

        #endregion
    }
}
