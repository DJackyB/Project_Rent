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
    /// <summary>
    /// 放置区域（DropZone），标记可接收卡牌放置的有效目标。
    /// 验证卡牌是否可被放置在该区域（类型匹配、空间可用）。
    /// 管理高亮显示的动画反馈，向拖拽控制器通知放置意图。
    /// 支持房间和放置区域两种目标类型。
    /// </summary>
    public class UICardDropZone : MonoBehaviour, IDropHandler
    {
        private static readonly List<UICardDropZone> RegisteredZones = new();

        [Header("Drop Zone")]
        [SerializeField] private CardPlayTargetKind zoneKind = CardPlayTargetKind.Room;
        [SerializeField] private RectTransform dropAnchor;
        [SerializeField] private Graphic highlightGraphic;
        [SerializeField] private Color highlightColor = new Color(0.48f, 0.84f, 0.62f, 0.26f);
        [SerializeField] private Color rejectColor = new Color(1f, 0.42f, 0.36f, 0.28f);
        [SerializeField] private float highlightFadeSeconds = 0.12f;
        [SerializeField] private float highlightScale = 1.04f;
        [SerializeField] private float pulseDuration = 0.2f;

        private Graphic _raycastGraphic;
        private Color _hiddenColor;
        private RoomSlot _boundRoom;
        private bool _isHighlighted;
        private RectTransform _highlightRect;
        private Tween _highlightScaleTween;
        private Tween _highlightColorTween;

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

            if (CardTargeting.PersistsInRoom(card.Data))
            {
                if (card.Data.cardType == CardType.Tenant)
                {
                    return _boundRoom.CanPlaceTenant;
                }

                if (card.Data.cardType == CardType.Equipment)
                {
                    return _boundRoom.CanPlaceEquipment;
                }
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
            _highlightScaleTween?.Kill(false);
            _highlightColorTween?.Kill(false);
            Color targetColor = highlighted ? highlightColor : _hiddenColor;
            Vector3 targetScale = highlighted ? Vector3.one * highlightScale : Vector3.one;

            if (immediate)
            {
                highlightGraphic.color = targetColor;
                if (_highlightRect != null)
                {
                    _highlightRect.localScale = targetScale;
                }
                return;
            }

            _highlightColorTween = highlightGraphic.DOColor(targetColor, highlightFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            if (_highlightRect != null)
            {
                _highlightScaleTween = _highlightRect.DOScale(targetScale, highlightFadeSeconds).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject);
            }
        }

        public void PlayAcceptedFeedback()
        {
            PlayPulse(highlightColor, 0.95f, 0.1f);
        }

        public void PlayRejectedFeedback()
        {
            PlayPulse(rejectColor, 0.7f, 0.06f);
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
                _highlightRect = highlightGraphic.rectTransform;
            }
        }

        private void CacheHiddenColor()
        {
            if (highlightGraphic == null)
            {
                return;
            }

            _hiddenColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
            if (_highlightRect != null)
            {
                _highlightRect.localScale = Vector3.one;
            }
        }

        private static bool RequiresRoomTarget(CardInstance card)
        {
            return CardTargeting.GetRequiredTargetKind(card != null ? card.Data : null) == CardPlayTargetKind.Room;
        }

        private void PlayPulse(Color pulseColor, float targetAlpha, float scalePunch)
        {
            if (highlightGraphic == null)
            {
                return;
            }

            EnsureVisuals();
            highlightGraphic.DOKill();
            _highlightScaleTween?.Kill(false);
            _highlightColorTween?.Kill(false);

            var visibleColor = new Color(pulseColor.r, pulseColor.g, pulseColor.b, targetAlpha);
            DG.Tweening.Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            sequence.AppendCallback(() =>
            {
                highlightGraphic.color = visibleColor;
                if (_highlightRect != null)
                {
                    _highlightRect.localScale = Vector3.one;
                }
            });

            if (_highlightRect != null)
            {
                sequence.Join(_highlightRect.DOPunchScale(Vector3.one * scalePunch, pulseDuration, 8, 0.7f).SetUpdate(true).SetLink(gameObject));
            }

            sequence.Append(highlightGraphic.DOColor(_isHighlighted ? highlightColor : _hiddenColor, pulseDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject));
            if (_highlightRect != null)
            {
                sequence.Join(_highlightRect.DOScale(_isHighlighted ? Vector3.one * highlightScale : Vector3.one, pulseDuration).SetEase(Ease.OutQuad).SetUpdate(true).SetLink(gameObject));
            }
        }
    }
}
