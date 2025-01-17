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

        [Serializable]
        private struct SystemWindowContext
        {
            public string Title;
            public string Message;
            public Color FrameColor;
            public Color BorderColor;
            public Color TextColor;
            public Action CloseAction;
            public bool FrameOutButton;

            public bool DoneButton;
            public Action DoneAction;
            public string DoneLabel;
            public Color DoneLabelColor;
            public bool DonActionClose;

            public bool CancelButton;
            public Action CancelAction;
            public string CancelLabel;
            public Color CancelLabelColor;

            public void Clear()
            {
                Title = string.Empty;
                Message = string.Empty;
                FrameColor = SystemConsoleHandler.DefaultColorFrame;
                BorderColor = SystemConsoleHandler.DefaultColorBorder;
                TextColor = SystemConsoleHandler.DefaultColorText;
                CloseAction = null;
                FrameOutButton = false;

                DoneButton = false;
                DoneAction = null;
                DoneLabel = SystemConsoleHandler.DefaultLabelDone;
                DoneLabelColor = SystemConsoleHandler.DefaultColorLabel;
                DonActionClose = false;

                CancelButton = false;
                CancelAction = null;
                CancelLabel = SystemConsoleHandler.DefaultLabelCancel;
                CancelLabelColor = SystemConsoleHandler.DefaultColorLabel;
            }
        }

        private SystemWindowContext systemWindowContext = new SystemWindowContext();

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
            systemWindowContext.Clear();

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

        private bool SetSystemWindowContext()
        {
            systemWindowTitle.text = systemWindowContext.Title;
            systemWindowMessage.text = systemWindowContext.Message;

            systemWindowFrame.color = systemWindowContext.FrameColor;
            systemWindowBorder.color = systemWindowContext.BorderColor;
            systemWindowTitle.color = systemWindowContext.TextColor;
            systemWindowMessage.color = systemWindowContext.TextColor;

            _systemWindowDoneAction = systemWindowContext.DoneAction;
            _systemWindowCancelAction = systemWindowContext.CancelAction;
            _systemWindowCloseAction = systemWindowContext.CloseAction;

            _doneClose = systemWindowContext.DonActionClose;

            systemWindowFrameOutButton.interactable = systemWindowContext.FrameOutButton;

            systemWindowDoneButton.gameObject.SetActive(systemWindowContext.DoneButton);
            systemWindowDoneLabel.text = systemWindowContext.DoneLabel;
            systemWindowDoneLabel.color = systemWindowContext.DoneLabelColor;

            systemWindowCancelButton.gameObject.SetActive(systemWindowContext.CancelButton);
            systemWindowCancelLabel.text = systemWindowContext.CancelLabel;
            systemWindowCancelLabel.color = systemWindowContext.CancelLabelColor;

            // ボタンが１つでも有効になっているか確認する
            return (systemWindowFrameOutButton.interactable ||
                    systemWindowDoneButton.gameObject.activeSelf ||
                    systemWindowCancelButton.gameObject.activeSelf);
        }

        private void ClearActions()
        {
            _systemWindowDoneAction = null;
            _systemWindowCancelAction = null;
            _systemWindowCloseAction = null;
        }

        /// <summary>ウィンドウを閉じる</summary>
        public void Close()
        {
            if (_systemWindowCloseAction != null)
                _systemWindowCloseAction();

            ClearActions();
            IsOpenWindow = false;
        }

        /// <summary>ウィンドウを開く</summary>
        public void Show()
        {
            if (!SetSystemWindowContext())
            {
                // 問題があったら開かない
                IsOpenWindow = false;
                return;
            }

            IsOpenWindow = true;

            // 開いたら情報をクリア
            systemWindowContext.Clear();
        }

        /// <summary>メッセージ設定</summary>
        public SystemConsoleUI SetMessage(string title, string message, bool frameOutButton = false)
        {
            systemWindowContext.Title = title;
            systemWindowContext.Message = message;
            systemWindowContext.FrameOutButton = frameOutButton;
            return this;
        }

        /// <summary>確定操作</summary>
        public SystemConsoleUI OnActionDone(Action onAction, string label = SystemConsoleHandler.DefaultLabelDone, bool actionClose = true)
            => OnActionDone(onAction, label, SystemConsoleHandler.DefaultColorLabel, actionClose);
        /// <summary>確定操作</summary>
        public SystemConsoleUI OnActionDone(Action onAction, string label, Color labelColor, bool actionClose)
        {
            systemWindowContext.DoneButton = true;
            systemWindowContext.DoneAction = onAction;
            systemWindowContext.DoneLabel = label;
            systemWindowContext.DoneLabelColor = labelColor;
            systemWindowContext.DonActionClose = actionClose;
            return this;
        }

        /// <summary>不確定操作</summary>
        public SystemConsoleUI OnActionCancel(Action onAction, string label = SystemConsoleHandler.DefaultLabelCancel)
            => OnActionCancel(onAction, label, SystemConsoleHandler.DefaultColorLabel);
        /// <summary>不確定操作</summary>
        public SystemConsoleUI OnActionCancel(Action onAction, string label, Color labelColor)
        {
            systemWindowContext.CancelButton = true;
            systemWindowContext.CancelAction = onAction;
            systemWindowContext.CancelLabel = label;
            systemWindowContext.CancelLabelColor = labelColor;
            return this;
        }

        /// <summary>閉じる操作</summary>
        public SystemConsoleUI OnActionClose(Action onAction)
        {
            systemWindowContext.CloseAction = onAction;
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
            systemWindowContext.FrameColor = frame;
            systemWindowContext.BorderColor = border;
            systemWindowContext.TextColor = textColor;
            return this;
        }

        #endregion
    }
}
