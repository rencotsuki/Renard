using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class SystemConsoleHandler
{
    public const string DefaultLabelDone = "Done";
    public const string DefaultLabelCancel = "Cancel";

    public static readonly Color DefaultColorFrame = new Color(24f / 255f, 24f / 255f, 24f / 255f, 1f);
    public static readonly Color DefaultColorBorder = Color.white;
    public static readonly Color DefaultColorText = Color.white;
    public static readonly Color DefaultColorLabel = Color.white;

    private static Renard.SystemConsoleUI console => Renard.SystemConsoleUI.Singleton;

    public static Renard.SystemWindow SystemWindow => console != null ? console.SystemWindow : null;

    public static Renard.LicenseWindow LicenseWindow => console != null ? console.LicenseWindow : null;
}

namespace Renard
{
    public class SystemConsoleUI : SingletonMonoBehaviourCustom<SystemConsoleUI>
    {
        [Header("Canvas")]
        [SerializeField] private Canvas _canvas = default;
        [SerializeField] private CanvasScaler _canvasScaler = default;
        [SerializeField] private GraphicRaycaster _graphicRaycaster = default;
        [Header("UI")]
        [SerializeField] protected GameObject root = default;
        [SerializeField] protected SystemWindow systemWindow = new SystemWindow();
        [SerializeField] protected LicenseWindow licenseWindow = new LicenseWindow();

        public SystemWindow SystemWindow => systemWindow;
        public LicenseWindow LicenseWindow => licenseWindow;

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

