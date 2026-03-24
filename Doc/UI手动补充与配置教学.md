# UI 手动补充与配置教学

## 1. 这份文档解决什么问题

当前代码已经可以把局内 UI 主链路跑起来，但为了后面更好查问题，还是建议你把几个关键节点在场景和 prefab 里显式补齐。

这份文档只讲最小必补项，不讲美术细调。

## 2. 先记住一个前提

游戏运行时目前不支持中文显示。

所以：

- 玩家能看到的固定 UI 文案，继续用英文
- `CardData.cardName`
- `CardData.description`
- 导表文本

在字体方案补上之前，也建议先用英文

中文可以继续保留在：

- 注释
- Inspector 说明
- 文档

## 3. 卡牌 prefab 推荐补什么

目标脚本：
[UICardView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardView.cs)

完整卡牌 prefab 建议显式准备这些节点：

- `background`
- `nameText`
- `costText`
- `descText`
- `typeText`
- `statsText`
- `Button`

额外建议显式挂这些组件：

- `CanvasGroup`
- `LayoutElement`

说明：

- 就算你不手动挂，代码现在也会尽量补齐
- 但手工挂好以后，拖拽和布局排查会明显轻松

## 4. 手牌区怎么补

目标脚本：
[UIHandPanel.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIHandPanel.cs)

你需要确认：

- `cardPrefab` 指向完整卡牌 prefab
- `handContainer` 指向手牌内容容器

推荐容器组件：

- `HorizontalLayoutGroup`

推荐设置：

- `Child Control Width = false`
- `Child Control Height = false`
- `Child Force Expand Width = false`
- `Child Force Expand Height = false`

## 5. 公共拖拽层怎么补

目标脚本：
[UIManager.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIManager.cs)

当前代码会在运行时确保有一个 `UICardDragController`。

如果你想改成显式配置，推荐这样补：

1. 在根 Canvas 下新建一个 `CardDragController`
2. 挂上 [UICardDragController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UICardDragController.cs)
3. 让它的 `RectTransform` 拉满整个 Canvas
4. 把它放在 Canvas 较靠后的层级，保证拖拽中的卡能在最上层

代码里现在把这个节点同时当作 `DragLayer` 使用。

## 6. 公共 `Play Area` 怎么补

目标脚本：
[UIBoardPanel.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIBoardPanel.cs)

当前代码如果没找到 `PlayAreaDropZone`，会运行时创建一个最小可用版本。

推荐你后面手工做成显式节点：

1. 在 `BoardPanel` 下新建一个 `PlayAreaDropZone`
2. 挂上 [UICardDropZone.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)
3. `ZoneKind` 设为 `PlayArea`
4. 准备一个底图 Image
5. 再准备一个单独的高亮层 `DropHighlight`

推荐布局：

- 放在牌盘区域下边缘，手牌上方
- 宽度明显大于单张卡
- 文案写 `Play Area`

注意：

- 高亮层最好单独做，不要直接拿底图本体做高亮
- 否则高亮淡入淡出时会把底图本身也改透明

## 7. 房间 prefab 怎么补

目标脚本：
[UIRoomView.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIRoomView.cs)

每个房间 prefab 建议显式准备这些引用：

- `titleText`
- `cardListContainer`
- `roomButton`
- `dropAnchor`
- `dropZone`
- `highlightGraphic`

房间根节点上建议挂：

- `Image`
- `Button`
- [UICardDropZone.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)

`dropZone` 设置：

- `ZoneKind = Room`

建议额外加一个子节点：

- `DropHighlight`

它只负责拖拽高亮，不负责常态底图。

## 8. 房间内容结构怎么补

当前逻辑固定是：

- `TenantSlot x1`
- `EquipmentSlot x3`

这部分现在仍由代码生成。

如果你后面想改成显式 prefab 结构，建议保留这个语义不变，再慢慢把“代码生成槽位”替换成“预摆好的槽位节点”。

## 9. 合同区怎么补

目标脚本：
[UIBoardPanel.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/UIBoardPanel.cs)

当前合同区还是运行时生成的。

如果你后面要改成显式配置，建议准备：

- `ContractPanel`
- `ContractContainer`

然后把 `RefreshContracts()` 的挂载点改成显式容器，而不是运行时创建。

## 10. Hover 预览怎么验收

目标脚本：

- [HoverPreviewController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Hover/HoverPreviewController.cs)
- [CardHoverPreviewPresenter.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/CardHoverPreviewPresenter.cs)

你主要确认：

- 场上租客可 Hover
- 场上装备可 Hover
- 合同卡可 Hover
- Hover 时显示完整卡
- 拖拽手牌时不会弹 Hover

## 11. 结算跳字怎么验收

目标脚本：

- [UISettlementSequenceController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)
- [UISequenceTextController.cs](C:/Users/x/.codex/worktrees/91ab/Project_Rent/Assets/_Assets/Scripts/UI/Common/Sequence/UISequenceTextController.cs)

重点看：

- 房间收益跳字是不是在对应房间上方
- 合同收益跳字是不是在合同卡附近
- 事件收益是不是在屏幕中间
- 顺序有没有乱
- 最终值有没有明显强调

## 12. 你进 Unity 后最建议先验收什么

按这个顺序最省时间：

1. 开局能否正常看到手牌
2. 手牌 hover 是否有轻微抬起
3. 手拖租客到房间能否成功
4. 手拖装备到房间能否成功
5. 普通事件拖到 `Play Area` 能否成功
6. 非法释放是否会回弹
7. 场上卡 hover 完整预览是否正常
8. 结算跳字是否顺序播放
9. 左下资金变化是否有轻微缩放反馈

## 13. 如果你想把“运行时补节点”逐步拆掉

建议顺序：

1. 先把 `PlayAreaDropZone` 改成显式场景节点
2. 再把房间的 `DropHighlight` 改成显式 prefab 子节点
3. 再把 `CardDragController` 改成显式 Canvas 子节点
4. 最后再考虑把合同区和房间槽位也改成显式容器

这样每次只替换一层，不容易一次性把局内 UI 全拆散。
