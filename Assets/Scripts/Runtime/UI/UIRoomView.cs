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
    /// <summary>
    /// 房间视图，代表棋盘上的单个房间，包含标题、租户和装备插槽列表。
    /// 配置房间的拖拽放置区域（DropZone）以接收卡牌放置。
    /// 提供不同的锚点用于拖拽反馈和结算演示定位。
    /// </summary>
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
        private readonly List<CardInstance> _slotCards = new();
        private RoomSlot _room;
        private GameObject _cardPrefab;

        public RoomSlot Room => _room;
        public GameObject SlotPrefab => slotPrefab;
        public RectTransform SettlementAnchor => dropAnchor != null ? dropAnchor : (cardListContainer as RectTransform) ?? transform as RectTransform;
        public RectTransform DropAnchor => dropAnchor != null ? dropAnchor : transform as RectTransform;

        public void Setup(RoomSlot room, GameObject cardPrefab)
        {
            _room = room;
            _cardPrefab = cardPrefab;

            EnsureRuntimeReferences();
            ConfigureDropZone();
            BuildSlots();
            ApplyGridLayout();
            RefreshTitle();
            BindSlots();
            RefreshCardScales();
        }

        /// <summary>
        /// 若此房间视图持有指定卡牌，偷走其卡牌对象并返回；否则返回 null。
        /// </summary>
        public GameObject TryStealCardObjectForAnimation(CardInstance card)
        {
            if (card == null)
            {
                return null;
            }

            foreach (var slot in _slotViews)
            {
                if (slot != null && ReferenceEquals(slot.BoundCard, card))
                {
                    return slot.StealCardObject();
                }
            }

            return null;
        }

        private void BuildSlots()
        {
            var container = cardListContainer != null ? cardListContainer : transform;
            ClearContainer(container);
            _slotViews.Clear();
            _slotCards.Clear();

            int tenantCapacity = _room != null ? Mathf.Max(0, _room.TenantSlotCapacity) : 0;
            for (int i = 0; i < tenantCapacity; i++)
            {
                var tenantCard = _room != null ? _room.GetTenantAt(i) : null;
                if (tenantCard == null)
                {
                    continue;
                }

                var tenantSlot = CreateSlot(container, $"TenantSlot_{i}", CardViewContext.RoomTenant);
                if (tenantSlot == null) continue;
                _slotViews.Add(tenantSlot);
                _slotCards.Add(tenantCard);
            }

            int equipmentCapacity = _room != null ? Mathf.Max(0, _room.EquipmentSlotCapacity) : 0;
            for (int i = 0; i < equipmentCapacity; i++)
            {
                var equipmentCard = _room != null ? _room.GetEquipmentAt(i) : null;
                if (equipmentCard == null)
                {
                    continue;
                }

                var equipmentSlot = CreateSlot(container, $"EquipmentSlot_{i}", CardViewContext.RoomEquipment);
                if (equipmentSlot == null) continue;
                _slotViews.Add(equipmentSlot);
                _slotCards.Add(equipmentCard);
            }
        }

        private UIRoomSlotView CreateSlot(Transform parent, string slotName, CardViewContext context)
        {
            if (slotPrefab == null)
            {
                Debug.LogError("[UIRoomView] slotPrefab is not assigned. Wire CardSlot.prefab in the Inspector.", gameObject);
                return null;
            }

            var slotObject = Instantiate(slotPrefab, parent, false);
            slotObject.name = slotName;
            var slotView = slotObject.GetComponent<UIRoomSlotView>();
            if (slotView == null)
            {
                Debug.LogError("[UIRoomView] slotPrefab is missing UIRoomSlotView component.", slotObject);
                return null;
            }

            slotView.Setup(context, _cardPrefab);
            return slotView;
        }

        private void RefreshTitle()
        {
            if (titleText == null || _room == null)
            {
                return;
            }

            titleText.text = GameText.RoomSummary(
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
                dropZone.BindRoom(_room);
                dropZone.SetHighlighted(false, true);
            }

            if (roomButton != null)
            {
                roomButton.onClick.RemoveAllListeners();
            }
        }

        private void BindSlots()
        {
            int count = Mathf.Min(_slotViews.Count, _slotCards.Count);
            for (int i = 0; i < count; i++)
            {
                _slotViews[i].Bind(_slotCards[i]);
            }
        }

        private void ApplyGridLayout()
        {
            var container = cardListContainer != null ? cardListContainer : transform;
            var grid = container.GetComponent<GridLayoutGroup>();
            var containerRect = container as RectTransform;
            if (grid == null || containerRect == null)
            {
                return;
            }

            int itemCount = _slotViews.Count;
            if (itemCount <= 0)
            {
                return;
            }

            ResolveGrid(itemCount, out int columns, out int rows);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            float availableWidth = containerRect.rect.width - grid.padding.left - grid.padding.right - grid.spacing.x * (columns - 1);
            float availableHeight = containerRect.rect.height - grid.padding.top - grid.padding.bottom - grid.spacing.y * (rows - 1);
            if (availableWidth <= 0f || availableHeight <= 0f)
            {
                return;
            }

            grid.cellSize = new Vector2(
                availableWidth / columns,
                availableHeight / rows);
        }

        private void RefreshCardScales()
        {
            var containerRect = cardListContainer as RectTransform;
            if (containerRect != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }

            for (int i = 0; i < _slotViews.Count; i++)
            {
                _slotViews[i]?.RefreshCardScale();
            }
        }

        private static void ResolveGrid(int itemCount, out int columns, out int rows)
        {
            if (itemCount <= 1)
            {
                columns = 1;
                rows = 1;
                return;
            }

            if (itemCount == 2)
            {
                columns = 2;
                rows = 1;
                return;
            }

            if (itemCount <= 4)
            {
                columns = 2;
                rows = 2;
                return;
            }

            if (itemCount <= 9)
            {
                columns = 3;
                rows = 3;
                return;
            }

            int dimension = Mathf.CeilToInt(Mathf.Sqrt(itemCount));
            columns = dimension;
            rows = dimension;
        }

        private void EnsureRuntimeReferences()
        {
            if (roomButton == null)
                roomButton = GetComponent<Button>();

            if (dropAnchor == null)
                dropAnchor = transform as RectTransform;

            if (dropZone == null)
                Debug.LogError("[UIRoomView] Missing UICardDropZone on Room.prefab. Add the component and assign it in the Inspector.", gameObject);

            if (highlightGraphic == null)
                Debug.LogError("[UIRoomView] Missing highlightGraphic on Room.prefab. Assign the DropHighlight Image in the Inspector.", gameObject);
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
