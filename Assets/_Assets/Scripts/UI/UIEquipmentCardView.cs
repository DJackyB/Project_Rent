using BaoZuPo.Card;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 装备卡片视图，为房间装备插槽提供紧凑显示。
    /// 包装 UICardView，应用装备专用的尺寸约束。
    /// </summary>
    [RequireComponent(typeof(UICardView))]
    [RequireComponent(typeof(LayoutElement))]
    public class UIEquipmentCardView : MonoBehaviour
    {
        [SerializeField] private Vector2 compactSize = new Vector2(170f, 238f);

        private UICardView _cardView;
        private LayoutElement _layoutElement;

        public CardInstance CurrentCard => _cardView != null ? _cardView.Card : null;

        private void Awake()
        {
            _cardView = GetComponent<UICardView>();
            _layoutElement = GetComponent<LayoutElement>();
        }

        public void Setup(CardInstance card)
        {
            if (_cardView == null)
            {
                _cardView = GetComponent<UICardView>();
            }

            if (_layoutElement == null)
            {
                _layoutElement = GetComponent<LayoutElement>();
            }

            _cardView.Setup(card, CardViewContext.RoomEquipment, null);
            ApplyCompactSizing();
        }

        private void ApplyCompactSizing()
        {
            if (_layoutElement != null)
            {
                _layoutElement.preferredWidth = compactSize.x;
                _layoutElement.preferredHeight = compactSize.y;
                _layoutElement.flexibleWidth = 0f;
                _layoutElement.flexibleHeight = 0f;
            }
        }
    }
}
