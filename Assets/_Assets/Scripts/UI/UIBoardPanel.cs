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
        [SerializeField] private float destroyFlashSeconds = 0.08f;
        [SerializeField] private float destroyMoveSeconds = 0.28f;
        [SerializeField] private float destroyExitYOffset = 60f;

        private readonly List<UIRoomView> _roomViews = new();
        private readonly List<UICardView> _contractViews = new();
        private readonly Dictionary<RoomSlot, UIRoomView> _roomLookup = new();
        private readonly Dictionary<CardInstance, UICardView> _contractLookup = new();

        private Transform _contractContainer;
        private RectTransform _destroyAnimationLayer;
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

        public RectTransform ResolveContractAnchor(CardInstance card)
        {
            if (card != null && _contractLookup.TryGetValue(card, out var contractView))
            {
                return contractView.HoverAnchor;
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
                var obj = contractView.gameObject;
                _contractLookup.Remove(evt.Card);
                _contractViews.Remove(contractView);
                obj.transform.SetParent(null, true);
                StartCoroutine(PlayDestroyAnimation(obj));
            }
        }

        private IEnumerator PlayDestroyAnimation(GameObject cardObject)
        {
            if (cardObject == null)
            {
                yield break;
            }

            _pendingDestroyAnimations++;
            EnsureDestroyAnimationLayer();
            if (_destroyAnimationLayer != null)
            {
                cardObject.transform.SetParent(_destroyAnimationLayer, true);
            }

            var rect = cardObject.transform as RectTransform;
            var canvasGroup = cardObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = cardObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 红闪 + 下沉 + 缩小 + 淡出
            var graphics = cardObject.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
            var originalColors = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
            {
                originalColors[i] = graphics[i].color;
            }

            var seq = DOTween.Sequence().SetUpdate(true).SetLink(cardObject, LinkBehaviour.KillOnDestroy);

            // 红闪
            foreach (var g in graphics)
            {
                seq.Join(g.DOColor(new Color(1f, 0.2f, 0.2f, g.color.a), destroyFlashSeconds)
                    .SetEase(Ease.OutQuad)
                    .SetLink(cardObject, LinkBehaviour.KillOnDestroy));
            }

            seq.AppendInterval(destroyFlashSeconds * 0.5f);

            // 缩小 + 下沉 + 淡出
            if (rect != null)
            {
                seq.Join(rect.DOScale(Vector3.zero, destroyMoveSeconds).SetEase(Ease.InBack).SetLink(cardObject, LinkBehaviour.KillOnDestroy));
                seq.Join(rect.DOMove(rect.position + new Vector3(0f, -destroyExitYOffset, 0f), destroyMoveSeconds).SetEase(Ease.InCubic).SetLink(cardObject, LinkBehaviour.KillOnDestroy));
            }

            seq.Join(canvasGroup.DOFade(0f, destroyMoveSeconds * 0.6f).SetEase(Ease.InQuad).SetLink(cardObject, LinkBehaviour.KillOnDestroy));

            yield return seq.WaitForCompletion();

            _pendingDestroyAnimations--;
            if (cardObject != null)
            {
                Destroy(cardObject);
            }
        }

        private void EnsureDestroyAnimationLayer()
        {
            if (_destroyAnimationLayer != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            var layerObject = new GameObject("BoardDestroyAnimationLayer", typeof(RectTransform), typeof(CanvasGroup));
            layerObject.transform.SetParent(canvas.transform, false);

            _destroyAnimationLayer = layerObject.GetComponent<RectTransform>();
            _destroyAnimationLayer.anchorMin = Vector2.zero;
            _destroyAnimationLayer.anchorMax = Vector2.one;
            _destroyAnimationLayer.offsetMin = Vector2.zero;
            _destroyAnimationLayer.offsetMax = Vector2.zero;

            var cg = layerObject.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
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

                roomView.Setup(rooms[i], cardPrefab, this);
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

            var contracts = BoardManager.Instance.GetAllContracts();
            for (int i = 0; i < contracts.Count; i++)
            {
                var contractObject = Instantiate(cardPrefab, _contractContainer);
                var cardView = contractObject.GetComponent<UICardView>();
                if (cardView == null)
                {
                    Debug.LogError("[UIBoardPanel] Card prefab requires UICardView.");
                    continue;
                }

                cardView.Setup(contracts[i], CardViewContext.Contract, null);
                _contractViews.Add(cardView);
                _contractLookup[contracts[i]] = cardView;
            }

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

            if (container.GetComponent<ContentSizeFitter>() == null)
            {
                Debug.LogError(
                    $"[UIBoardPanel] ContractContainer '{container.name}' is missing ContentSizeFitter. Add and configure it in the scene.",
                    container);
                return false;
            }

            return true;
        }
    }
}
