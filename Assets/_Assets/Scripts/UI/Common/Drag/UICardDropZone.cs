using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaoZuPo.UI.Common.Drag
{
    public class UICardDropZone : MonoBehaviour, IDropHandler
    {
        private static readonly List<UICardDropZone> RegisteredZones = new();

        [Header("Drop Zone")]
        [SerializeField] private CardPlayTargetKind zoneKind = CardPlayTargetKind.Room;
        [SerializeField] private RectTransform dropAnchor;
        [SerializeField] private Graphic highlightGraphic;
        [SerializeField] private Color highlightColor = new Color(0.48f, 0.84f, 0.62f, 0.26f);
        [SerializeField] private float highlightFadeSeconds = 0.12f;

        private Graphic _raycastGraphic;
        private Color _hiddenColor;
        private RoomSlot _boundRoom;
        private bool _isHighlighted;

        public static IReadOnlyList<UICardDropZone> Zones => RegisteredZones;

        public CardPlayTargetKind ZoneKind
        {
            get => zoneKind;
            set => zoneKind = value;
        }

        public RectTransform DropAnchor => dropAnchor != null ? dropAnchor : transform as RectTransform;
        public RoomSlot BoundRoom => _boundRoom;

        private void Awake()
        {
            EnsureVisuals();
            CacheHiddenColor();
            SetHighlighted(false, true);
        }

        private void OnEnable()
        {
            if (!RegisteredZones.Contains(this))
            {
                RegisteredZones.Add(this);
            }

            EnsureVisuals();
            CacheHiddenColor();
            SetHighlighted(false, true);
        }

        private void OnDisable()
        {
            RegisteredZones.Remove(this);
            if (highlightGraphic != null)
            {
                highlightGraphic.DOKill();
            }

            SetHighlighted(false, true);
        }

        public void AssignRuntimeReferences(RectTransform anchor, Graphic highlight)
        {
            if (anchor != null)
            {
                dropAnchor = anchor;
            }

            if (highlight != null)
            {
                highlightGraphic = highlight;
                highlightGraphic.raycastTarget = false;
            }

            EnsureVisuals();
            CacheHiddenColor();
            SetHighlighted(false, true);
        }

        public void BindRoom(RoomSlot room)
        {
            _boundRoom = room;
        }

        public bool CanPotentiallyAccept(CardInstance card)
        {
            if (card == null || card.Data == null)
            {
                return false;
            }

            bool requiresRoom = TurnManager.Instance != null
                ? TurnManager.Instance.CardNeedsRoomTarget(card)
                : RequiresRoomTarget(card);

            if (zoneKind == CardPlayTargetKind.PlayArea)
            {
                return !requiresRoom;
            }

            if (!requiresRoom || _boundRoom == null)
            {
                return false;
            }

            if (card.Data.cardType == CardType.Tenant)
            {
                return _boundRoom.CanPlaceTenant;
            }

            if (card.Data.cardType == CardType.Equipment)
            {
                return _boundRoom.CanPlaceEquipment;
            }

            return true;
        }

        public void SetHighlighted(bool highlighted, bool immediate = false)
        {
            if (_isHighlighted == highlighted && !immediate)
            {
                return;
            }

            _isHighlighted = highlighted;
            if (highlightGraphic == null)
            {
                return;
            }

            highlightGraphic.DOKill();
            Color targetColor = highlighted ? highlightColor : _hiddenColor;

            if (immediate)
            {
                highlightGraphic.color = targetColor;
                return;
            }

            highlightGraphic.DOColor(targetColor, highlightFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true);
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            UICardDragController.Instance?.RequestDrop(this);
        }

        private void EnsureVisuals()
        {
            if (_raycastGraphic == null)
            {
                _raycastGraphic = GetComponent<Graphic>();
            }

            if (_raycastGraphic == null)
            {
                Debug.LogError("[UICardDropZone] Missing a Graphic component for raycast detection. Add an Image component to the prefab.", gameObject);
            }
            else
            {
                _raycastGraphic.raycastTarget = true;
            }

            if (highlightGraphic == null)
            {
                var highlightTransform = transform.Find("DropHighlight");
                if (highlightTransform != null)
                {
                    highlightGraphic = highlightTransform.GetComponent<Graphic>();
                }
            }

            if (highlightGraphic == null)
            {
                Debug.LogError("[UICardDropZone] Missing highlightGraphic. Create a DropHighlight child (Image) in the prefab and assign it.", gameObject);
            }
            else
            {
                highlightGraphic.raycastTarget = false;
            }
        }

        private void CacheHiddenColor()
        {
            if (highlightGraphic == null)
            {
                return;
            }

            _hiddenColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
        }

        private static bool RequiresRoomTarget(CardInstance card)
        {
            return CardTargeting.GetRequiredTargetKind(card != null ? card.Data : null) == CardPlayTargetKind.Room;
        }
    }
}
