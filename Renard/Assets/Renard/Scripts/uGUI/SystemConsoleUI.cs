using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

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
        [SerializeField] protected CanvasGroup canvasGroupSystemWindow = default;

        [HideInInspector] private bool _openSystemWindow = false;
        protected bool openSystemWindow
        {
            get => _openSystemWindow;
            set
            {
                _openSystemWindow = value;

                if (canvasGroupSystemWindow != null)
                {
                    canvasGroupSystemWindow.alpha = _openSystemWindow ? 1f : 0f;
                    canvasGroupSystemWindow.blocksRaycasts = _openSystemWindow;
                }
            }
        }

        protected override void Initialized()
        {
            base.Initialized();

            DontDestroyOnLoad(this);

            openSystemWindow = false;
        }
    }
}
