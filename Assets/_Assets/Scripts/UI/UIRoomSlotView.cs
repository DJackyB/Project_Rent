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
            ApplySlotSizing();
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
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
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
                contentRoot = transform.Find("ContentRoot") as RectTransform;
            }

            if (contentRoot == null)
            {
                var contentObject = new GameObject("ContentRoot", typeof(RectTransform));
                contentObject.transform.SetParent(transform, false);
                contentRoot = contentObject.GetComponent<RectTransform>();

                contentRoot.anchorMin = Vector2.zero;
                contentRoot.anchorMax = Vector2.one;
                contentRoot.offsetMin = Vector2.zero;
                contentRoot.offsetMax = Vector2.zero;
            }

            if (layoutElement == null)
            {
                layoutElement = GetComponent<LayoutElement>();
            }

            if (placeholderObject == null)
            {
                placeholderObject = contentRoot.Find("EmptySlot")?.gameObject;
            }

            if (placeholderLabel == null && placeholderObject != null)
            {
                placeholderLabel = placeholderObject.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        private void ApplySlotSizing()
        {
            if (layoutElement == null)
            {
                layoutElement = GetComponent<LayoutElement>();
            }

            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            Vector2 size = slotContext == CardViewContext.RoomEquipment ? equipmentSlotSize : tenantSlotSize;
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
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
            if (placeholderObject != null)
            {
                placeholderObject.SetActive(true);
                if (placeholderLabel == null)
                {
                    placeholderLabel = placeholderObject.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                if (placeholderLabel != null)
                {
                    placeholderLabel.text = slotContext == CardViewContext.RoomEquipment ? GameText.EmptyEquipmentSlot : GameText.EmptyTenantSlot;
                }
                return;
            }

            placeholderObject = new GameObject("EmptySlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            placeholderObject.transform.SetParent(contentRoot, false);

            var rect = placeholderObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = placeholderObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(placeholderObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            placeholderLabel = labelObject.GetComponent<TextMeshProUGUI>();
            placeholderLabel.text = slotContext == CardViewContext.RoomEquipment ? GameText.EmptyEquipmentSlot : GameText.EmptyTenantSlot;
            placeholderLabel.alignment = TextAlignmentOptions.Center;
            placeholderLabel.fontSize = 20f;
            placeholderLabel.color = new Color(1f, 1f, 1f, 0.45f);
            placeholderLabel.raycastTarget = false;
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
