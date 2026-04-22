using BaoZuPo.Card;
using UnityEngine;
using UnityEngine.UI;
namespace BaoZuPo.UI
{
    /// <summary>
    /// Thin room card item wrapper.
    /// It hosts one room card view and provides a stable handoff point for
    /// room-specific animations without owning layout rules.
    /// </summary>
    public class UIRoomSlotView : MonoBehaviour
    {
        [SerializeField] private CardViewContext slotContext = CardViewContext.RoomTenant;
        [SerializeField] private RectTransform contentRoot;

        private GameObject _currentCardObject;
        private GameObject _cardPrefab;
        private CardInstance _boundCard;

        public CardInstance BoundCard => _boundCard;
        public UICardView CurrentCardView => _currentCardObject != null ? _currentCardObject.GetComponent<UICardView>() : null;
        public RectTransform CardAnchor
        {
            get
            {
                var cardView = CurrentCardView;
                if (cardView != null)
                {
                    return cardView.HoverAnchor;
                }

                return _currentCardObject != null ? _currentCardObject.transform as RectTransform : transform as RectTransform;
            }
        }

        public void Setup(CardViewContext context, GameObject cardPrefab)
        {
            slotContext = context;
            _cardPrefab = cardPrefab;
            EnsureContentRoot();
        }

        public void Bind(CardInstance card)
        {
            _boundCard = card;
            EnsureContentRoot();
            ClearCurrentCard();

            if (card == null || _cardPrefab == null || contentRoot == null)
            {
                return;
            }

            _currentCardObject = Instantiate(_cardPrefab, contentRoot, false);

            var cardView = _currentCardObject.GetComponent<UICardView>();
            if (cardView != null)
            {
                cardView.Setup(card, slotContext, null);
            }

            RefreshCardScale();
        }

        public void RefreshCardScale()
        {
            if (_currentCardObject == null || contentRoot == null)
            {
                return;
            }

            var cardRect = _currentCardObject.transform as RectTransform;
            if (cardRect == null)
            {
                return;
            }

            float baseWidth = ResolveBaseWidth(cardRect);
            float baseHeight = ResolveBaseHeight(cardRect);
            if (baseWidth <= 0f || baseHeight <= 0f)
            {
                return;
            }

            float availableWidth = contentRoot.rect.width;
            float availableHeight = contentRoot.rect.height;
            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                cardRect.localScale = Vector3.one;
                return;
            }

            float fitScale = Mathf.Min(availableWidth / baseWidth, availableHeight / baseHeight);
            cardRect.localScale = new Vector3(fitScale, fitScale, 1f);
            cardRect.anchoredPosition = Vector2.zero;
        }

        public GameObject StealCardObject()
        {
            var obj = _currentCardObject;
            if (obj == null)
            {
                return null;
            }

            _currentCardObject = null;
            obj.transform.SetParent(null, true);
            return obj;
        }

        private void EnsureContentRoot()
        {
            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }
        }

        private static float ResolveBaseWidth(RectTransform cardRect)
        {
            var layout = cardRect.GetComponent<LayoutElement>();
            if (layout != null && layout.preferredWidth > 0f)
            {
                return layout.preferredWidth;
            }

            if (cardRect.rect.width > 0f)
            {
                return cardRect.rect.width;
            }

            return Mathf.Abs(cardRect.sizeDelta.x);
        }

        private static float ResolveBaseHeight(RectTransform cardRect)
        {
            var layout = cardRect.GetComponent<LayoutElement>();
            if (layout != null && layout.preferredHeight > 0f)
            {
                return layout.preferredHeight;
            }

            if (cardRect.rect.height > 0f)
            {
                return cardRect.rect.height;
            }

            return Mathf.Abs(cardRect.sizeDelta.y);
        }

        private void ClearCurrentCard()
        {
            if (_currentCardObject != null)
            {
                Destroy(_currentCardObject);
                _currentCardObject = null;
            }
        }
    }
}
