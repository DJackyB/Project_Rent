# 局内 UI 模块说明

## 1. 这份文档看什么

这份文档用来快速理解当前局内 UI 的代码结构，重点覆盖：

- 手牌拖拽出牌
- 房间 / 合同 / 公共出牌区显示
- Hover 完整预览
- 结算跳字
- HUD 与基础动效

如果你后面要继续改局内 UI，优先看这份文档，再按模块去看对应脚本。

## 2. 当前整体结构

当前局内 UI 可以按 4 层理解：

- 数据层
  - `CardData`
  - `CardInstance`
  - `RoomSlot`
  - `BoardManager`
- 出牌逻辑层
  - `TurnManager`
  - `GamePlayTypes`
  - `GameEvents`
- 业务 UI 层
  - `UIManager`
  - `UIHandPanel`
  - `UIBoardPanel`
  - `UIRoomView`
  - `UIRoomSlotView`
  - `UICardView`
  - `UIEquipmentCardView`
  - `UITopBar`
  - `UIPhasePanel`
- 通用交互 / 演出层
  - `UI/Common/Hover/*`
  - `UI/Common/Drag/*`
  - `UI/Common/Sequence/*`
  - `UI/Common/Animation/*`
  - `UI/Settlement/*`

## 3. 出牌主链路

### 3.1 现在的交互方式

旧的“点手牌，再点房间”主路径已经移除。

现在主路径是：

1. 手牌卡片挂 `UICardDragHandler`
2. 鼠标按下并拖拽
3. `UICardDragController` 接管真实卡牌对象
4. 根据鼠标位置识别 `UICardDropZone`
5. 松手时先做 `TurnManager.ValidatePlay(...)`
6. 合法则播放飞牌动画
7. 动画结束后调用 `TurnManager.PlayCard(...)`
8. `CardPlayed` 事件触发 `UIManager.RefreshAll()`

### 3.2 目标规则

`TurnManager.GetRequiredTargetKind(...)` 现在统一决定目标类型：

- `Tenant` -> `Room`
- `Equipment` -> `Room`
- 任意效果字符串中包含 `SelectedRoom` 的牌 -> `Room`
- 其他普通 `Event / Contract` -> `PlayArea`

对应 UI 规则：

- 需要房间目标的牌，拖到整个房间面板
- 不需要房间目标的牌，拖到公共 `Play Area`

### 3.3 关键脚本

- [TurnManager.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/GameFlow/TurnManager.cs)
  - `CurrentPhase`
  - `GetRequiredTargetKind`
  - `ValidatePlay`
  - `PlayCard`
- [GamePlayTypes.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/GameFlow/GamePlayTypes.cs)
  - `GamePhase`
  - `CardPlayTargetKind`
  - `CardPlayBlockReason`
  - `CardPlayValidationResult`

## 4. 拖拽模块

### 4.1 `UICardDragHandler`

路径：
[UICardDragHandler.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Drag/UICardDragHandler.cs)

职责：

- 只在 `Hand` 上下文启用
- 接收 `BeginDrag / Drag / EndDrag`
- 做手牌 hover 上浮 / 放大
- 做拖拽开始时的卡牌缩放状态切换

### 4.2 `UICardDragController`

路径：
[UICardDragController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardDragController.cs)

职责：

- 全局只保留一个
- 管理当前拖拽中的真实卡牌
- 创建手牌占位对象
- 把卡提到顶层拖拽层
- 识别当前鼠标下的 `UICardDropZone`
- 做合法落点高亮
- 做非法回弹
- 做合法飞牌动画
- 在动画结束后调用 `TurnManager.PlayCard`

几个关键点：

- 拖的是“真实手牌卡片”，不是影子卡
- 成功打出后，原拖拽中的 UI 会被主动销毁
- 失败或取消时，卡会回到原手牌位置
- `RefreshAll` / 阶段切换 / 回合切换时，会强制取消拖拽

### 4.3 `UICardDropZone`

路径：
[UICardDropZone.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)

职责：

- 标记可放置区域
- 标记自己的目标类型：`Room` 或 `PlayArea`
- 提供落点锚点 `DropAnchor`
- 提供高亮图层

当前主要有两类：

- 房间根节点上的 `Room` 落点
- 公共 `Play Area` 落点

## 5. 卡牌显示模块

### 5.1 `UICardView`

路径：
[UICardView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardView.cs)

职责：

- 按 `CardViewContext` 渲染卡牌
- 动态拼装 A/B/C 三层资源
  - A：类型卡面
  - B：稀有度外框
  - C：卡牌插图
