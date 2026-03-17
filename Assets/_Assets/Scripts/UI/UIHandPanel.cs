using System.Collections.Generic;
using UnityEngine;
using BaoZuPo.Card;
using BaoZuPo.Deck;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 手牌区域面板
    /// 管理手牌 UI 的生成和刷新。
    /// </summary>
    public class UIHandPanel : MonoBehaviour
    {
        [Header("配置")]
        [Tooltip("卡牌 UI Prefab（挂 UICardView 脚本的 Prefab）")]
        public GameObject cardPrefab;

        [Tooltip("手牌容器（带 HorizontalLayoutGroup）")]
        public Transform handContainer;

        private List<UICardView> _cardViews = new();

        /// <summary>
        /// 当前被选中的手牌
        /// </summary>
        public CardInstance SelectedCard { get; private set; }

        /// <summary>
        /// 刷新手牌显示
        /// </summary>
        public void RefreshHand()
        {
            // 清除旧的
            foreach (var view in _cardViews)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
            _cardViews.Clear();

            // 生成新的
            var hand = DeckManager.Instance.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var go = Instantiate(cardPrefab, handContainer);
                var cardView = go.GetComponent<UICardView>();
                cardView.Setup(hand[i], this);
                _cardViews.Add(cardView);
            }
        }

        /// <summary>
        /// 选中一张手牌（由 UICardView 调用）
        /// </summary>
        public void SelectCard(CardInstance card)
        {
            SelectedCard = card;
            Debug.Log($"[UI] 选中手牌: {card.Data.cardName}");

            // 高亮选中的卡牌
            foreach (var view in _cardViews)
            {
                view.SetSelected(view.Card == card);
            }
        }

        /// <summary>
        /// 取消选中
        /// </summary>
        public void DeselectCard()
        {
            SelectedCard = null;
            foreach (var view in _cardViews)
            {
                view.SetSelected(false);
            }
        }
    }
}
