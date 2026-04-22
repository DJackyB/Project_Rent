using System.Collections;
using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.Core;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Drag;
using DG.Tweening;
using Martian.EventBus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    /// <summary>
    /// 房间面板，负责显示棋盘上的所有房间和已承诺合约。
    /// 为每个房间创建房间视图及其内部的租户和装备插槽。
    /// 管理放置区域（PlayArea）的拖拽验证，提供锚点解析用于拖拽反馈。
    /// </summary>
    public class UIBoardPanel : MonoBehaviour
    {
        [Header("Optional Scene References")]
        public GameObject roomPrefab;
        public Transform roomContainer;
        public GameObject roomCardEntryPrefab;
        public UICardDropZone playAreaDropZone;
        [SerializeField] private RectTransform contractPanelRoot;
        [SerializeField] private Transform contractContainer;
        [SerializeField] private TextMeshProUGUI contractTitleText;

        [Header("Destroy Animation")]
        [SerializeField] private RectTransform destroyAnimationLayer;
        [SerializeField] private Color destroyFlashColor = new Color(1f, 0.18f, 0.18f, 1f);
        [SerializeField] private float destroyPunchStrength = 0.14f;
        [SerializeField] private float destroyPunchSeconds = 0.1f;
        [SerializeField] private float destroyFlashSeconds = 0.09f;
        [SerializeField] private float destroyHoldSeconds = 0.06f;
        [SerializeField] private float destroyCollapseSeconds = 0.32f;
        [SerializeField] private float destroyExitYOffset = 72f;
        [SerializeField] private float destroyRotateDegrees = -12f;

        private readonly List<UIRoomView> _roomViews = new();
        private readonly List<UIRoomSlotView> _contractViews = new();
        private readonly Dictionary<RoomSlot, UIRoomView> _roomLookup = new();
        private readonly Dictionary<CardInstance, UIRoomSlotView> _contractLookup = new();

        private Transform _contractContainer;
        private int _pendingDestroyAnimations;

        public bool HasDestroyAnimations => _pendingDestroyAnimations > 0;

        private void OnEnable()
        {
            EventBus.Subscribe<GameEvents.CardDestroyed>(OnCardDestroyed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameEvents.CardDestroyed>(OnCardDestroyed);
        }

        public void RefreshBoard()
        {
            EnsurePlayAreaDropZone();
            RefreshContractPanelText();

            var cardPrefab = ResolveCardPrefab();
            if (cardPrefab == null)
            {
                Debug.LogError("[UIBoardPanel] Missing card prefab.");
                return;
            }

            RefreshRooms(cardPrefab);
            RefreshContracts(cardPrefab);
        }

        public RectTransform ResolveRoomAnchor(RoomSlot room)
        {
            if (room != null && _roomLookup.TryGetValue(room, out var roomView))
            {
                return roomView.DropAnchor;
            }

            return roomContainer as RectTransform ?? transform as RectTransform;
        }

        public RectTransform ResolveRoomCardAnchor(RoomSlot room, CardInstance card)
        {
            if (room != null && card != null && _roomLookup.TryGetValue(room, out var roomView) && roomView != null)
            {
                var cardAnchor = roomView.ResolveCardAnchor(card);
                if (cardAnchor != null)
                {
                    return cardAnchor;
                }
            }

            return ResolveRoomAnchor(room);
        }

        public RectTransform ResolveContractAnchor(CardInstance card)
        {
            if (card != null && _contractLookup.TryGetValue(card, out var contractView) && contractView != null)
            {
                var cardView = contractView.CurrentCardView;
                if (cardView != null)
                {
                    return cardView.HoverAnchor;
                }

                return contractView.transform as RectTransform;
            }

            return _contractContainer as RectTransform ?? transform as RectTransform;
        }

        public RectTransform ResolvePlayAreaAnchor()
        {
            return playAreaDropZone != null ? playAreaDropZone.DropAnchor : transform as RectTransform;
        }

        private void OnCardDestroyed(GameEvents.CardDestroyed evt)
        {
            if (evt.Card == null)
            {
                return;
            }

            // 先在房间里找
            foreach (var roomView in _roomViews)
            {
                var stolen = roomView.TryStealCardObjectForAnimation(evt.Card);
                if (stolen != null)
                {
                    StartCoroutine(PlayDestroyAnimation(stolen));
                    return;
                }
            }

            // 再在合同区找
            if (_contractLookup.TryGetValue(evt.Card, out var contractView) && contractView != null)
            {
                var obj = contractView.StealCardObject() ?? contractView.gameObject;
                _contractLookup.Remove(evt.Card);
                _contractViews.Remove(contractView);
                if (obj != contractView.gameObject)
                {
                    Destroy(contractView.gameObject);
                }
                StartCoroutine(PlayDestroyAnimation(obj));
            }
        }

        private IEnumerator PlayDestroyAnimation(GameObject cardObject)
        {
            if (cardObject == null) yield break;

            if (destroyAnimationLayer == null)
            {
                Debug.LogError("[UIBoardPanel] destroyAnimationLayer is not assigned.", this);
                Destroy(cardObject);
                yield break;
            }

            _pendingDestroyAnimations++;

            var rect = cardObject.transform as RectTransform;

            // 在挂到动画层之前捕获：
            // - worldPos：以原始 pivot 为基准的世界坐标（不改 pivot 就不会偏移）
            // - localScale：原始相对于父级的局部缩放（lossyScale 含 Canvas 累积缩放，不能直接用）
            Vector3 worldPos = rect != null ? (Vector3)rect.position : cardObject.transform.position;
            Vector3 localScale = cardObject.transform.localScale;

            // 挂到动画层：只改 anchor 到中心，不改 pivot（pivot 改变会使 worldPos 参照点偏移）
            cardObject.transform.SetParent(destroyAnimationLayer, false);
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.position = worldPos;   // 还原位置（参照点与 pivot 一致，无偏移）
                rect.localScale = localScale; // 还原局部缩放（不含 Canvas 累积，不会叠加）
            }

            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = cardObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            var graphics = cardObject.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(cardObject, LinkBehaviour.KillOnDestroy);

            // Phase 1: 冲击感 punch + 红闪同时触发
            if (rect != null)
            {
                seq.Append(rect.DOPunchScale(Vector3.one * destroyPunchStrength, destroyPunchSeconds, 6, 0.4f)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
            }
            foreach (var g in graphics)
            {
                seq.Join(g.DOColor(new Color(destroyFlashColor.r, destroyFlashColor.g, destroyFlashColor.b, g.color.a),
                        destroyFlashSeconds)
                    .SetEase(Ease.OutQuad)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
            }

            // Phase 2: 红色停留一拍，让玩家看清
            seq.AppendInterval(destroyHoldSeconds);

            // Phase 3: 旋转跌落 + 缩到零 + 淡出（带回弹感的 InBack）
            if (rect != null)
            {
                seq.Append(rect.DOScale(Vector3.zero, destroyCollapseSeconds)
                    .SetEase(Ease.InBack)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                seq.Join(rect.DOAnchorPos(rect.anchoredPosition + new Vector2(0f, -destroyExitYOffset), destroyCollapseSeconds)
                    .SetEase(Ease.InCubic)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                seq.Join(rect.DOLocalRotate(new Vector3(0f, 0f, destroyRotateDegrees), destroyCollapseSeconds)
                    .SetEase(Ease.InQuad)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
            }
            seq.Join(canvasGroup.DOFade(0f, destroyCollapseSeconds * 0.65f)
                .SetEase(Ease.InQuad)
                .SetLink(cardObject, LinkBehaviour.KillOnDestroy));

            yield return seq.WaitForCompletion();

            _pendingDestroyAnimations--;
            if (cardObject != null) Destroy(cardObject);
        }

        private void EnsurePlayAreaDropZone()
        {
            if (playAreaDropZone == null)
            {
                var zoneTransform = transform.Find("PlayAreaDropZone");
                if (zoneTransform == null)
                {
                    var zoneObject = new GameObject("PlayAreaDropZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(UICardDropZone));
                    zoneObject.transform.SetParent(transform, false);
                    zoneTransform = zoneObject.transform;

                    var zoneRect = zoneTransform as RectTransform;
                    zoneRect.anchorMin = new Vector2(0.5f, 0f);
                    zoneRect.anchorMax = new Vector2(0.5f, 0f);
                    zoneRect.pivot = new Vector2(0.5f, 0f);
                    zoneRect.anchoredPosition = new Vector2(0f, 24f);
                    zoneRect.sizeDelta = new Vector2(280f, 72f);

                    var zoneImage = zoneObject.GetComponent<Image>();
                    zoneImage.color = new Color(0.18f, 0.26f, 0.33f, 0.36f);
                    zoneImage.type = Image.Type.Sliced;
                }

                playAreaDropZone = zoneTransform.GetComponent<UICardDropZone>();
            }

            playAreaDropZone.ZoneKind = CardPlayTargetKind.PlayArea;
            playAreaDropZone.BindRoom(null);
            playAreaDropZone.AssignRuntimeReferences(playAreaDropZone.transform as RectTransform, null);
            playAreaDropZone.SetHighlighted(false, true);
        }

        private void RefreshRooms(GameObject cardPrefab)
        {
            var container = roomContainer != null ? roomContainer : transform;
            ClearContainer(container);
            _roomViews.Clear();
            _roomLookup.Clear();

            var rooms = BoardManager.Instance.GetAllRooms();
            for (int i = 0; i < rooms.Count; i++)
            {
                if (roomPrefab == null)
                {
                    Debug.LogError("[UIBoardPanel] Missing room prefab.");
                    break;
                }

                var roomObject = Instantiate(roomPrefab, container);
                var roomView = roomObject.GetComponent<UIRoomView>();
                if (roomView == null)
                {
                    Debug.LogError("[UIBoardPanel] roomPrefab requires UIRoomView.");
                    continue;
                }

                roomView.Setup(rooms[i], cardPrefab);
                _roomViews.Add(roomView);
                _roomLookup[rooms[i]] = roomView;
            }
        }

        private void RefreshContracts(GameObject cardPrefab)
        {
            if (!EnsureContractPanel())
            {
                return;
            }

            ClearContainer(_contractContainer);
            _contractViews.Clear();
            _contractLookup.Clear();

            var slotPrefab = ResolveContractSlotPrefab();
            if (slotPrefab == null)
            {
                Debug.LogError("[UIBoardPanel] Missing contract slot prefab. Reuse CardSlot.prefab via roomPrefab or assign room prefab correctly.");
                return;
            }

            var contracts = BoardManager.Instance.GetAllContracts();
            for (int i = 0; i < contracts.Count; i++)
            {
                var slotObject = Instantiate(slotPrefab, _contractContainer, false);
                slotObject.name = $"ContractSlot_{i}";

                var slotView = slotObject.GetComponent<UIRoomSlotView>();
                if (slotView == null)
                {
                    Debug.LogError("[UIBoardPanel] Contract slot prefab requires UIRoomSlotView.");
                    continue;
                }

                slotView.Setup(CardViewContext.Contract, cardPrefab);
                slotView.Bind(contracts[i]);
                _contractViews.Add(slotView);
                _contractLookup[contracts[i]] = slotView;
            }

            ApplyContractSlotLayout(cardPrefab);
            RefreshContractPanelText();
        }

        private GameObject ResolveCardPrefab()
        {
            if (UIManager.Instance != null && UIManager.Instance.handPanel != null)
            {
                return UIManager.Instance.handPanel.cardPrefab;
            }

            return roomCardEntryPrefab;
        }

        private GameObject ResolveContractSlotPrefab()
        {
            if (roomPrefab == null)
            {
                return null;
            }

            var roomView = roomPrefab.GetComponent<UIRoomView>();
            return roomView != null ? roomView.SlotPrefab : null;
        }

        private bool EnsureContractPanel()
        {
            if (_contractContainer != null)
            {
                RefreshContractPanelText();
                return true;
            }

            if (contractPanelRoot == null)
            {
                Debug.LogError("[UIBoardPanel] contractPanelRoot is not assigned. Please wire the ContractPanel RectTransform in the scene.", this);
                return false;
            }

            if (contractTitleText == null)
            {
                Debug.LogError("[UIBoardPanel] contractTitleText is not assigned. Please wire the ContractPanel title text in the scene.", contractPanelRoot);
                return false;
            }

            if (contractContainer == null)
            {
                Debug.LogError("[UIBoardPanel] contractContainer is not assigned. Please wire the ContractContainer transform in the scene.", contractPanelRoot);
                return false;
            }

            if (!EnsureContractContainerLayout(contractContainer))
            {
                return false;
            }

            _contractContainer = contractContainer;
            RefreshContractPanelText();
            return true;
        }

        private void RefreshContractPanelText()
        {
            if (contractPanelRoot == null)
            {
                return;
            }

            var titleText = contractTitleText != null
                ? contractTitleText
                : contractPanelRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = GameText.Contracts;
                contractTitleText = titleText;
            }
        }

        private void ApplyContractSlotLayout(GameObject cardPrefab)
        {
            var containerRect = _contractContainer as RectTransform;
            if (containerRect == null || _contractViews.Count <= 0)
            {
                return;
            }

            var layoutGroup = _contractContainer.GetComponent<HorizontalLayoutGroup>();
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            ResolveCardDesignSize(cardPrefab, out float designWidth, out float designHeight);
            if (designWidth <= 0f || designHeight <= 0f)
            {
                return;
            }

            float ratio = designWidth / designHeight;
            float availableWidth = containerRect.rect.width;
            float availableHeight = containerRect.rect.height;
            float spacing = 0f;

            if (layoutGroup != null)
            {
                availableWidth -= layoutGroup.padding.horizontal;
                availableHeight -= layoutGroup.padding.vertical;
                spacing = layoutGroup.spacing;
            }

            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            float maxWidthPerSlot = (availableWidth - spacing * (_contractViews.Count - 1)) / _contractViews.Count;
            if (maxWidthPerSlot <= 0f)
            {
                return;
            }

            float targetHeight = availableHeight;
            float targetWidth = targetHeight * ratio;

            if (targetWidth > maxWidthPerSlot)
            {
                targetWidth = maxWidthPerSlot;
                targetHeight = targetWidth / ratio;
            }

            for (int i = 0; i < _contractViews.Count; i++)
            {
                var slotView = _contractViews[i];
                if (slotView == null)
                {
                    continue;
                }

                var slotRect = slotView.transform as RectTransform;
                if (slotRect == null)
                {
                    continue;
                }

                var layout = slotRect.GetComponent<LayoutElement>();
                if (layout == null)
                {
                    Debug.LogError("[UIBoardPanel] Contract slot prefab is missing LayoutElement.", slotRect);
                    continue;
                }

                layout.preferredWidth = targetWidth;
                layout.preferredHeight = targetHeight;
                slotRect.sizeDelta = new Vector2(targetWidth, targetHeight);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            for (int i = 0; i < _contractViews.Count; i++)
            {
                _contractViews[i]?.RefreshCardScale();
            }
        }

        private static void ResolveCardDesignSize(GameObject cardPrefab, out float width, out float height)
        {
            width = 120f;
            height = 200f;

            if (cardPrefab == null)
            {
                return;
            }

            var cardRect = cardPrefab.transform as RectTransform;
            var layout = cardPrefab.GetComponent<LayoutElement>();

            if (layout != null)
            {
                if (layout.preferredWidth > 0f)
                {
                    width = layout.preferredWidth;
                }

                if (layout.preferredHeight > 0f)
                {
                    height = layout.preferredHeight;
                }
            }

            if (cardRect != null)
            {
                if (width <= 0f && cardRect.rect.width > 0f)
                {
                    width = cardRect.rect.width;
                }

                if (height <= 0f && cardRect.rect.height > 0f)
                {
                    height = cardRect.rect.height;
                }
            }
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

        private static bool EnsureContractContainerLayout(Transform container)
        {
            if (container == null)
            {
                return false;
            }

            var layout = container.GetComponent<LayoutGroup>();
            if (layout == null)
            {
                Debug.LogError(
                    $"[UIBoardPanel] ContractContainer '{container.name}' is missing a layout component. Add a HorizontalLayoutGroup or VerticalLayoutGroup in the scene.",
                    container);
                return false;
            }

            return true;
        }
    }
}
