using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Deck;
using UnityEngine;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 手牌面板，负责显示和管理玩家手中的卡牌列表。
    /// 从 DeckManager 获取当前手牌，动态生成卡牌视图并显示。
    /// 响应手牌变化时刷新整个容器中的卡牌实例。
    /// </summary>
    public class UIHandPanel : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Hand card prefab. Must contain UICardView.")]
        public GameObject cardPrefab;

        [Tooltip("Hand container. This should be assigned explicitly.")]
        public Transform handContainer;

        private readonly List<UICardView> _cardViews = new();

        public void RefreshHand()
        {
            var container = EnsureContainer();
            if (cardPrefab == null)
            {
                Debug.LogError("[UIHandPanel] Missing cardPrefab.");
                return;
            }

            ClearContainer(container);
            _cardViews.Clear();

            var hand = DeckManager.Instance.Hand;
            for (int i = 0; i < hand.Count; i++)
            {
                var cardObject = Instantiate(cardPrefab, container);
                var cardView = cardObject.GetComponent<UICardView>();
                if (cardView == null)
                {
                    Debug.LogError("[UIHandPanel] Card prefab requires UICardView.");
                    continue;
                }

                cardView.Setup(hand[i], CardViewContext.Hand, this);
                _cardViews.Add(cardView);
            }
        }

        private Transform EnsureContainer()
        {
            if (handContainer == null)
            {
                Debug.LogError("[UIHandPanel] handContainer is not assigned in the Inspector. Create a HandContainer child under UIHandPanel and assign it.");
            }

            return handContainer;
        }

        private static void ClearContainer(Transform container)
        {
            if (container == null)
            {
                return;
            }

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
