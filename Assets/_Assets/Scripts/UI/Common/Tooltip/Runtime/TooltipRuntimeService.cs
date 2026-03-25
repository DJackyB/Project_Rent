using BaoZuPo.UI.Common.Tooltip;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BaoZuPo.UI.Common.Tooltip.Runtime
{
    public sealed class TooltipRuntimeService : MonoBehaviour, ITooltipService
    {
        private static TooltipRuntimeService _instance;

        private Canvas _currentCanvas;
        private RectTransform _currentCanvasRect;
        private RectTransform _currentLayerRoot;
        private ITooltipPresenter _currentPresenter;
        private TooltipRequest _currentRequest;
        private object _currentOwner;
        private Vector2 _lastPointerPosition;

        public bool IsAvailable => true;

        public static TooltipRuntimeService EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            var existing = FindFirstObjectByType<TooltipRuntimeService>();
            if (existing != null)
            {
                return existing;
            }

            var serviceObject = new GameObject("TooltipRuntimeService");
            DontDestroyOnLoad(serviceObject);
            return serviceObject.AddComponent<TooltipRuntimeService>();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            TooltipServices.SetCurrent(this);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                TooltipServices.ResetCurrent(this);
                _instance = null;
            }
        }

        private void LateUpdate()
        {
            if (_currentPresenter == null || _currentPresenter.Root == null || _currentRequest == null)
            {
                return;
            }

            if (_currentRequest.Anchor == null || !_currentRequest.Anchor.gameObject.activeInHierarchy)
            {
                HideAll();
                return;
            }

            if (_currentRequest.PlacementMode == TooltipPlacementMode.FollowPointer)
            {
                _lastPointerPosition = GetPointerPosition(_lastPointerPosition);
            }

            PositionCurrentTooltip();
            _currentPresenter.Root.SetAsLastSibling();
        }

        public void Show(TooltipRequest request, Vector2? pointerPosition = null)
        {
            if (request == null || request.Content == null || request.Anchor == null || !request.Anchor.gameObject.activeInHierarchy)
            {
                return;
            }

            var canvas = request.Anchor.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (!EnsureLayerRoot(canvas))
            {
                return;
            }

            if (!TooltipPresenterRegistry.TryCreatePresenter(request.Content, _currentLayerRoot, out var presenter))
            {
                HideAll();
                return;
            }

            HideAll();

            _currentRequest = request;
            _currentOwner = request.Owner;
            _currentPresenter = presenter;
            _lastPointerPosition = pointerPosition ?? GetPointerPosition(_lastPointerPosition);

            _currentPresenter.Show(request);
            PositionCurrentTooltip();
        }

        public void Hide(object owner)
        {
            if (owner != null && ReferenceEquals(owner, _currentOwner))
            {
                HideAll();
            }
        }

        public void HideAll()
        {
            _currentRequest = null;
            _currentOwner = null;

            if (_currentPresenter == null)
            {
                return;
            }

            _currentPresenter.Hide();
            if (_currentPresenter is Component component && component != null)
            {
                Destroy(component.gameObject);
            }

            _currentPresenter = null;
        }

        private bool EnsureLayerRoot(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            if (_currentCanvas == canvas && _currentLayerRoot != null)
            {
                _currentCanvasRect = canvas.transform as RectTransform;
                return _currentCanvasRect != null;
            }

            if (_currentLayerRoot != null)
            {
                Destroy(_currentLayerRoot.gameObject);
                _currentLayerRoot = null;
            }

            _currentCanvas = canvas;
            _currentCanvasRect = canvas.transform as RectTransform;
            if (_currentCanvasRect == null)
            {
                return false;
            }

            var layerObject = new GameObject("TooltipRuntimeRoot", typeof(RectTransform), typeof(CanvasGroup));
            layerObject.transform.SetParent(canvas.transform, false);
            _currentLayerRoot = layerObject.transform as RectTransform;
            _currentLayerRoot.anchorMin = Vector2.zero;
            _currentLayerRoot.anchorMax = Vector2.one;
            _currentLayerRoot.offsetMin = Vector2.zero;
            _currentLayerRoot.offsetMax = Vector2.zero;
            _currentLayerRoot.SetAsLastSibling();

            var canvasGroup = layerObject.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return true;
        }

        private void PositionCurrentTooltip()
        {
            if (_currentCanvasRect == null || _currentPresenter == null || _currentPresenter.Root == null || _currentRequest == null)
            {
                return;
            }

            var eventCamera = _currentCanvas != null && _currentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _currentCanvas.worldCamera
                : null;

            _currentPresenter.Root.anchoredPosition = _currentRequest.PlacementMode switch
            {
                TooltipPlacementMode.AnchorRect => TooltipPositioner.CalculateAnchorPosition(
                    _currentCanvasRect,
                    _currentPresenter.Root,
                    _currentRequest.Anchor,
                    _currentRequest.Offset,
                    eventCamera),
                _ => TooltipPositioner.CalculatePointerPosition(
                    _currentCanvasRect,
                    _currentPresenter.Root,
                    _lastPointerPosition,
                    _currentRequest.Offset,
                    eventCamera)
            };
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
