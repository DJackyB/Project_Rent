using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Drag;
using DG.Tweening;
using Martian.Tooltip;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 卡牌拖拽控制器，处理手牌的拖拽输入、位置更新和放置验证。
    /// 管理拖拽期间的视觉反馈（动画、高亮）和占位符显示。
    /// 验证放置目标有效性，支持返回手牌或完成打出卡牌的流程。
    /// 使用 DOTween 处理动画、EventSystem 进行射线检测。
    /// </summary>
    public class UICardDragController : MonoBehaviour
    {
        private static UICardDragController _instance;

        [Header("Scene References")]
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private UICardDropZone playAreaDropZone;

        [Header("Drag Motion")]
        [SerializeField] private float returnDuration = 0.18f;
        [SerializeField] private float successDuration = 0.18f;
        [SerializeField] private float invalidShakeDuration = 0.14f;
        [SerializeField] private float invalidShakeStrength = 18f;

        private readonly List<RaycastResult> _raycastResults = new();

        private Canvas _rootCanvas;
        private GraphicRaycaster _graphicRaycaster;
        private UICardDragHandler _activeHandler;
        private RectTransform _activeRect;
        private CanvasGroup _activeCanvasGroup;
        private Transform _originalParent;
        private int _originalSiblingIndex;
        private Vector2 _originalAnchorMin;
        private Vector2 _originalAnchorMax;
        private Vector2 _originalPivot;
        private GameObject _placeholderObject;
        private UICardDropZone _currentZone;
        private bool _isCompletingPlay;

        public static UICardDragController Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            ResolveSceneReferences();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public bool IsDragging(UICardDragHandler handler)
        {
            return _activeHandler != null && _activeHandler == handler;
        }

        public void BindDragLayer(RectTransform layer)
        {
            dragLayer = layer;
            ResolveSceneReferences();
        }

        public void CancelCurrentDrag(bool immediate)
        {
            CancelActiveDrag(immediate);
        }

        public void BeginDrag(UICardDragHandler handler, PointerEventData eventData)
        {
            if (handler == null || handler.CardView == null || handler.CardView.Card == null)
            {
                return;
            }

            GamePhase currentPhase = TurnManager.Instance != null
                ? TurnManager.Instance.CurrentPhase
                : UIManager.Instance != null
                    ? UIManager.Instance.CurrentPhase
                    : GamePhase.Prepare;

            if (currentPhase != GamePhase.Action)
            {
                return;
            }

            ResolveSceneReferences();
            if (dragLayer == null)
            {
                Debug.LogError("[UICardDragController] Missing DragLayer.");
                return;
            }

            if (_activeHandler != null && _activeHandler != handler)
            {
                CancelActiveDrag(true);
            }

            TooltipServices.Current.HideAll();

            _activeHandler = handler;
            _activeRect = handler.RectTransform;
            _activeCanvasGroup = handler.CanvasGroup;
            _originalParent = _activeRect.parent;
            _originalSiblingIndex = _activeRect.GetSiblingIndex();
            _originalAnchorMin = _activeRect.anchorMin;
            _originalAnchorMax = _activeRect.anchorMax;
            _originalPivot = _activeRect.pivot;

            CreatePlaceholder();

            _activeRect.SetParent(dragLayer, true);
            _activeRect.SetAsLastSibling();
            _activeRect.anchorMin = new Vector2(0.5f, 0.5f);
            _activeRect.anchorMax = new Vector2(0.5f, 0.5f);
            _activeRect.pivot = new Vector2(0.5f, 0.5f);

            _activeCanvasGroup.blocksRaycasts = false;
            _activeCanvasGroup.interactable = false;
            handler.SetDragVisualState(true);

            UpdateDragPosition(eventData);
            UpdateHoveredZone(eventData);
        }

        public void UpdateDrag(PointerEventData eventData)
        {
            if (_activeHandler == null || eventData == null)
            {
                return;
            }

            UpdateDragPosition(eventData);
            UpdateHoveredZone(eventData);
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (_activeHandler == null)
            {
                return;
            }

            UpdateHoveredZone(eventData);

            CardPlayValidationResult validation;
            if (TryResolveValidDropZone(_currentZone, out validation))
            {
                PlayIntoZone(validation, _currentZone);
                return;
            }

            ReturnToHand(true);
        }

        public void RequestDrop(UICardDropZone zone)
        {
            if (_activeHandler == null)
            {
                return;
            }

            _currentZone = zone;
        }

        public void NotifySourceDisabled(UICardDragHandler handler)
        {
            if (_activeHandler != null && _activeHandler == handler)
            {
                CancelActiveDrag(true);
            }
        }

        public void CancelActiveDrag(bool immediate)
        {
            if (_activeHandler == null || _isCompletingPlay)
            {
                return;
            }

            if (immediate)
            {
                RestoreToHandImmediate();
            }
            else
            {
                ReturnToHand(false);
            }
        }

        private void ResolveSceneReferences()
        {
            if (_rootCanvas == null)
            {
                _rootCanvas = GetComponent<Canvas>();
                if (_rootCanvas == null)
                {
                    _rootCanvas = GetComponentInParent<Canvas>();
                }
            }

            if (_graphicRaycaster == null && _rootCanvas != null)
            {
                _graphicRaycaster = _rootCanvas.GetComponent<GraphicRaycaster>();
            }

            if (dragLayer == null)
            {
                Transform dragLayerTransform = null;
                if (_rootCanvas != null)
                {
                    dragLayerTransform = _rootCanvas.transform.Find("DragLayer");
                }

                if (dragLayerTransform == null)
                {
                    dragLayerTransform = transform.Find("DragLayer");
                }

                if (dragLayerTransform != null)
                {
                    dragLayer = dragLayerTransform as RectTransform;
                    if (dragLayer != null)
                    {
                        dragLayer.SetAsLastSibling();
                    }
                }
                else
                {
                    Debug.LogError("[UICardDragController] dragLayer is not assigned in the Inspector and no DragLayer was found in the scene. Create a DragLayer child under the Canvas and assign it.");
                }
            }

            if (playAreaDropZone == null && UIManager.Instance != null && UIManager.Instance.boardPanel != null)
            {
                playAreaDropZone = UIManager.Instance.boardPanel.playAreaDropZone;
            }
        }

        private void CreatePlaceholder()
        {
            if (_placeholderObject != null || _originalParent == null || _activeRect == null)
            {
                return;
            }

            _placeholderObject = new GameObject($"{_activeRect.name}_Placeholder", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            var placeholderRect = _placeholderObject.transform as RectTransform;
            placeholderRect.SetParent(_originalParent, false);
            placeholderRect.SetSiblingIndex(_originalSiblingIndex);
            placeholderRect.localScale = Vector3.one;
            placeholderRect.anchorMin = _originalAnchorMin;
            placeholderRect.anchorMax = _originalAnchorMax;
            placeholderRect.pivot = _originalPivot;
            placeholderRect.sizeDelta = _activeRect.sizeDelta;

            var layout = _placeholderObject.GetComponent<LayoutElement>();
            float width = _activeRect.rect.width;
            float height = _activeRect.rect.height;
            if (_activeHandler != null && _activeHandler.LayoutElement != null)
            {
                width = _activeHandler.LayoutElement.preferredWidth > 0f ? _activeHandler.LayoutElement.preferredWidth : width;
                height = _activeHandler.LayoutElement.preferredHeight > 0f ? _activeHandler.LayoutElement.preferredHeight : height;
            }

            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;

            var image = _placeholderObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            image.raycastTarget = false;
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_activeRect == null || dragLayer == null || eventData == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, eventData.pressEventCamera, out var localPoint))
            {
                _activeRect.anchoredPosition = localPoint;
            }
        }

        private void UpdateHoveredZone(PointerEventData eventData)
        {
            var nextZone = ResolveDropZone(eventData);
            CardPlayValidationResult validation;
            bool isValid = TryResolveValidDropZone(nextZone, out validation);

            if (_currentZone != nextZone)
            {
                if (_currentZone != null)
                {
                    _currentZone.SetHighlighted(false);
                }

                _currentZone = nextZone;
            }

            if (_currentZone != null)
            {
                _currentZone.SetHighlighted(isValid);
            }
        }

        private UICardDropZone ResolveDropZone(PointerEventData eventData)
        {
            if (_graphicRaycaster == null || EventSystem.current == null || eventData == null)
            {
                return null;
            }

            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);

            for (int i = 0; i < _raycastResults.Count; i++)
            {
                var zone = _raycastResults[i].gameObject.GetComponentInParent<UICardDropZone>();
                if (zone != null)
                {
                    return zone;
                }
            }

            return null;
        }

        private bool TryResolveValidDropZone(UICardDropZone zone, out CardPlayValidationResult validation)
        {
            validation = default;
            if (_activeHandler == null || _activeHandler.CardView == null || _activeHandler.CardView.Card == null || TurnManager.Instance == null)
            {
                return false;
            }

            CardPlayTargetKind requiredTarget = TurnManager.Instance.GetRequiredTargetKind(_activeHandler.CardView.Card);
            if (zone == null)
            {
                validation = CardPlayValidationResult.Failure(
                    requiredTarget == CardPlayTargetKind.Room ? CardPlayBlockReason.MissingTarget : CardPlayBlockReason.InvalidTarget,
                    requiredTarget);
                return false;
            }

            if (zone.ZoneKind != requiredTarget)
            {
                validation = CardPlayValidationResult.Failure(CardPlayBlockReason.InvalidTarget, requiredTarget, zone.BoundRoom);
                return false;
            }

            RoomSlot targetRoom = zone.ZoneKind == CardPlayTargetKind.Room ? zone.BoundRoom : null;
            validation = TurnManager.Instance.ValidatePlay(_activeHandler.CardView.Card, targetRoom);
            return validation.IsValid;
        }

        private void ReturnToHand(bool shakeFirst)
        {
            if (_activeRect == null)
            {
                RestoreToHandImmediate();
                return;
            }

            SetZoneHighlight(false);
            _activeRect.DOKill(false);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (shakeFirst)
            {
                sequence.Append(_activeRect.DOShakeAnchorPos(invalidShakeDuration, invalidShakeStrength, 18, 90f, false, true));
            }

            Vector3 targetWorldPosition = _placeholderObject != null
                ? _placeholderObject.transform.position
                : _activeRect.position;

            sequence.Append(_activeRect.DOMove(targetWorldPosition, returnDuration).SetEase(Ease.OutCubic));
            sequence.Join(_activeRect.DOScale(Vector3.one, returnDuration).SetEase(Ease.OutQuad));
            sequence.OnComplete(RestoreToHandImmediate);
        }

        private void RestoreToHandImmediate()
        {
            if (_activeRect == null)
            {
                ClearActiveState(false);
                return;
            }

            SetZoneHighlight(false);

            if (_originalParent != null)
            {
                _activeRect.SetParent(_originalParent, true);
                _activeRect.SetSiblingIndex(Mathf.Min(_originalSiblingIndex, _originalParent.childCount));
                _activeRect.anchorMin = _originalAnchorMin;
                _activeRect.anchorMax = _originalAnchorMax;
                _activeRect.pivot = _originalPivot;
            }

            if (_activeCanvasGroup != null)
            {
                _activeCanvasGroup.alpha = 1f;
                _activeCanvasGroup.blocksRaycasts = true;
                _activeCanvasGroup.interactable = true;
            }

            if (_originalParent is RectTransform originalRect)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(originalRect);
            }

            if (_activeHandler != null)
            {
                _activeHandler.ResetToIdleVisual(false);
            }

            ClearActiveState(true);
        }

        private void PlayIntoZone(CardPlayValidationResult validation, UICardDropZone zone)
        {
            if (_activeRect == null || _activeHandler == null || zone == null)
            {
                RestoreToHandImmediate();
                return;
            }

            SetZoneHighlight(true);

            _activeRect.DOKill(false);
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(_activeRect.DOMove(zone.DropAnchor.position, successDuration).SetEase(Ease.OutCubic));
            sequence.Join(_activeRect.DOScale(Vector3.one * 0.92f, successDuration).SetEase(Ease.OutQuad));
            if (_activeCanvasGroup != null)
            {
                sequence.Join(_activeCanvasGroup.DOFade(0.35f, successDuration * 0.85f).SetEase(Ease.OutQuad));
            }

            sequence.OnComplete(() => CommitPlay(validation));
        }

        private void CommitPlay(CardPlayValidationResult validation)
        {
            if (_activeHandler == null || _activeHandler.CardView == null || _activeHandler.CardView.Card == null)
            {
                RestoreToHandImmediate();
                return;
            }

            var draggedObject = _activeHandler.gameObject;
            var card = _activeHandler.CardView.Card;
            RoomSlot targetRoom = validation.RequiredTargetKind == CardPlayTargetKind.Room ? validation.TargetRoom : null;

            _isCompletingPlay = true;
            bool played = TurnManager.Instance != null && TurnManager.Instance.PlayCard(card, targetRoom);
            _isCompletingPlay = false;

            if (!played)
            {
                if (_activeCanvasGroup != null)
                {
                    _activeCanvasGroup.alpha = 1f;
                    _activeCanvasGroup.blocksRaycasts = false;
                }

                ReturnToHand(true);
                return;
            }

            SetZoneHighlight(false);
            ClearActiveState(true);

            if (draggedObject != null)
            {
                Destroy(draggedObject);
            }
        }

        private void ClearActiveState(bool destroyPlaceholder)
        {
            if (destroyPlaceholder && _placeholderObject != null)
            {
                Destroy(_placeholderObject);
            }

            _placeholderObject = null;
            _currentZone = null;
            _activeHandler = null;
            _activeRect = null;
            _activeCanvasGroup = null;
            _originalParent = null;
            _originalSiblingIndex = 0;
            _originalAnchorMin = Vector2.zero;
            _originalAnchorMax = Vector2.one;
            _originalPivot = new Vector2(0.5f, 0.5f);
        }

        private void SetZoneHighlight(bool highlighted)
        {
            if (_currentZone != null)
            {
                _currentZone.SetHighlighted(highlighted);
            }
        }
    }
}
