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
    public class UIRoomView : MonoBehaviour
    {
        [Header("Optional Scene References")]
        public TextMeshProUGUI titleText;
        public Transform cardListContainer;
        public Button roomButton;
        public RectTransform dropAnchor;
        public UICardDropZone dropZone;
        public Graphic highlightGraphic;
        [SerializeField] private GameObject slotPrefab;

        private readonly List<UIRoomSlotView> _slotViews = new();
        private RoomSlot _room;
        private UIBoardPanel _boardPanel;
        private GameObject _cardPrefab;

        public RoomSlot Room => _room;
        public RectTransform SettlementAnchor => dropAnchor != null ? dropAnchor : (cardListContainer as RectTransform) ?? transform as RectTransform;
        public RectTransform DropAnchor => dropAnchor != null ? dropAnchor : transform as RectTransform;

        public void Setup(RoomSlot room, GameObject cardPrefab, UIBoardPanel boardPanel)
        {
            _room = room;
            _cardPrefab = cardPrefab;
            _boardPanel = boardPanel;

            EnsureRuntimeReferences();
            EnsureSlotContainerLayout();
            ConfigureDropZone();
            BuildSlots();
            RefreshTitle();
        }

        private void BuildSlots()
        {
            var container = cardListContainer != null ? cardListContainer : transform;
            ClearContainer(container);
            _slotViews.Clear();

            int tenantCapacity = _room != null ? Mathf.Max(0, _room.TenantSlotCapacity) : 0;
            for (int i = 0; i < tenantCapacity; i++)
            {
                var tenantSlot = CreateSlot(container, $"TenantSlot_{i}", CardViewContext.RoomTenant);
                tenantSlot.Bind(_room != null ? _room.GetTenantAt(i) : null);
                _slotViews.Add(tenantSlot);
            }

            int equipmentCapacity = _room != null ? Mathf.Max(0, _room.EquipmentSlotCapacity) : 0;
            for (int i = 0; i < equipmentCapacity; i++)
            {
                var equipmentSlot = CreateSlot(container, $"EquipmentSlot_{i}", CardViewContext.RoomEquipment);
                equipmentSlot.Bind(_room != null ? _room.GetEquipmentAt(i) : null);
                _slotViews.Add(equipmentSlot);
            }
        }

        private UIRoomSlotView CreateSlot(Transform parent, string slotName, CardViewContext context)
        {
            GameObject slotObject;
            UIRoomSlotView slotView;

            if (slotPrefab != null)
            {
                slotObject = Instantiate(slotPrefab, parent, false);
                slotObject.name = slotName;
                slotView = slotObject.GetComponent<UIRoomSlotView>();
                if (slotView == null)
                {
                    slotView = slotObject.AddComponent<UIRoomSlotView>();
                }
            }
            else
            {
                slotObject = new GameObject(slotName, typeof(RectTransform));
                slotObject.transform.SetParent(parent, false);
                slotView = slotObject.AddComponent<UIRoomSlotView>();
            }

            slotView.Setup(context, _cardPrefab);
            return slotView;
        }

        private void RefreshTitle()
        {
            UIFontCatalog.ApplyToText(titleText);
            if (titleText == null || _room == null)
            {
                return;
            }

            titleText.text = UIStrings.RoomSummary(
                _room.RoomIndex + 1,
                _room.TenantCount,
                _room.TenantSlotCapacity,
                _room.EquipmentCount,
                _room.EquipmentSlotCapacity);
        }

        private void ConfigureDropZone()
        {
            if (dropZone != null)
            {
                dropZone.ZoneKind = CardPlayTargetKind.Room;
                dropZone.BindRoom(_room);
                dropZone.AssignRuntimeReferences(DropAnchor, highlightGraphic);
                dropZone.SetHighlighted(false, true);
            }

            if (roomButton != null)
            {
                roomButton.onClick.RemoveAllListeners();
                roomButton.transition = Selectable.Transition.None;
                roomButton.interactable = false;
            }
        }

        private void EnsureSlotContainerLayout()
        {
            var container = cardListContainer != null ? cardListContainer : transform;

            var layout = container.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void EnsureRuntimeReferences()
        {
            if (cardListContainer == null)
            {
                var containerObject = new GameObject("CardList", typeof(RectTransform));
                containerObject.transform.SetParent(transform, false);
                cardListContainer = containerObject.transform;

                var rect = cardListContainer as RectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.offsetMin = new Vector2(12f, 12f);
                rect.offsetMax = new Vector2(-12f, -48f);
            }

            if (titleText == null)
            {
                var titleObject = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                titleObject.transform.SetParent(transform, false);

                var rect = titleObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(12f, -36f);
                rect.offsetMax = new Vector2(-12f, -8f);

                titleText = titleObject.GetComponent<TextMeshProUGUI>();
                titleText.font = UIFontCatalog.GetPreferredFontAsset();
                titleText.fontSize = 20f;
                titleText.color = Color.white;
                titleText.alignment = TextAlignmentOptions.Left;
                titleText.raycastTarget = false;
            }

            if (roomButton == null)
            {
                roomButton = GetComponent<Button>();
            }

            if (roomButton == null)
            {
                var image = GetComponent<Image>();
                if (image == null)
                {
                    image = gameObject.AddComponent<Image>();
                    image.color = new Color(0f, 0f, 0f, 0.18f);
                }

                roomButton = gameObject.AddComponent<Button>();
            }

            if (dropAnchor == null)
            {
                dropAnchor = transform as RectTransform;
            }

            if (dropZone == null)
            {
                dropZone = GetComponent<UICardDropZone>();
                if (dropZone == null)
                {
                    dropZone = gameObject.AddComponent<UICardDropZone>();
                }
            }

            if (highlightGraphic == null)
            {
                var highlight = transform.Find("DropHighlight");
                if (highlight == null)
                {
                    var highlightObject = new GameObject("DropHighlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    highlightObject.transform.SetParent(transform, false);
                    highlight = highlightObject.transform;

                    var highlightRect = highlight as RectTransform;
                    highlightRect.anchorMin = Vector2.zero;
                    highlightRect.anchorMax = Vector2.one;
                    highlightRect.offsetMin = Vector2.zero;
                    highlightRect.offsetMax = Vector2.zero;

                    var highlightImage = highlightObject.GetComponent<Image>();
                    highlightImage.color = new Color(0.48f, 0.84f, 0.62f, 0f);
                    highlightImage.raycastTarget = false;
                }

                highlightGraphic = highlight.GetComponent<Graphic>();
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
    }
}
