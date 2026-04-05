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
        [SerializeField] private float duration = 0.15f;

        private Vector3 _originalScale;
        private Tween _tween;
        private UICardDragHandler _dragHandler;

        private void Awake()
        {
            _originalScale = transform.localScale;
            _dragHandler = GetComponent<UICardDragHandler>();
        }

        private void OnEnable()
        {
            if (_dragHandler == null)
            {
                _dragHandler = GetComponent<UICardDragHandler>();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.isActiveAndEnabled)
            {
                return;
            }

            _tween?.Kill();
            _tween = transform.DOScale(_originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_dragHandler != null && _dragHandler.isActiveAndEnabled)
            {
                return;
            }

            _tween?.Kill();
            _tween = transform.DOScale(_originalScale, duration).SetEase(Ease.OutQuad);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            transform.localScale = _originalScale;
        }
    }
}
