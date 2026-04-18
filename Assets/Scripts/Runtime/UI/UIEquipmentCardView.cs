using BaoZuPo.Card;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 装备卡片视图，为房间装备插槽提供紧凑显示。
    /// 包装 UICardView，应用装备专用的尺寸约束。
    /// </summary>
    [RequireComponent(typeof(UICardView))]
    public class UIEquipmentCardView : MonoBehaviour
    {
        private UICardView _cardView;

        public CardInstance CurrentCard => _cardView != null ? _cardView.Card : null;

        private void Awake()
        {
            _cardView = GetComponent<UICardView>();
        }

        public void Setup(CardInstance card)
        {
            if (_cardView == null)
            {
                _cardView = GetComponent<UICardView>();
            }

            _cardView.Setup(card, CardViewContext.RoomEquipment, null);
        }
    }
}
