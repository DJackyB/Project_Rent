using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using BaoZuPo.UI.Common.Drag;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 卡牌悬停放大效果。鼠标进入时缩放到 hoverScale，离开时恢复原始比例。
    /// 挂在 Card.prefab 根节点上。
    /// </summary>
    public class UICardHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float hoverLift = 16f;
        [SerializeField] private float duration = 0.15f;

        private Vector3 _originalScale;
        private Vector2 _originalAnchoredPosition;
        private Tween _tween;
        private Tween _moveTween;
        private UICardDragHandler _dragHandler;
        private UICardView _cardView;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _dragHandler = GetComponent<UICardDragHandler>();
            _cardView = GetComponent<UICardView>();
            _rectTransform = transform as RectTransform;
            if (_rectTransform != null)
            {
                _originalAnchoredPosition = _rectTransform.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            if (_dragHandler == null)
            {
                _dragHandler = GetComponent<UICardDragHandler>();
            }

            if (_cardView == null)
            {
                _cardView = GetComponent<UICardView>();
            }

            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_rectTransform != null)
            {
                _originalAnchoredPosition = _rectTransform.anchoredPosition;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.isActiveAndEnabled)
            {
                return;
            }

            _tween?.Kill();
            _moveTween?.Kill();
            _cardView?.SetSelected(true);
            _tween = transform.DOScale(_originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
            if (_rectTransform != null)
            {
                _moveTween = _rectTransform.DOAnchorPos(_originalAnchoredPosition + new Vector2(0f, hoverLift), duration).SetEase(Ease.OutQuad);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.isActiveAndEnabled)
            {
                return;
            }

            _tween?.Kill();
            _moveTween?.Kill();
            _cardView?.SetSelected(false, true);
            _tween = transform.DOScale(_originalScale, duration).SetEase(Ease.OutQuad);
            if (_rectTransform != null)
            {
                _moveTween = _rectTransform.DOAnchorPos(_originalAnchoredPosition, duration).SetEase(Ease.OutQuad);
            }
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _moveTween?.Kill();
            transform.DOKill(false);
            _rectTransform?.DOKill(false);
            transform.localScale = _originalScale;
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
            }
            _cardView?.SetSelected(false);
        }
    }
}
