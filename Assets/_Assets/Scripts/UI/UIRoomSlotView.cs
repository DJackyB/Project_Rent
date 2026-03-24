using BaoZuPo.Card;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public class UIRoomSlotView : MonoBehaviour
    {
        [SerializeField] private CardViewContext slotContext = CardViewContext.RoomTenant;
        [SerializeField] private Vector2 tenantSlotSize = new Vector2(190f, 260f);
        [SerializeField] private Vector2 equipmentSlotSize = new Vector2(165f, 230f);

        private Transform _contentRoot;
        private GameObject _placeholderObject;
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
            _currentCardObject = Instantiate(_cardPrefab, _contentRoot);
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
                if (equipmentView == null)
                {
                    equipmentView = _currentCardObject.AddComponent<UIEquipmentCardView>();
                }

                equipmentView.Setup(card);
            }
            else if (cardView != null)
            {
                cardView.Setup(card, slotContext, null);
            }
        }

        private void EnsureRoots()
        {
            if (_contentRoot != null)
            {
                return;
            }

            var contentObject = new GameObject("ContentRoot", typeof(RectTransform));
            contentObject.transform.SetParent(transform, false);
            _contentRoot = contentObject.transform;

            var rect = _contentRoot as RectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ApplySlotSizing()
        {
            var layout = GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<LayoutElement>();
            }

            Vector2 size = slotContext == CardViewContext.RoomEquipment ? equipmentSlotSize : tenantSlotSize;
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
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
            if (_placeholderObject != null)
            {
                _placeholderObject.SetActive(true);
                return;
            }

            _placeholderObject = new GameObject("EmptySlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            _placeholderObject.transform.SetParent(_contentRoot, false);

            var rect = _placeholderObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = _placeholderObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(_placeholderObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = ResolveFont();
            label.text = slotContext == CardViewContext.RoomEquipment ? "Empty Equipment Slot" : "Empty Tenant Slot";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 20f;
            label.color = new Color(1f, 1f, 1f, 0.45f);
            label.raycastTarget = false;
        }

        private void HidePlaceholder()
        {
            if (_placeholderObject != null)
            {
                _placeholderObject.SetActive(false);
            }
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (TMP_Settings.defaultFontAsset != null)
            {
                return TMP_Settings.defaultFontAsset;
            }

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }
}
