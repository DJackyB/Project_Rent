using BaoZuPo.Card;
using BaoZuPo.UI.Common.Hover;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    [RequireComponent(typeof(UICardView))]
    public class UIEquipmentCardView : MonoBehaviour, IHoverPreviewSource
    {
        [SerializeField] private Vector2 compactSize = new Vector2(170f, 238f);

        private UICardView _cardView;
        private LayoutElement _layoutElement;

        public CardInstance CurrentCard => _cardView != null ? _cardView.Card : null;

        public GameObject HoverSourceObject => gameObject;
        public RectTransform HoverAnchor => transform as RectTransform;
        public object HoverPayload => CurrentCard;

        private void Awake()
        {
            _cardView = GetComponent<UICardView>();
            _layoutElement = GetComponent<LayoutElement>();
            if (_layoutElement == null)
            {
                _layoutElement = gameObject.AddComponent<LayoutElement>();
            }
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
            BindHoverTrigger();
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

        private void BindHoverTrigger()
        {
            var trigger = GetComponent<HoverPreviewTrigger>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<HoverPreviewTrigger>();
            }

            trigger.Bind(this);
        }
    }
}
