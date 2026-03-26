using System.Collections.Generic;
using BaoZuPo.Card;
using BaoZuPo.Deck;
using UnityEngine;

namespace BaoZuPo.UI
{
    public class UIHandPanel : MonoBehaviour
    {
        [Header("\u573A\u666F\u5F15\u7528")]
        [Tooltip("\u624B\u724C\u5361\u724C prefab\uff0c\u9700\u8981\u6302\u8F7D UICardView")]
        public GameObject cardPrefab;

        [Tooltip("\u624B\u724C\u5BB9\u5668\uff1B\u5982\u679C\u4E3A\u7A7A\uff0C\u5F53\u524D\u5B9E\u73B0\u4F1A\u5728\u8FD0\u884C\u65F6\u4E34\u65F6\u521B\u5EFA")]
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
                Debug.LogError("[UIHandPanel] handContainer 未在 Inspector 中赋值。请在 UIHandPanel 下创建子对象 HandContainer (RectTransform) 并拖入。");
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