- 控制费用、描述、类型、属性文本的显隐
- 在场上卡上提供 Hover 预览数据源
- 在手牌卡上接入 `UICardDragHandler`

### 5.2 `CardViewContext`

路径：
[CardViewContext.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/Card/CardViewContext.cs)

当前使用的上下文：

- `Hand`
- `RoomTenant`
- `RoomEquipment`
- `Contract`
- `HoverPreview`

### 5.3 `UIEquipmentCardView`

路径：
[UIEquipmentCardView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIEquipmentCardView.cs)

职责：

- 装备槽里的卡走简化显示
- 仍然复用 `UICardView` 做数据绑定
- 保留 Hover 完整预览

## 6. 牌盘模块

### 6.1 `UIBoardPanel`

路径：
[UIBoardPanel.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIBoardPanel.cs)

职责：

- 刷新房间列表
- 刷新合同列表
- 维护 `RoomSlot -> UIRoomView`
- 维护 `CardInstance -> UICardView`
- 提供结算跳字锚点
- 管理公共 `Play Area`

### 6.2 `UIRoomView`

路径：
[UIRoomView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIRoomView.cs)

职责：

- 表示一个房间
- 固定生成 `1 个租客槽 + 3 个装备槽`
- 绑定房间根节点的 `UICardDropZone`
- 暴露房间拖拽锚点

### 6.3 `UIRoomSlotView`

路径：
[UIRoomSlotView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIRoomSlotView.cs)

职责：

- 渲染单个槽位
- 没卡时显示空槽占位
- 有卡时根据上下文实例化对应卡视图

## 7. Hover 预览

路径：
[HoverPreviewController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Hover/HoverPreviewController.cs)

相关：

- [CardHoverPreviewPresenter.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/CardHoverPreviewPresenter.cs)

规则：

- 只对场上卡启用
- 鼠标悬停时显示完整卡
- 跟随鼠标
- 场景刷新、阶段切换、出牌时主动关闭
- Hover 与拖拽不会并行生效

## 8. 结算跳字

### 8.1 数据层

- `GameEvents.SettlementSequenceQueued`
- `UISettlementSequenceData`
- `UISequencePlaybackRequest`
- `UISequenceStep`

### 8.2 播放层

路径：
[UISequenceTextController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Sequence/UISequenceTextController.cs)

当前已经从旧的协程播放器改成 `DOTween Sequence`。

表现规则：

- 每个 step：淡入 + 上浮 + 轻微缩放 + 停留 + 淡出
- 同一个请求内 step 串行
- 多个请求之间也串行
- `Final` step 比普通 step 更强调

辅助工具：

- [UIAnimationTweenUtility.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Animation/UIAnimationTweenUtility.cs)

### 8.3 业务转译层

路径：
[UISettlementSequenceController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)

职责：

- 把 `SettlementSequenceQueued` 转成 UI 可播放数据
- 决定房间 / 合同 / 事件的锚点
- 决定每步颜色和偏移

## 9. HUD 与基础动效

### 9.1 `UITopBar`

路径：
[UITopBar.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UITopBar.cs)

职责：

- 显示 `Turn / Spent / Money`
- 监听 `MoneyChanged`
- 资金变化时对 `moneyText` 做轻微 `PunchScale`

### 9.2 `UIPhasePanel`

路径：
[UIPhasePanel.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIPhasePanel.cs)

职责：

- 显示 `Prepare / Action / Settle`
- 控制右下角结束回合按钮

## 10. 当前仍然是运行时生成的部分

为了先把功能跑通，目前仍然保留了少量运行时生成：

- `UIManager` 运行时确保 `UICardDragController`
- `UIBoardPanel` 运行时确保 `PlayAreaDropZone`
- `UIRoomView` 运行时确保 `DropHighlight`
- Hover / 结算气泡 Overlay

这些都不是玩法数据层的“兜底”，而是交互骨架层的轻量补件。

如果后面你想把场景彻底做成显式配置，优先替换这几类节点即可。

## 11. 建议你以后从哪几个入口查

如果你要排查：

- 出牌是否合法
  - 先看 [TurnManager.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/GameFlow/TurnManager.cs)
- 拖拽为什么没成功
  - 先看 [UICardDragController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardDragController.cs)
  - 再看 [UICardDropZone.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)
- 卡面为什么显示不对
  - 先看 [UICardView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardView.cs)
  - 再看 [CardSkinDatabase.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/Card/CardSkinDatabase.cs)
- 结算跳字为什么不对
  - 先看 [UISettlementSequenceController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)
  - 再看 [UISequenceTextController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Sequence/UISequenceTextController.cs)
