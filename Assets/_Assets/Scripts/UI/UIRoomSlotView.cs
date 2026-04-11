using BaoZuPo.Card;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 房间插槽视图，代表房间内的单个租户或装备插槽。
    /// 动态创建和绑定卡牌实例，显示占位符当插槽为空。
    /// 支持不同的上下文（租户、装备）并应用对应的尺寸约束。
    /// </summary>
    public class UIRoomSlotView : MonoBehaviour
    {
        [SerializeField] private CardViewContext slotContext = CardViewContext.RoomTenant;
        [SerializeField] private Vector2 tenantSlotSize = new Vector2(190f, 260f);
        [SerializeField] private Vector2 equipmentSlotSize = new Vector2(165f, 230f);
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private GameObject placeholderObject;
        [SerializeField] private TextMeshProUGUI placeholderLabel;

        private GameObject _currentCardObject;
        private GameObject _cardPrefab;
        private CardInstance _boundCard;

        public CardInstance BoundCard => _boundCard;
        public UICardView CurrentCardView => _currentCardObject != null ? _currentCardObject.GetComponent<UICardView>() : null;

        public void Setup(CardViewContext context, GameObject cardPrefab)
        {
            slotContext = context;
            _cardPrefab = cardPrefab;

            EnsureRoots();
        }

        public void Bind(CardInstance card)
        {
            _boundCard = card;
            EnsureRoots();
            ClearCurrentCard();

            if (card == null || _cardPrefab == null)
            {
                ShowPlaceholder();
                return;
            }

            HidePlaceholder();
            _currentCardObject = Instantiate(_cardPrefab, contentRoot);
            _currentCardObject.transform.localPosition = Vector3.zero;
            _currentCardObject.transform.localRotation = Quaternion.identity;
            _currentCardObject.transform.localScale = Vector3.one;

            var rect = _currentCardObject.transform as RectTransform;
            if (rect != null)
            {
                ApplyCardSizing(rect);
            }

            var cardView = _currentCardObject.GetComponent<UICardView>();
            if (slotContext == CardViewContext.RoomEquipment)
            {
                var equipmentView = _currentCardObject.GetComponent<UIEquipmentCardView>();
                if (equipmentView != null)
                {
                    equipmentView.Setup(card);
                }
                else
                {
                    Debug.LogError("[UIRoomSlotView] Equipment card prefab is missing UIEquipmentCardView.", _currentCardObject);
                    cardView?.Setup(card, slotContext, null);
                }
            }
            else if (cardView != null)
            {
                cardView.Setup(card, slotContext, null);
            }
        }

        private void EnsureRoots()
        {
            if (contentRoot == null)
            {
                Debug.LogError("[UIRoomSlotView] contentRoot is not assigned. Wire it in the Prefab Inspector.", this);
                return;
            }

            if (layoutElement == null)
            {
                Debug.LogError("[UIRoomSlotView] layoutElement is not assigned. Wire it in the Prefab Inspector.", this);
            }

            if (placeholderObject == null)
            {
                Debug.LogError("[UIRoomSlotView] placeholderObject is not assigned. Wire it in the Prefab Inspector.", this);
            }

            if (placeholderLabel == null)
            {
                Debug.LogError("[UIRoomSlotView] placeholderLabel is not assigned. Wire it in the Prefab Inspector.", this);
            }
        }

        private void ApplyCardSizing(RectTransform rect)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var fitter = rect.GetComponent<AspectRatioFitter>();
            if (fitter == null)
            {
                Debug.LogError("[UIRoomSlotView] Card prefab is missing AspectRatioFitter. Add it to Card.prefab.", rect.gameObject);
                return;
            }
            
            Vector2 designSize = slotContext == CardViewContext.RoomEquipment ? equipmentSlotSize : tenantSlotSize;
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = designSize.x / designSize.y;
        }

        /// <summary>
        /// 将当前卡牌对象从插槽中取出并返回，同时立即显示占位符。
        /// 调用方负责对取出的对象播放动画并最终销毁。
        /// 若插槽为空则返回 null。
        /// </summary>
        public GameObject StealCardObject()
        {
            var obj = _currentCardObject;
            if (obj == null)
            {
                return null;
            }

            _currentCardObject = null;
            obj.transform.SetParent(null, true);
            ShowPlaceholder();
            return obj;
        }

        private void ClearCurrentCard()
        {
            if (_currentCardObject != null)
            {
                Destroy(_currentCardObject);
                _currentCardObject = null;
            }
        }

        private void ShowPlaceholder()
        {
            if (placeholderObject == null)
            {
                Debug.LogError("[UIRoomSlotView] placeholderObject is not assigned. Wire it in the Prefab Inspector.", this);
                return;
            }

            placeholderObject.SetActive(true);

            if (placeholderLabel != null)
            {
                placeholderLabel.text = slotContext == CardViewContext.RoomEquipment ? GameText.EmptyEquipmentSlot : GameText.EmptyTenantSlot;
            }
        }

        private void HidePlaceholder()
        {
            if (placeholderObject != null)
            {
                placeholderObject.SetActive(false);
            }
        }
    }
}
