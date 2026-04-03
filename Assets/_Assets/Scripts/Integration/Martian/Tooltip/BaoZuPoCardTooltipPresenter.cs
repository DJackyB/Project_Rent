using BaoZuPo.Card;
using BaoZuPo.UI.Common.Drag;
using BaoZuPo.UI;
using Martian.Tooltip;
using Martian.Tooltip.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.Integration.Martian.Tooltip
{
    /// <summary>
    /// 卡牌预览呈现器。
    /// 实现 ITooltipPresenter 接口，负责生成和管理卡牌预览的视觉显示。
    ///
    /// 工作流程：
    /// 1. 用户悬停卡牌，TooltipTrigger 发送 TooltipRequest（含原始卡牌 GameObject）
    /// 2. Show() 被调用：克隆原始卡牌，禁用交互，调整大小
    /// 3. 预览显示在提示气泡中
    /// 4. 用户移开鼠标，Hide() 销毁预览
    ///
    /// 关键：
    /// - 克隆的是完整的卡牌 UI 对象（包括所有 UICardView 组件）
    /// - 禁用克隆上的所有交互和动画组件（Button、Layout、Drag 等）
    /// - 大小写死为 previewSize（防止遮挡提示内容）
    /// </summary>
    public sealed class BaoZuPoCardTooltipPresenter : MonoBehaviour, ITooltipPresenter
    {
        [SerializeField] private Vector2 previewSize = new Vector2(260f, 360f);

        private RectTransform _containerRoot;
        private RectTransform _root;
        private GameObject _previewInstance;

        public RectTransform Root => _containerRoot;

        /// <summary>
        /// 显示提示：克隆卡牌 UI，禁用交互，调整大小后显示。
        /// </summary>
        public void Show(TooltipRequest request)
        {
            Hide();

            if (request == null || request.Content == null || request.Content.ContentId != BaoZuPoTooltipContentIds.CardPreview)
            {
                return;
            }

            var sourceObject = request.Anchor != null ? request.Anchor.gameObject : null;
            if (sourceObject == null)
            {
                return;
            }

            // 克隆原始卡牌 UI 对象
            _containerRoot = transform as RectTransform;
            _previewInstance = Instantiate(sourceObject, transform, false);
            _previewInstance.name = $"{sourceObject.name}_TooltipPreview";
            _root = _previewInstance.transform as RectTransform;

            // 配置克隆的卡牌视图（重新设置卡牌数据）
            ConfigureClonedCardView(request.Content.Payload as CardInstance);

            // 禁用所有交互（按钮、拖拽、触碰等）
            DisableInteraction(_previewInstance);

            // 调整大小并刷新布局
            ApplyPreviewSizing();
            _previewInstance.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_root);
            if (_containerRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_containerRoot);
            }
        }

        /// <summary>
        /// 隐藏提示：销毁预览对象。
        /// </summary>
        public void Hide()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }

            _containerRoot = null;
            _root = null;
        }

        /// <summary>
        /// 配置克隆的卡牌视图：重新绑定卡牌数据。
        /// </summary>
        private void ConfigureClonedCardView(CardInstance card)
        {
            if (_previewInstance == null || card == null)
            {
                return;
            }

            var cardView = _previewInstance.GetComponent<UICardView>();
            if (cardView == null)
            {
                return;
            }

            // 用原始卡牌数据重新初始化克隆的卡牌视图
            cardView.Setup(card, CardViewContext.TooltipPreview, null);
            cardView.SetSelected(false);
        }

        /// <summary>
        /// 禁用预览对象上的所有交互组件，使其仅显示，不响应输入。
        /// </summary>
        private static void DisableInteraction(GameObject previewObject)
        {
            // 禁用 Raycast 检测（防止点击透穿）
            foreach (var graphic in previewObject.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }

            // 禁用按钮交互
            foreach (var button in previewObject.GetComponentsInChildren<Button>(true))
            {
                button.interactable = false;
            }

            // 禁用布局控制
            foreach (var layoutElement in previewObject.GetComponentsInChildren<LayoutElement>(true))
            {
                layoutElement.enabled = false;
            }

            // 禁用装备卡视图（如果有）
            foreach (var equipmentView in previewObject.GetComponentsInChildren<UIEquipmentCardView>(true))
            {
                equipmentView.enabled = false;
            }

            // 禁用提示触发器（防止套嵌提示）
            foreach (var trigger in previewObject.GetComponentsInChildren<TooltipTrigger>(true))
            {
                trigger.enabled = false;
            }

            // 禁用拖拽
            foreach (var dragHandler in previewObject.GetComponentsInChildren<UICardDragHandler>(true))
            {
                dragHandler.enabled = false;
            }

            // 配置 CanvasGroup 阻止交互
            var canvasGroup = previewObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                return;
            }

            Debug.LogError(
                "[CardTooltipPresenter] Card prefab requires CanvasGroup for tooltip previews. " +
                "Please configure Card.prefab instead of relying on AddComponent.",
                previewObject);
        }

        /// <summary>
        /// 调整预览大小和锚点。保持原始 sizeDelta 不变，通过 localScale 等比缩放，
        /// 避免子元素（字体、位置、间距）因 RectTransform 拉伸而失真。
        /// </summary>
        private void ApplyPreviewSizing()
        {
            if (_root == null)
            {
                return;
            }

            if (_containerRoot == null)
            {
                _containerRoot = transform as RectTransform;
            }

            if (_containerRoot != null)
            {
                _containerRoot.anchorMin = new Vector2(0f, 0f);
                _containerRoot.anchorMax = new Vector2(0f, 0f);
                _containerRoot.pivot = new Vector2(0f, 0f);
                _containerRoot.sizeDelta = Vector2.zero;
                _containerRoot.anchoredPosition = Vector2.zero;
            }

            _root.anchorMin = new Vector2(0f, 0f);
            _root.anchorMax = new Vector2(0f, 0f);
            _root.pivot = new Vector2(0f, 0f);
            _root.anchoredPosition = Vector2.zero;

            // 保持原始 sizeDelta，用 localScale 等比缩放
            Vector2 originalSize = _root.sizeDelta;
            if (originalSize.x > 0f && originalSize.y > 0f)
            {
                float scaleX = previewSize.x / originalSize.x;
                float scaleY = previewSize.y / originalSize.y;
                float uniformScale = Mathf.Min(scaleX, scaleY);
                _root.localScale = new Vector3(uniformScale, uniformScale, 1f);
            }

            var layoutElement = _root.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.enabled = false;
            }
        }
    }

    /// <summary>
    /// 卡牌预览呈现器工厂。实现 ITooltipPresenterFactory，用于注册到提示系统。
    /// 当提示系统需要呈现卡牌预览时，通过本工厂创建呈现器实例。
    /// </summary>
    internal sealed class BaoZuPoCardTooltipPresenterFactory : ITooltipPresenterFactory
    {
        /// <summary>
        /// 检查是否能处理此内容（必须是卡牌预览且 Payload 为 CardInstance）。
        /// </summary>
        public bool CanPresent(TooltipContent content)
        {
            return content != null
                && content.ContentId == BaoZuPoTooltipContentIds.CardPreview
                && content.Payload is CardInstance;
        }

        /// <summary>
        /// 创建呈现器实例（在提示气泡内创建新 GameObject）。
        /// </summary>
        public ITooltipPresenter Create(Transform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var presenterObject = new GameObject(nameof(BaoZuPoCardTooltipPresenter), typeof(RectTransform), typeof(BaoZuPoCardTooltipPresenter));
            presenterObject.transform.SetParent(parent, false);
            return presenterObject.GetComponent<BaoZuPoCardTooltipPresenter>();
        }
    }

    /// <summary>
    /// 卡牌预览呈现器注册管理。
    /// 游戏初始化时调用 Install() 以注册工厂到提示系统。
    /// </summary>
    public static class BaoZuPoCardTooltipPresenterRegistration
    {
        private static readonly ITooltipPresenterFactory Factory = new BaoZuPoCardTooltipPresenterFactory();
        private static bool _installed;

        /// <summary>
        /// 注册工厂到提示系统（仅执行一次）。
        /// </summary>
        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            TooltipPresenterRegistry.Register(Factory);
            _installed = true;
        }
    }
}
