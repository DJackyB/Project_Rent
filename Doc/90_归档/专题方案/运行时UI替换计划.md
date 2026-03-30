# 已归档：运行时 UI 生成 → Prefab/Inspector 显式配置 替换计划

> 归档时间：2026-03-30
> 归档原因：该文档属于阶段性替换计划，当前维护入口已转为 `10_模块梳理/06_UI系统.md` 与 `10_模块梳理/UI手动配置与验收.md`，不再作为主文档入口。

# 运行时 UI 生成 → Prefab/Inspector 显式配置 替换计划

---

## 🟢 第 1 批：纯代码处理 — ✅ 已完成

| # | 文件 | 处理 | 状态 |
|---|------|------|------|
| A1 | `UIEquipmentCardView.cs` | 加 `[RequireComponent(typeof(LayoutElement))]` | ✅ |
| A2 | `UICardDragController.cs` Placeholder | 保留 | ✅ |
| A3 | `BaoZuPoCardTooltipPresenter.cs` Factory | 保留 | ✅ |

---

## 🟡 第 2 批：代码修改 + Inspector 配置 — ✅ 已完成

### B1: UIManager → CardDragController

**代码变更**: 删除了 `EnsureCardDragController()` 中的 `new GameObject` 逻辑

**Inspector 操作:**
1. 在 Hierarchy 中找到 `UIManager` 对象
2. 在其下创建子 GameObject，命名 `CardDragController`
3. 添加 `RectTransform` 组件（若默认没有）
4. 设置 Anchor: Stretch-Stretch (anchorMin=0,0  anchorMax=1,1  offset=0)
5. 挂载 `UICardDragController` 组件
6. 将该对象拖入 UIManager 的 `Card Drag Controller` 字段

---

### B2: UIManager → FeedbackBootstrap

**Inspector 操作:**
1. 在 `UIManager` 下创建子 GameObject，命名 `FeedbackBootstrap`
2. 添加 `RectTransform`
3. 挂载 `FeedbackBootstrap` 组件
4. 拖入 UIManager Inspector 的 `Feedback Bootstrap` 字段

---

### B3: UIManager → SettlementSequenceController

**Inspector 操作:**
1. 在 `UIManager` 下创建子 GameObject，命名 `SettlementSequenceController`
2. 添加 `RectTransform`
3. 挂载 `UISettlementSequenceController` 组件
4. 拖入 UIManager Inspector 的 `Settlement Sequence Controller` 字段

---

### B4: UIHandPanel → HandContainer

**代码变更**: 删除了 `EnsureContainer()` 中的 `new GameObject("HandContainer")` fallback

**Inspector 操作:**
1. 在 `UIHandPanel` 下创建子 GameObject，命名 `HandContainer`
2. 确保是 `RectTransform`
3. 拖入 `UIHandPanel` Inspector 的 `Hand Container` 字段
4. 注意：如果已有 HandContainer 子节点，直接拖入即可

---

### B5: UICardDragController → DragLayer

**代码变更**: 删除了 `ResolveSceneReferences()` 中创建 DragLayer 的 fallback

**Inspector 操作:**
1. 在 **Canvas 根节点**下创建子 GameObject，命名 `DragLayer`
2. 确保是 `RectTransform`
3. 设置 Anchor: Stretch-Stretch (全屏)
4. 拖入 `UICardDragController` Inspector 的 `Drag Layer` 字段
5. 确保 `DragLayer` 是 Canvas 的最后一个子节点（最上层渲染）

---

### B6: BoardManager → Rooms 容器

**代码变更**: `_roomRoot` 改为 `[SerializeField]`，删除运行时创建

**Inspector 操作:**
1. 在场景中创建空 GameObject，命名 `Rooms`
2. 拖入 `BoardManager` Inspector 的 `Room Root` 字段

---

---

## 🔴 第 3 批：UI Prefab 重构 — 待处理

| # | 文件 | 内容 | 工作量 |
|---|------|------|-------|
| C1 | `UITopBar.cs` | 3 个 TMP HUD 标签 (Turn/Money/Spent) | 中 |
| C2 | `UIPhasePanel.cs` | EndTurnButton (Image+Button+Label TMP) | 中 |
| C3 | `UIBoardPanel.cs` | PlayAreaDropZone (Image+Label+UICardDropZone) | 中 |
| C4 | `UIBoardPanel.cs` | ContractPanel (Image+Title+VerticalLayout容器) | 大 |
| C5 | `UIRoomView.cs` | 完整房间视图 (CardList+Title+DropHighlight+Button+DropZone+Layout) | 大 |
| C6 | `UIRoomSlotView.cs` | 卡槽视图 Prefab (ContentRoot+EmptySlot占位符+LayoutElement) | 中 |
| C7 | `UISequenceTextController.cs` | 整个 Overlay Canvas + SequenceBubble (CanvasGroup+Image+Label) | 大 |
| C8 | `UICardDropZone.cs` | 各出牌区或房间的 DropHighlight 节点与 Raycast Image | (与 C3/C5 合并) |

*注：C8 所需配置即在 C3 和 C5 的预制体创建中预先加入 Image(Alpha 0, Raycast Ture) 和子节点 DropHighlight (Image, Alpha 0, Raycast False)。*
