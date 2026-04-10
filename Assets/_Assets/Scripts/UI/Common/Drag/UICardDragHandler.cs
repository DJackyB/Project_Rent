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
        [SerializeField] private float hoverLift = 22f;
        [SerializeField] private float hoverScale = 1.075f;
        [SerializeField] private float hoverDuration = 0.12f;
        [SerializeField] private float dragScale = 1.1f;
        [SerializeField] private float dragScaleDuration = 0.1f;

        [Header("Idle Motion")]
        [SerializeField] private bool idleMotionEnabled = true;
        [SerializeField] private Vector2 idleMotionAmplitude = new(4f, 2.4f);
        [SerializeField] private Vector2 idleMotionFrequency = new(0.38f, 0.54f);
        [SerializeField] private float idleMotionSmoothing = 6f;

        private UICardView _cardView;
        private CanvasGroup _canvasGroup;
        private LayoutElement _layoutElement;
        private RectTransform _rectTransform;
        private Tween _hoverMoveTween;
        private Tween _hoverScaleTween;
        private Vector2 _baseAnchoredPosition;
        private bool _isBound;
        private bool _loggedMissingCardView;
        private bool _loggedMissingCanvasGroup;
        private bool _loggedMissingLayoutElement;
        private bool _loggedMissingRectTransform;
        private bool _isHovered;
        private bool _hasLayoutBaseline;
        private Vector2 _idleMotionOffset;
        private float _idleMotionSeedX;
        private float _idleMotionSeedY;

        public UICardView CardView => _cardView != null ? _cardView : GetComponent<UICardView>();
        public CanvasGroup CanvasGroup => _canvasGroup;
        public LayoutElement LayoutElement => _layoutElement;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            CacheReferences();
            InitializeIdleMotionSeeds();
            ResetToIdleVisual(false);
        }

        private void Update()
        {
            UpdateIdleMotion();
        }

        private void OnDisable()
        {
            StopTweens();
            _rectTransform?.DOKill(false);
            _isHovered = false;
            _hasLayoutBaseline = false;
            _idleMotionOffset = Vector2.zero;
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

            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _isBound = _cardView != null && _cardView.CurrentContext == CardViewContext.Hand && _cardView.Card != null;
            enabled = _isBound;
            _cardView?.SetDragging(false, true);
            _cardView?.SetSelected(false, true);
            _isHovered = false;
            _hasLayoutBaseline = false;
            ResetIdleMotionOffset();
            ApplyIdleVisualStateWithoutBaseline();
        }

        public void Unbind()
        {
            _isBound = false;
            enabled = false;
            _cardView?.SetDragging(false, true);
            _cardView?.SetSelected(false, true);
            _isHovered = false;
            _hasLayoutBaseline = false;
            ResetIdleMotionOffset();
            ApplyIdleVisualStateWithoutBaseline();
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

            _isHovered = true;
            _cardView?.SetSelected(true);
            AnimateHover(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isBound || IsDragging())
            {
                return;
            }

            _isHovered = false;
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
                ResetIdleMotionOffset();
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
            ResetIdleMotionOffset();

            if (!_hasLayoutBaseline)
            {
                ApplyIdleVisualStateWithoutBaseline();
                return;
            }

            Vector2 targetPosition = _baseAnchoredPosition;

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

        public void RefreshLayoutBaseline(bool snapToIdle = true)
        {
            if (_rectTransform == null)
            {
                return;
            }

            CaptureLayoutBaseline();
            _hasLayoutBaseline = true;
            ResetIdleMotionOffset();

            if (!snapToIdle || IsDragging())
            {
                return;
            }

            StopTweens();
            if (_isHovered)
            {
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, hoverLift);
                _rectTransform.localScale = Vector3.one * hoverScale;
                return;
            }

            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            _rectTransform.localScale = Vector3.one;
        }

        private void AnimateHover(bool hovered)
        {
            if (_rectTransform == null)
            {
                return;
            }

            EnsureLayoutBaseline();
            StopTweens();
            ResetIdleMotionOffset();
            Vector2 targetPosition = hovered
                ? _baseAnchoredPosition + new Vector2(0f, hoverLift)
                : _baseAnchoredPosition;
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

        private void InitializeIdleMotionSeeds()
        {
            int seed = Mathf.Abs(GetInstanceID());
            _idleMotionSeedX = (seed * 0.173f) + 11.3f;
            _idleMotionSeedY = (seed * 0.231f) + 47.9f;
        }

        private void UpdateIdleMotion()
        {
            if (_rectTransform == null)
            {
                return;
            }

            if (!_isBound || !idleMotionEnabled || _isHovered || IsDragging() || IsHoverTweenRunning())
            {
                return;
            }

            EnsureLayoutBaseline();
            float time = Time.unscaledTime;
            Vector2 targetOffset = new(
                (Mathf.PerlinNoise(_idleMotionSeedX, time * idleMotionFrequency.x) - 0.5f) * 2f * idleMotionAmplitude.x,
                (Mathf.PerlinNoise(_idleMotionSeedY, time * idleMotionFrequency.y) - 0.5f) * 2f * idleMotionAmplitude.y);

            float smoothing = idleMotionSmoothing <= 0f
                ? 1f
                : 1f - Mathf.Exp(-idleMotionSmoothing * Time.unscaledDeltaTime);
            _idleMotionOffset = Vector2.Lerp(_idleMotionOffset, targetOffset, smoothing);
            _rectTransform.anchoredPosition = _baseAnchoredPosition + _idleMotionOffset;
        }

        private bool IsHoverTweenRunning()
        {
            return IsTweenRunning(_hoverMoveTween) || IsTweenRunning(_hoverScaleTween);
        }

        private static bool IsTweenRunning(Tween tween)
        {
            return tween != null && tween.IsActive() && tween.IsPlaying();
        }

        private void ResetIdleMotionOffset()
        {
            if (_rectTransform == null)
            {
                return;
            }

            _idleMotionOffset = Vector2.zero;
        }

        private void EnsureLayoutBaseline()
        {
            if (_rectTransform == null || _hasLayoutBaseline)
            {
                return;
            }

            CaptureLayoutBaseline();
            _hasLayoutBaseline = true;
        }

        private void CaptureLayoutBaseline()
        {
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
        }

        private void ApplyIdleVisualStateWithoutBaseline()
        {
            if (_rectTransform == null)
            {
                return;
            }

            StopTweens();
            _rectTransform.localScale = Vector3.one;
            _cardView?.SetDragging(false, true);
            _cardView?.SetSelected(false, true);
        }
    }
}