            systemWindow?.Setup();
            licenseWindow?.Setup();
        }
    }

    [Serializable]
    public class SystemWindow
    {
        [SerializeField] protected CanvasGroup canvasGroupWindow = default;
        [SerializeField] protected Button frameOutButton = default;
        [SerializeField] protected Image frame = default;
        [SerializeField] protected Image frameBorder = default;
        [SerializeField] protected TMP_Text txtTitle = default;
        [SerializeField] protected TMP_Text txtMessage = default;
        [SerializeField] protected Button btnDone = default;
        [SerializeField] protected TMP_Text labelDone = default;
        [SerializeField] protected Button btnCancel = default;
        [SerializeField] protected TMP_Text labelCancel = default;

        [Serializable]
        protected struct SystemWindowContext
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
            public bool DoneActionClose;

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
                DoneActionClose = false;

                CancelButton = false;
                CancelAction = null;
                CancelLabel = SystemConsoleHandler.DefaultLabelCancel;
                CancelLabelColor = SystemConsoleHandler.DefaultColorLabel;
            }
        }

        private SystemWindowContext windowContext = new SystemWindowContext();

        private bool _isOpenWindow = false;
        /// <summary></summary>
        public bool IsOpenWindow
        {
            get => _isOpenWindow;
            protected set
            {
                _isOpenWindow = value;

                if (canvasGroupWindow != null)
                {
                    canvasGroupWindow.alpha = _isOpenWindow ? 1f : 0f;
                    canvasGroupWindow.blocksRaycasts = _isOpenWindow;
                }
            }
        }

        private bool _doneClose = false;
        private Action _doneAction = null;
        private Action _cancelAction = null;
        private Action _closeAction = null;

        public void Setup()
        {
            windowContext.Clear();

            if (frameOutButton != null)
                frameOutButton.onClick.AddListener(OnClickCancel);

            if (btnDone != null)
                btnDone.onClick.AddListener(OnClickDone);

            if (btnCancel != null)
                btnCancel.onClick.AddListener(OnClickCancel);

            Close();
        }

        private void OnClickDone()
        {
            if (_doneAction != null)
                _doneAction();

            if (_doneClose)
                Close();
        }

        private void OnClickCancel()
        {
            if (_cancelAction != null)
                _cancelAction();

            Close();
        }

        private bool SetWindowContext()
        {
            try
            {
                txtTitle.text = windowContext.Title;
                txtMessage.text = windowContext.Message;

                frame.color = windowContext.FrameColor;
                frameBorder.color = windowContext.BorderColor;
                txtTitle.color = windowContext.TextColor;
                txtMessage.color = windowContext.TextColor;

                _doneAction = windowContext.DoneAction;
                _cancelAction = windowContext.CancelAction;
                _closeAction = windowContext.CloseAction;

                _doneClose = windowContext.DoneActionClose;

                frameOutButton.interactable = windowContext.FrameOutButton;

                btnDone.gameObject.SetActive(windowContext.DoneButton);
                labelDone.text = windowContext.DoneLabel;
                labelDone.color = windowContext.DoneLabelColor;

                btnCancel.gameObject.SetActive(windowContext.CancelButton);
                labelCancel.text = windowContext.CancelLabel;
                labelCancel.color = windowContext.CancelLabelColor;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ClearActions()
        {
            _doneAction = null;
            _cancelAction = null;
            _closeAction = null;
        }

        /// <summary>ウィンドウを閉じる</summary>
        public void Close()
        {
            if (_closeAction != null)
                _closeAction();

            ClearActions();
            IsOpenWindow = false;
        }

        /// <summary>ウィンドウを開く</summary>
        public void Show()
        {
            if (!SetWindowContext())
            {
                // 問題があったら開かない
                IsOpenWindow = false;
                return;
            }

            IsOpenWindow = true;

            // 開いたら情報をクリア
            windowContext.Clear();
        }

        /// <summary>メッセージ設定</summary>
        public SystemWindow SetMessage(string title, string message, bool frameOutButton = false)
        {
            windowContext.Title = title;
            windowContext.Message = message;
            windowContext.FrameOutButton = frameOutButton;
            return this;
        }

        /// <summary>確定操作</summary>
        public SystemWindow OnActionDone(Action onAction, string label = SystemConsoleHandler.DefaultLabelDone, bool actionClose = true)
            => OnActionDone(onAction, label, SystemConsoleHandler.DefaultColorLabel, actionClose);
        /// <summary>確定操作</summary>
        public SystemWindow OnActionDone(Action onAction, string label, Color labelColor, bool actionClose)
        {
            windowContext.DoneButton = true;
            windowContext.DoneAction = onAction;
            windowContext.DoneLabel = label;
            windowContext.DoneLabelColor = labelColor;
            windowContext.DoneActionClose = actionClose;
            return this;
        }

        /// <summary>不確定操作</summary>
        public SystemWindow OnActionCancel(Action onAction, string label = SystemConsoleHandler.DefaultLabelCancel)
            => OnActionCancel(onAction, label, SystemConsoleHandler.DefaultColorLabel);
        /// <summary>不確定操作</summary>
        public SystemWindow OnActionCancel(Action onAction, string label, Color labelColor)
        {
            windowContext.CancelButton = true;
            windowContext.CancelAction = onAction;
            windowContext.CancelLabel = label;
            windowContext.CancelLabelColor = labelColor;
            return this;
        }

        /// <summary>閉じる操作</summary>
        public SystemWindow OnActionClose(Action onAction)
        {
            windowContext.CloseAction = onAction;
            return this;
        }

        /// <summary>テキスト色設定</summary>
        public SystemWindow SetTextColor(Color textColor)
            => SetColor(SystemConsoleHandler.DefaultColorFrame, SystemConsoleHandler.DefaultColorBorder, textColor);
        /// <summary>フレーム色設定</summary>
        public SystemWindow SetFrameColor(Color frame, Color border)
            => SetColor(frame, border, SystemConsoleHandler.DefaultColorText);
        /// <summary>色設定</summary>
        public SystemWindow SetColor(Color frame, Color border, Color textColor)
        {
            windowContext.FrameColor = frame;
            windowContext.BorderColor = border;
            windowContext.TextColor = textColor;
            return this;
        }
    }

    [Serializable]
    public class LicenseWindow
    {
        [SerializeField] protected CanvasGroup canvasGroupWindow = default;
        [SerializeField] protected Button frameOutButton = default;
        [SerializeField] protected Image frame = default;
        [SerializeField] protected Image frameBorder = default;
        [SerializeField] protected TMP_Text txtTitle = default;
        [SerializeField] protected TMP_Text txtMessage = default;
        [SerializeField] protected TMP_Text txtMessageSub = default;
        [SerializeField] protected RawImage imgView = default;
        [SerializeField] protected Button btnMain = default;
        [SerializeField] protected TMP_Text labelMain = default;
        [SerializeField] protected Button btnSub = default;
        [SerializeField] protected TMP_Text labelSub = default;

        [Serializable]
        protected struct LicenseWindowContext
        {
            public string Title;
            public string Message;
            public Texture ViewImage;
            public bool ViewImageCamera;
            public Color FrameColor;
            public Color BorderColor;
            public Color TextColor;
            public Action CloseAction;
            public bool FrameOutButton;

            public bool MainButton;
            public Action MainAction;
            public string MainLabel;
            public Color MainLabelColor;
            public bool MainActionClose;

            public bool SubButton;
            public Action SubAction;
            public string SubLabel;
            public Color SubLabelColor;
            public bool SubActionClose;

            public void Clear()
            {
                Title = string.Empty;
                Message = string.Empty;
                ViewImage = null;
                ViewImageCamera = false;
                FrameColor = SystemConsoleHandler.DefaultColorFrame;
                BorderColor = SystemConsoleHandler.DefaultColorBorder;
                TextColor = SystemConsoleHandler.DefaultColorText;
                CloseAction = null;
                FrameOutButton = false;

                MainButton = false;
                MainAction = null;
                MainLabel = "Main";
                MainLabelColor = SystemConsoleHandler.DefaultColorLabel;
                MainActionClose = false;

                SubButton = false;
                SubAction = null;
                SubLabel = "Sub";
                SubLabelColor = SystemConsoleHandler.DefaultColorLabel;
                SubActionClose = false;
            }
        }

        private LicenseWindowContext windowContext = new LicenseWindowContext();

        private bool _isOpenWindow = false;
        /// <summary></summary>
        public bool IsOpenWindow
        {
            get => _isOpenWindow;
            protected set
            {
                _isOpenWindow = value;

                if (canvasGroupWindow != null)
                {
                    canvasGroupWindow.alpha = _isOpenWindow ? 1f : 0f;
                    canvasGroupWindow.blocksRaycasts = _isOpenWindow;
                }
            }
        }

        private ScreenOrientation screenOrientation => Screen.orientation;

        private bool _mainClose = false;
        private bool _subClose = false;
        private Action _mainAction = null;
        private Action _subAction = null;
        private Action _closeAction = null;

        public void Setup()
        {
            windowContext.Clear();

            if (frameOutButton != null)
                frameOutButton.onClick.AddListener(Close);

            if (btnMain != null)
                btnMain.onClick.AddListener(OnClickMain);

            if (btnSub != null)
                btnSub.onClick.AddListener(OnClickSub);

            Close();
        }

        private void OnClickMain()
        {
            if (_mainAction != null)
                _mainAction();

            if (_mainClose)
                Close();
        }

        private void OnClickSub()
        {
            if (_subAction != null)
                _subAction();

            if (_subClose)
                Close();
        }

        private bool SetWindowContext()
        {
            try
            {
                txtTitle.text = windowContext.Title;
                txtMessage.text = windowContext.Message;
                txtMessageSub.text = windowContext.Message;

                frame.color = windowContext.FrameColor;
                frameBorder.color = windowContext.BorderColor;
                txtTitle.color = windowContext.TextColor;
                txtMessage.color = windowContext.TextColor;
                txtMessageSub.color = windowContext.TextColor;

                imgView.texture = windowContext.ViewImage;
                imgView.enabled = (imgView.texture != null);

                imgView.rectTransform.localScale = Vector3.one;
                imgView.rectTransform.localRotation = Quaternion.identity;

                // モバイル端末のみ処理
                if (Application.platform == RuntimePlatform.Android ||
                    Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    // 反転対応
                    imgView.rectTransform.localScale = new Vector3(-imgView.rectTransform.localScale.x, imgView.rectTransform.localScale.y, imgView.rectTransform.localScale.z);

                    if (windowContext.ViewImageCamera)
                    {
                        if (screenOrientation == ScreenOrientation.Portrait)
                            imgView.rectTransform.localRotation = Quaternion.AngleAxis(90f, Vector3.forward);

                        if (screenOrientation == ScreenOrientation.PortraitUpsideDown)
                            imgView.rectTransform.localRotation = Quaternion.AngleAxis(-90f, Vector3.forward);
                    }
                }

                txtMessage.enabled = !imgView.enabled;
                txtMessageSub.enabled = imgView.enabled;

                _mainAction = windowContext.MainAction;
                _subAction = windowContext.SubAction;
                _closeAction = windowContext.CloseAction;

                _mainClose = windowContext.MainActionClose;
                _subClose = windowContext.SubActionClose;

                frameOutButton.interactable = windowContext.FrameOutButton;

                btnMain.gameObject.SetActive(windowContext.MainButton);
                labelMain.text = windowContext.MainLabel;
                labelMain.color = windowContext.MainLabelColor;

                btnSub.gameObject.SetActive(windowContext.SubButton);
                labelSub.text = windowContext.SubLabel;
                labelSub.color = windowContext.SubLabelColor;

                // ボタンが１つでも有効になっているか確認する
                return (frameOutButton.interactable ||
                        btnMain.gameObject.activeSelf ||
                        btnSub.gameObject.activeSelf);
            }
            catch
            {
                return false;
            }
        }

        private void ClearActions()
        {
            _mainAction = null;
            _subAction = null;
            _closeAction = null;
        }

        /// <summary>ウィンドウを閉じる</summary>
        public void Close()
        {
            if (_closeAction != null)
                _closeAction();

            ClearActions();
            IsOpenWindow = false;
        }

        /// <summary>ウィンドウを開く</summary>
        public void Show()
        {
            if (!SetWindowContext())
            {
                // 問題があったら開かない
                IsOpenWindow = false;
                return;
            }

            IsOpenWindow = true;

            // 開いたら情報をクリア
            windowContext.Clear();
        }

        /// <summary>メッセージ設定</summary>
        public LicenseWindow SetMessage(string title, string message, bool frameOutButton = false)
            => SetMessage(title, message, null, false, frameOutButton);

        /// <summary>メッセージ設定</summary>
        public LicenseWindow SetMessage(string title, string message, Texture viewImage, bool viewImageCamera = false, bool frameOutButton = false)
        {
            windowContext.Title = title;
            windowContext.Message = message;
            windowContext.ViewImage = viewImage;
            windowContext.ViewImageCamera = viewImageCamera;
            windowContext.FrameOutButton = frameOutButton;
            return this;
        }

        /// <summary>メインボタン操作</summary>
        public LicenseWindow OnActionMain(Action onAction, string label = SystemConsoleHandler.DefaultLabelDone, bool actionClose = true)
            => OnActionMain(onAction, label, SystemConsoleHandler.DefaultColorLabel, actionClose);
        /// <summary>メインボタン操作</summary>
        public LicenseWindow OnActionMain(Action onAction, string label, Color labelColor, bool actionClose)
        {
            windowContext.MainButton = true;
            windowContext.MainAction = onAction;
            windowContext.MainLabel = label;
            windowContext.MainLabelColor = labelColor;
            windowContext.MainActionClose = actionClose;
            return this;
        }

        /// <summary>サブボタン操作</summary>
        public LicenseWindow OnActionSub(Action onAction, string label = SystemConsoleHandler.DefaultLabelCancel, bool actionClose = true)
            => OnActionSub(onAction, label, SystemConsoleHandler.DefaultColorLabel, actionClose);
        /// <summary>サブボタン操作</summary>
        public LicenseWindow OnActionSub(Action onAction, string label, Color labelColor, bool actionClose)
        {
            windowContext.SubButton = true;
            windowContext.SubAction = onAction;
            windowContext.SubLabel = label;
            windowContext.SubLabelColor = labelColor;
            windowContext.SubActionClose = actionClose;
            return this;
        }

        /// <summary>閉じる操作</summary>
        public LicenseWindow OnActionClose(Action onAction)
        {
            windowContext.CloseAction = onAction;
            return this;
        }

        /// <summary>テキスト色設定</summary>
        public LicenseWindow SetTextColor(Color textColor)
            => SetColor(SystemConsoleHandler.DefaultColorFrame, SystemConsoleHandler.DefaultColorBorder, textColor);
        /// <summary>フレーム色設定</summary>
        public LicenseWindow SetFrameColor(Color frame, Color border)
            => SetColor(frame, border, SystemConsoleHandler.DefaultColorText);
        /// <summary>色設定</summary>
        public LicenseWindow SetColor(Color frame, Color border, Color textColor)
        {
            windowContext.FrameColor = frame;
            windowContext.BorderColor = border;
            windowContext.TextColor = textColor;
            return this;
        }
    }
}
