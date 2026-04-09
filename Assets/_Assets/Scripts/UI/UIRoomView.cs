using System.Collections;
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
        private UIBoardPanel _boardPanel;
        private GameObject _cardPrefab;
        private Coroutine _deferredSlotBindingRoutine;
        private bool _slotsBound;

        public RoomSlot Room => _room;
        public RectTransform SettlementAnchor => dropAnchor != null ? dropAnchor : (cardListContainer as RectTransform) ?? transform as RectTransform;
        public RectTransform DropAnchor => dropAnchor != null ? dropAnchor : transform as RectTransform;

        public void Setup(RoomSlot room, GameObject cardPrefab, UIBoardPanel boardPanel)
        {
            _room = room;
            _cardPrefab = cardPrefab;
            _boardPanel = boardPanel;

            EnsureRuntimeReferences();
            ConfigureDropZone();
            BuildSlots();
            RefreshTitle();
            FinalizeSlotsOrScheduleFallback();
        }

        private void BuildSlots()
        {
            var container = cardListContainer != null ? cardListContainer : transform;
            ClearContainer(container);
            _slotViews.Clear();
            _slotCards.Clear();
            _slotsBound = false;

            int tenantCapacity = _room != null ? Mathf.Max(0, _room.TenantSlotCapacity) : 0;
            for (int i = 0; i < tenantCapacity; i++)
            {
                var tenantSlot = CreateSlot(container, $"TenantSlot_{i}", CardViewContext.RoomTenant);
                if (tenantSlot == null) continue;
                _slotViews.Add(tenantSlot);
                _slotCards.Add(_room != null ? _room.GetTenantAt(i) : null);
            }

            int equipmentCapacity = _room != null ? Mathf.Max(0, _room.EquipmentSlotCapacity) : 0;
            for (int i = 0; i < equipmentCapacity; i++)
            {
                var equipmentSlot = CreateSlot(container, $"EquipmentSlot_{i}", CardViewContext.RoomEquipment);
                if (equipmentSlot == null) continue;
                _slotViews.Add(equipmentSlot);
                _slotCards.Add(_room != null ? _room.GetEquipmentAt(i) : null);
            }
        }

        private UIRoomSlotView CreateSlot(Transform parent, string slotName, CardViewContext context)
        {
            if (slotPrefab == null)
            {
                Debug.LogError("[UIRoomView] slotPrefab is not assigned. Wire RoomSlot.prefab in the Inspector.", gameObject);
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

        private void FinalizeSlotsOrScheduleFallback()
        {
            if (_deferredSlotBindingRoutine != null)
            {
                StopCoroutine(_deferredSlotBindingRoutine);
                _deferredSlotBindingRoutine = null;
            }

            if (TryApplyGridCellSizeAndBind())
            {
                return;
            }

            _deferredSlotBindingRoutine = StartCoroutine(RefreshGridCellSize());
        }

        private IEnumerator RefreshGridCellSize()
        {
            const int MaxRetryFrames = 2;
            for (int i = 0; i < MaxRetryFrames; i++)
            {
                yield return null; // wait a frame for layout to settle
                if (TryApplyGridCellSizeAndBind())
                {
                    _deferredSlotBindingRoutine = null;
                    yield break;
                }
            }

            BindSlots();
            _deferredSlotBindingRoutine = null;
        }

        private bool TryApplyGridCellSizeAndBind()
        {
            var container = cardListContainer != null ? cardListContainer : transform;
            var grid = container.GetComponent<GridLayoutGroup>();
            if (_slotViews.Count == 0)
            {
                _slotsBound = true;
                return true;
            }

            Canvas.ForceUpdateCanvases();
            var roomRect = transform as RectTransform;
            var parentRect = roomRect != null ? roomRect.parent as RectTransform : null;
            if (parentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }

            if (roomRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(roomRect);
            }

            var containerRect = container as RectTransform;
            if (containerRect == null)
            {
                return false;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            float w = containerRect.rect.width;
            float h = containerRect.rect.height;
            if (w <= 0f || h <= 0f)
            {
                return false;
            }

            if (grid != null)
            {
                ApplyBestGridCellSize(grid, w, h, _slotViews.Count);
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            }

            BindSlots();
            return true;
        }

        private static void ApplyBestGridCellSize(GridLayoutGroup grid, float width, float height, int count)
        {
            int bestCols = 1;
            float bestCellArea = 0f;

            for (int cols = 1; cols <= count; cols++)
            {
                int rows = Mathf.CeilToInt((float)count / cols);
                float cellW = (width - grid.padding.left - grid.padding.right - grid.spacing.x * (cols - 1)) / cols;
                float cellH = (height - grid.padding.top - grid.padding.bottom - grid.spacing.y * (rows - 1)) / rows;
                if (cellW <= 0f || cellH <= 0f) continue;
                float area = cellW * cellH;
                if (area > bestCellArea)
                {
                    bestCellArea = area;
                    bestCols = cols;
                }
            }

            int bestRows = Mathf.CeilToInt((float)count / bestCols);
            float finalCellW = (width - grid.padding.left - grid.padding.right - grid.spacing.x * (bestCols - 1)) / bestCols;
            float finalCellH = (height - grid.padding.top - grid.padding.bottom - grid.spacing.y * (bestRows - 1)) / bestRows;

            grid.cellSize = new Vector2(finalCellW, finalCellH);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = bestCols;
        }

        private void BindSlots()
        {
            if (_slotsBound)
            {
                return;
            }

            int count = Mathf.Min(_slotViews.Count, _slotCards.Count);
            for (int i = 0; i < count; i++)
            {
                _slotViews[i].Bind(_slotCards[i]);
            }

            _slotsBound = true;
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
