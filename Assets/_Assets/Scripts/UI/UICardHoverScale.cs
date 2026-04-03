using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

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

        private void Awake()
        {
            _originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tween?.Kill();
            _tween = transform.DOScale(_originalScale * hoverScale, duration).SetEase(Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
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
