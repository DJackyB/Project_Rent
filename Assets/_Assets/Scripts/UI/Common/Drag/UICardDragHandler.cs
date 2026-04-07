using BaoZuPo.Card;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.Drag
{
    /// <summary>
    /// 卡牌拖拽处理器，负责卡牌拖拽输入的捕获和处理。
    /// 实现 Unity 拖拽系统的完整接口（初始化、开始、拖动中、结束）。
    /// 在手牌上下文时启用，提供悬停效果（提升、缩放）和拖拽中的视觉反馈。
    /// 委派给 UICardDragController 处理拖拽逻辑。
    /// </summary>
    [RequireComponent(typeof(UICardView))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(LayoutElement))]
    public class UICardDragHandler : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hand Motion")]
        [SerializeField] private float hoverLift = 18f;
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float dragScale = 1.08f;
        [SerializeField] private float dragScaleDuration = 0.1f;

        private UICardView _cardView;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;
        private RectTransform _rectTransform;
        private Tween _hoverMoveTween;
        private Tween _hoverScaleTween;
        private Vector2 _idleAnchoredPosition;
        private bool _isBound;
        private bool _loggedMissingCardView;
        private bool _loggedMissingCanvasGroup;
        private bool _loggedMissingLayoutElement;
        private bool _loggedMissingRectTransform;

        public UICardView CardView => _cardView != null ? _cardView : GetComponent<UICardView>();
        public CanvasGroup CanvasGroup => _canvasGroup;
        public LayoutElement LayoutElement => _layoutElement;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            CacheReferences();
            ResetToIdleVisual(false);
        }

        private void OnDisable()
        {
            StopTweens();
            _rectTransform?.DOKill(false);
            if (UICardDragController.Instance != null)
            {
                UICardDragController.Instance.NotifySourceDisabled(this);
            }
        }

        public void Bind(UICardView cardView)
        {
            _cardView = cardView;
            CacheReferences();

            if (_cardView == null || _canvasGroup == null || _layoutElement == null || _rectTransform == null)
            {
                _isBound = false;
                enabled = false;
                return;
            }

            _idleAnchoredPosition = _rectTransform.anchoredPosition;
            _isBound = _cardView != null && _cardView.CurrentContext == CardViewContext.Hand && _cardView.Card != null;
            enabled = _isBound;
            _cardView?.SetDragging(false, true);
            _cardView?.SetSelected(false, true);
            ResetToIdleVisual(false);
        }

        public void Unbind()
        {
            _isBound = false;
            enabled = false;
            _cardView?.SetDragging(false, true);
            _cardView?.SetSelected(false, true);
            ResetToIdleVisual(false);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (_isBound)
            {
                eventData.useDragThreshold = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isBound || CardView == null)
            {
                return;
            }

            UICardDragController.Instance?.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isBound)
            {
                return;
            }

            UICardDragController.Instance?.UpdateDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isBound)
            {
                return;
            }

            UICardDragController.Instance?.EndDrag(eventData);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isBound || IsDragging())
            {
                return;
            }

            _cardView?.SetSelected(true);
            AnimateHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isBound || IsDragging())
            {
                return;
            }

            _cardView?.SetSelected(false);
            AnimateHover(false);
        }

        public void SetDragVisualState(bool dragging)
        {
            StopTweens();
            if (_rectTransform == null)
            {
                return;
            }

            if (dragging)
            {
                _cardView?.SetDragging(true);
                _cardView?.SetSelected(true);
                _hoverMoveTween = _rectTransform.DOAnchorPos(_rectTransform.anchoredPosition, 0f).SetUpdate(true);
                _hoverScaleTween = _rectTransform.DOScale(Vector3.one * dragScale, dragScaleDuration).SetEase(Ease.OutQuad).SetUpdate(true);
                return;
            }

            _cardView?.SetDragging(false);
            _cardView?.SetSelected(false);
            ResetToIdleVisual(true);
        }

        public void ResetToIdleVisual(bool animate)
        {
            if (_rectTransform == null)
            {
                return;
            }

            StopTweens();
            Vector2 targetPosition = _idleAnchoredPosition == default ? _rectTransform.anchoredPosition : _idleAnchoredPosition;

            if (!animate)
            {
                _rectTransform.anchoredPosition = targetPosition;
                _rectTransform.localScale = Vector3.one;
                _cardView?.SetDragging(false, true);
                _cardView?.SetSelected(false, true);
                return;
            }

            _hoverMoveTween = _rectTransform.DOAnchorPos(targetPosition, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            _hoverScaleTween = _rectTransform.DOScale(Vector3.one, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            _cardView?.SetDragging(false);
            _cardView?.SetSelected(false);
        }

        private void AnimateHover(bool hovered)
        {
            if (_rectTransform == null)
            {
                return;
            }

            StopTweens();
            if (hovered)
            {
                RefreshIdlePositionFromLayout();
            }

            Vector2 targetPosition = hovered
                ? _idleAnchoredPosition + new Vector2(0f, hoverLift)
                : _idleAnchoredPosition;
            Vector3 targetScale = hovered ? Vector3.one * hoverScale : Vector3.one;

            _hoverMoveTween = _rectTransform.DOAnchorPos(targetPosition, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            _hoverScaleTween = _rectTransform.DOScale(targetScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        private bool IsDragging()
        {
            return UICardDragController.Instance != null && UICardDragController.Instance.IsDragging(this);
        }

        private void CacheReferences()
        {
            if (_cardView == null)
            {
                _cardView = GetComponent<UICardView>();
                if (_cardView == null && !_loggedMissingCardView)
                {
                    _loggedMissingCardView = true;
                    Debug.LogError("[UICardDragHandler] Missing UICardView on Card.prefab.", this);
                }
            }

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null && !_loggedMissingCanvasGroup)
                {
                    _loggedMissingCanvasGroup = true;
                    Debug.LogError(
                        "[UICardDragHandler] Missing CanvasGroup on Card.prefab. " +
                        "Please configure the prefab instead of relying on AddComponent.",
                        this);
                }
            }

            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
                if (_layoutElement == null && !_loggedMissingLayoutElement)
                {
                    _loggedMissingLayoutElement = true;
                    Debug.LogError(
                        "[UICardDragHandler] Missing LayoutElement on Card.prefab. " +
                        "Please configure the prefab instead of relying on AddComponent.",
                        this);
                }
            }

            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
                if (_rectTransform == null && !_loggedMissingRectTransform)
                {
                    _loggedMissingRectTransform = true;
                    Debug.LogError("[UICardDragHandler] Missing RectTransform on draggable card.", this);
                }
            }
        }

        private void StopTweens()
        {
            _hoverMoveTween?.Kill(false);
            _hoverScaleTween?.Kill(false);
            _hoverMoveTween = null;
            _hoverScaleTween = null;
        }

        private void RefreshIdlePositionFromLayout()
        {
            var parentRect = _rectTransform.parent as RectTransform;
            if (parentRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            _idleAnchoredPosition = _rectTransform.anchoredPosition;
        }
    }
}
