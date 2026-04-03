using Martian.Tooltip;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Martian.Tooltip.Runtime
{
    /// <summary>
    /// 提示框运行时服务。实现 ITooltipService 接口，负责显示和隐藏提示。
    ///
    /// 架构：
    /// - 单例模式：全局只有一个实例，自动在首次访问时创建
    /// - 多 Canvas 支持：自动检测锚点所在 Canvas，并在该 Canvas 下创建提示层
    /// - 动态定位：支持跟随指针或固定在锚点两种模式
    /// - 演示者工厂：使用 TooltipPresenterRegistry 创建具体的演示者实现
    ///
    /// 生命周期：
    /// 1. Show() 被调用
    /// 2. 查找或创建目标 Canvas 和提示层
    /// 3. 通过 TooltipPresenterRegistry 为内容创建演示者
    /// 4. 演示者渲染提示 UI
    /// 5. LateUpdate 更新提示位置（跟随指针或锚点）
    /// 6. Hide() 或 HideAll() 销毁演示者
    /// </summary>
    public sealed class TooltipRuntimeService : MonoBehaviour, ITooltipService
    {
        /// <summary>全局单例实例。</summary>
        private static TooltipRuntimeService _instance;

        /// <summary>当前显示提示的 Canvas。</summary>
        private Canvas _currentCanvas;

        /// <summary>当前 Canvas 的 RectTransform。用于位置计算。</summary>
        private RectTransform _currentCanvasRect;

        /// <summary>当前 Canvas 下的提示层根节点。</summary>
        private RectTransform _currentLayerRoot;

        /// <summary>当前显示的提示的演示者（实际 UI 组件）。</summary>
        private ITooltipPresenter _currentPresenter;

        /// <summary>当前显示的提示请求。</summary>
        private TooltipRequest _currentRequest;

        /// <summary>当前提示的所有者。用于隐藏时的所有者校验。</summary>
        private object _currentOwner;

        /// <summary>最后记录的指针位置（用于跟随指针模式）。</summary>
        private Vector2 _lastPointerPosition;

        /// <summary>提示服务总是可用（一旦实例创建）。</summary>
        public bool IsAvailable => true;

        /// <summary>
        /// 获取或创建全局实例。
        /// 若已存在实例，直接返回。
        /// 否则在场景中寻找，或创建新的 GameObject 并添加组件。
        /// </summary>
        public static TooltipRuntimeService EnsureInstance()
        {
            if (_instance != null)
            {
                return _instance;
            }

            // 尝试在场景中寻找现有实例
            var existing = FindFirstObjectByType<TooltipRuntimeService>();
            if (existing != null)
            {
                _instance = existing;
                TooltipServices.SetCurrent(existing);
                return existing;
            }

            // 创建新的 GameObject 并添加组件
            var serviceObject = new GameObject("TooltipRuntimeService");
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(serviceObject);
            }

            var service = serviceObject.AddComponent<TooltipRuntimeService>();
            _instance = service;
            TooltipServices.SetCurrent(service);
            return service;
        }

        /// <summary>
        /// Awake：单例初始化。
        /// 若已有其他实例，销毁自己。
        /// 否则标记为全局实例。
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                DestroyPresenterObject(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            TooltipServices.SetCurrent(this);
        }

        /// <summary>
        /// OnDestroy：清理单例和提示框。
        /// </summary>
        private void OnDestroy()
        {
            if (_instance == this)
            {
                TooltipServices.ResetCurrent(this);
                _instance = null;
            }
        }

        /// <summary>
        /// LateUpdate：每帧更新提示位置。
        /// - 若提示已隐藏或锚点失活，隐藏提示
        /// - 若跟随指针模式，更新指针位置
        /// - 重新计算和应用提示位置
        /// </summary>
        private void LateUpdate()
        {
            if (_currentPresenter == null || _currentPresenter.Root == null || _currentRequest == null)
            {
                return;
            }

            // 若锚点失活，隐藏提示
            if (_currentRequest.Anchor == null || !_currentRequest.Anchor.gameObject.activeInHierarchy)
            {
                HideAll();
                return;
            }

            // 跟随指针模式则更新指针位置
            if (_currentRequest.PlacementMode == TooltipPlacementMode.FollowPointer)
            {
                _lastPointerPosition = GetPointerPosition(_lastPointerPosition);
            }

            // 重新计算提示位置并渲染
            PositionCurrentTooltip();
            _currentPresenter.Root.SetAsLastSibling();
        }

        /// <summary>
        /// 显示提示框。
        /// 流程：
        /// 1. 验证请求和锚点有效性
        /// 2. 查找或创建提示层
        /// 3. 为内容创建演示者
        /// 4. 隐藏旧提示
        /// 5. 记录新提示的请求和所有者
        /// 6. 调用演示者的 Show()
        /// 7. 计算和应用位置
        /// </summary>
        public void Show(TooltipRequest request, Vector2? pointerPosition = null)
        {
            if (request == null || request.Content == null || request.Anchor == null || !request.Anchor.gameObject.activeInHierarchy)
            {
                return;
            }

            // 查找锚点所在的 Canvas
            var canvas = request.Anchor.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            // 确保提示层在该 Canvas 下
            if (!EnsureLayerRoot(canvas))
            {
                return;
            }

            // 为内容创建演示者
            if (!TooltipPresenterRegistry.TryCreatePresenter(request.Content, _currentLayerRoot, out var presenter))
            {
                HideAll();
                return;
            }

            // 隐藏旧提示
            HideAll();

            // 记录新提示
            _currentRequest = request;
            _currentOwner = request.Owner;
            _currentPresenter = presenter;
            _lastPointerPosition = pointerPosition ?? GetPointerPosition(_lastPointerPosition);

            // 通知演示者显示提示
            _currentPresenter.Show(request);

            // 计算并应用位置
            PositionCurrentTooltip();
        }

        /// <summary>
        /// 隐藏指定所有者的提示。
        /// 只有所有者一致时才隐藏（防止误操作）。
        /// </summary>
        public void Hide(object owner)
        {
            if (owner != null && ReferenceEquals(owner, _currentOwner))
            {
                HideAll();
            }
        }

        /// <summary>
        /// 隐藏所有当前活跃的提示。
        /// 销毁演示者并清理状态。
        /// </summary>
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
                DestroyPresenterObject(component.gameObject);
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
                DestroyPresenterObject(_currentLayerRoot.gameObject);
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

        private static void DestroyPresenterObject(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
