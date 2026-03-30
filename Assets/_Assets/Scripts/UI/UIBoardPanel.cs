using System.Collections.Generic;
using BaoZuPo.Board;
using BaoZuPo.Card;
using BaoZuPo.GameFlow;
using BaoZuPo.UI.Common.Drag;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BaoZuPo.UI
{
    public class UIBoardPanel : MonoBehaviour
    {
        [Header("Optional Scene References")]
        public GameObject roomPrefab;
        public Transform roomContainer;
        public GameObject roomCardEntryPrefab;
        public UICardDropZone playAreaDropZone;
        [SerializeField] private TextMeshProUGUI playAreaLabel;
        [SerializeField] private RectTransform contractPanelRoot;
        [SerializeField] private Transform contractContainer;
        [SerializeField] private TextMeshProUGUI contractTitleText;

        private readonly List<UIRoomView> _roomViews = new();
        private readonly List<UICardView> _contractViews = new();
        private readonly Dictionary<RoomSlot, UIRoomView> _roomLookup = new();
        private readonly Dictionary<CardInstance, UICardView> _contractLookup = new();

        private Transform _contractContainer;

        public void RefreshBoard()
        {
            EnsurePlayAreaDropZone();
            RefreshContractPanelLocalization();

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

                    var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                    labelObject.transform.SetParent(zoneTransform, false);

                    var labelRect = labelObject.GetComponent<RectTransform>();
                    labelRect.anchorMin = Vector2.zero;
                    labelRect.anchorMax = Vector2.one;
                    labelRect.offsetMin = Vector2.zero;
                    labelRect.offsetMax = Vector2.zero;

                    var label = labelObject.GetComponent<TextMeshProUGUI>();
                    label.font = UIFontCatalog.GetPreferredFontAsset();
                    label.text = UIStrings.PlayArea;
                    label.fontSize = 24f;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = Color.white;
                    label.raycastTarget = false;
                }

                playAreaDropZone = zoneTransform.GetComponent<UICardDropZone>();
            }

            var zoneLabel = playAreaLabel != null
                ? playAreaLabel
                : playAreaDropZone != null
                    ? playAreaDropZone.GetComponentInChildren<TextMeshProUGUI>(true)
                    : null;
            if (zoneLabel != null)
            {
                UIFontCatalog.ApplyToText(zoneLabel);
                zoneLabel.text = UIStrings.PlayArea;
                playAreaLabel = zoneLabel;
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
            EnsureContractPanel();
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

            RefreshContractPanelLocalization();
        }

        private GameObject ResolveCardPrefab()
        {
            if (UIManager.Instance != null && UIManager.Instance.handPanel != null)
            {
                return UIManager.Instance.handPanel.cardPrefab;
            }

            return roomCardEntryPrefab;
        }

        private void EnsureContractPanel()
        {
            if (_contractContainer != null)
            {
                RefreshContractPanelLocalization();
                return;
            }

            if (contractContainer != null)
            {
                _contractContainer = contractContainer;
                EnsureContractContainerLayout(_contractContainer);
                RefreshContractPanelLocalization();
                return;
            }

            if (contractPanelRoot == null)
            {
                var existingRoot = transform.Find("ContractPanel");
                if (existingRoot != null)
                {
                    contractPanelRoot = existingRoot as RectTransform;
                }
            }

            if (contractPanelRoot != null)
            {
                if (contractTitleText == null)
                {
                    contractTitleText = contractPanelRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
                }

                if (contractContainer == null)
                {
                    contractContainer = contractPanelRoot.Find("ContractContainer");
                }

                if (contractContainer != null)
                {
                    _contractContainer = contractContainer;
                    EnsureContractContainerLayout(_contractContainer);
                    RefreshContractPanelLocalization();
                    return;
                }
            }

            var panelRoot = new GameObject("ContractPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(transform, false);
            contractPanelRoot = panelRoot.GetComponent<RectTransform>();

            var panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -72f);
            panelRect.sizeDelta = new Vector2(240f, 420f);

            var panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = new Color(0.10f, 0.12f, 0.15f, 0.72f);
            panelImage.raycastTarget = false;

            var titleObject = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            titleObject.transform.SetParent(panelRoot.transform, false);

            var titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(12f, -40f);
            titleRect.offsetMax = new Vector2(-12f, -12f);

            var titleText = titleObject.GetComponent<TextMeshProUGUI>();
            titleText.font = UIFontCatalog.GetPreferredFontAsset();
            titleText.text = UIStrings.Contracts;
            titleText.fontSize = 22f;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            titleText.raycastTarget = false;
            contractTitleText = titleText;

            var listObject = new GameObject("ContractContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listObject.transform.SetParent(panelRoot.transform, false);
            _contractContainer = listObject.transform;
            contractContainer = _contractContainer;

            var listRect = listObject.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.offsetMin = new Vector2(12f, 12f);
            listRect.offsetMax = new Vector2(-12f, -48f);

            var layout = listObject.GetComponent<VerticalLayoutGroup>();
            EnsureContractContainerLayout(listObject.transform);

            UIFontCatalog.ApplyToChildren(panelRoot.transform);
        }

        private void RefreshContractPanelLocalization()
        {
            if (contractPanelRoot == null)
            {
                return;
            }

            UIFontCatalog.ApplyToChildren(contractPanelRoot);
            var titleText = contractTitleText != null
                ? contractTitleText
                : contractPanelRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (titleText != null)
            {
                titleText.text = UIStrings.Contracts;
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

        private static void EnsureContractContainerLayout(Transform container)
        {
            if (container == null)
            {
                return;
            }

            var layout = container.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = container.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
