using UnityEngine;
using BaoZuPo.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BaoZuPo.UI.Common.Hover
{
    /// <summary>
    /// 悬停预览管理器，控制悬停卡牌预览的显示、隐藏和位置更新。
    /// 单例模式，跟踪鼠标位置并调用预览展示器渲染预览内容。
    /// 支持多个悬停源的快速切换和防重复显示。
    /// </summary>
    public class HoverPreviewController : MonoBehaviour
    {
        private static HoverPreviewController _instance;
        private static bool _loggedMissingInstance;

        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private CardHoverPreviewPresenter presenter;
        [SerializeField] private Vector2 previewOffset = new Vector2(24f, -24f);

        private IHoverPreviewSource _currentSource;
        private bool _loggedMissingConfiguration;
        private Vector2 _lastPointerPosition;

        public static HoverPreviewController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindFirstObjectByType<HoverPreviewController>();
                    if (_instance == null && !_loggedMissingInstance)
                    {
                        _loggedMissingInstance = true;
                        Debug.LogError(
                            "[HoverPreviewController] Missing scene HoverPreviewRoot. " +
                            "Please configure SampleScene instead of relying on runtime bootstrap.");
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolveReferences();
            ValidateConfiguration();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void LateUpdate()
        {
            if (presenter == null || presenter.Root == null || _currentSource == null || canvasRect == null)
            {
                return;
            }

            if (_currentSource.HoverSourceObject == null || !_currentSource.HoverSourceObject.activeInHierarchy)
            {
                Hide();
                return;
            }

            var mousePosition = GetPointerPosition(_lastPointerPosition);
            _lastPointerPosition = mousePosition;
            presenter.Root.anchoredPosition = HoverPreviewPositioner.CalculateClampedPosition(
                canvasRect,
                presenter.Root,
                mousePosition,
                previewOffset);
            presenter.Root.SetAsLastSibling();
        }

        public void Show(IHoverPreviewSource source)
        {
            if (source == null || source.HoverSourceObject == null || !source.HoverSourceObject.activeInHierarchy)
            {
                return;
            }

            ResolveReferences();
            if (!ValidateConfiguration())
            {
                return;
            }

            Show(source, GetPointerPosition(_lastPointerPosition));
        }

        public void Show(IHoverPreviewSource source, Vector2 pointerPosition)
        {
            if (source == null || source.HoverSourceObject == null || !source.HoverSourceObject.activeInHierarchy)
            {
                return;
            }

            ResolveReferences();
            if (!ValidateConfiguration())
            {
                return;
            }

            _currentSource = source;
            _lastPointerPosition = pointerPosition;
            presenter.Show(new HoverPreviewRequest(source, pointerPosition));

            if (presenter.Root != null)
            {
                presenter.Root.anchoredPosition = HoverPreviewPositioner.CalculateClampedPosition(
                    canvasRect,
                    presenter.Root,
                    pointerPosition,
                    previewOffset);
            }
        }

        public void HideIfCurrent(IHoverPreviewSource source)
        {
            if (source != null && _currentSource == source)
            {
                Hide();
            }
        }

        public void Hide()
        {
            _currentSource = null;
            presenter?.Hide();
        }

        private void ResolveReferences()
        {
            if (canvasRect == null)
            {
                var parentCanvas = GetComponentInParent<Canvas>();
                if (parentCanvas != null)
                {
                    canvasRect = parentCanvas.transform as RectTransform;
                }
            }

            if (presenter == null)
            {
                presenter = GetComponentInChildren<CardHoverPreviewPresenter>(true);
            }
        }

        private bool ValidateConfiguration()
        {
            bool valid = true;
            if (canvasRect == null)
            {
                valid = false;
            }

            if (presenter == null)
            {
                valid = false;
            }

            if (!valid && !_loggedMissingConfiguration)
            {
                _loggedMissingConfiguration = true;
                Debug.LogError(
                    "[HoverPreviewController] Missing canvasRect or presenter reference. " +
                    "Please configure HoverPreviewRoot in SampleScene.",
                    this);
            }

            return valid;
        }

        private static Vector2 GetPointerPosition(Vector2 fallback)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.position.ReadValue();
            }

            if (Pointer.current != null)
            {
                return Pointer.current.position.ReadValue();
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#else
            return fallback;
#endif
        }
    }
}
