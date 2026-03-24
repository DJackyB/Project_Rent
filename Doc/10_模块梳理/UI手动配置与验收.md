# UI手动配置与验收

## 文档目的

- 说明当前 UI 哪些引用最好在场景或 prefab 中显式配置。
- 说明哪些节点现在仍由代码在运行时兜底创建。
- 给 Unity 内手工验收提供最短路径。

## 当前原则

- 主功能先跑通，允许少量运行时补节点。
- 高频、易误配、排查成本高的引用，优先改为显式配置。
- 玩家可见中文能力后续要补齐，但当前很多固定文案仍是英文。

## 1. 手牌区

目标脚本：

- [UIHandPanel.cs](../../Assets/_Assets/Scripts/UI/UIHandPanel.cs)

建议显式配置：

- `cardPrefab`
- `handContainer`

推荐组件：

- `HorizontalLayoutGroup`
- `CanvasGroup`
- `LayoutElement`

重点确认：

- 手牌 prefab 挂有 `UICardView`
- 拖拽时手牌布局不会塌陷

## 2. 棋盘区

目标脚本：

- [UIBoardPanel.cs](../../Assets/_Assets/Scripts/UI/UIBoardPanel.cs)

建议显式配置：

- `roomPrefab`
- `roomContainer`
- `roomCardEntryPrefab`

当前仍可能运行时兜底的内容：

- `PlayAreaDropZone`
- 合同区容器

## 3. 房间 prefab

目标脚本：

- [UIRoomView.cs](../../Assets/_Assets/Scripts/UI/UIRoomView.cs)

建议显式配置：

- `titleText`
- `cardListContainer`
- `roomButton`
- `dropAnchor`
- `dropZone`
- `highlightGraphic`

当前限制：

- 代码仍固定生成 `1 个租客槽 + 3 个设备槽`
- 如果房间容量变化，必须回归 UI 是否同步

## 4. 卡牌 prefab

目标脚本：

- [UICardView.cs](../../Assets/_Assets/Scripts/UI/UICardView.cs)

建议显式配置：

- `nameText`
- `costText`
- `descText`
- `cardButton`
- `background`

允许运行时补齐：

- `typeText`
- `statsText`
- 一部分运行时装饰图层

## 5. 拖拽层与公共落点

目标脚本：

- [UICardDragController.cs](../../Assets/_Assets/Scripts/UI/UICardDragController.cs)
- [UICardDropZone.cs](../../Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)

建议后续显式化：

- `DragLayer`
- `PlayAreaDropZone`
- `DropHighlight`

当前现状：

- 缺失时会由代码创建可运行的最小版本
- 这能加快原型速度，但会增加后续排错成本

## 6. Hover 与结算跳字

目标脚本：

- [HoverPreviewController.cs](../../Assets/_Assets/Scripts/UI/Common/Hover/HoverPreviewController.cs)
- [UISettlementSequenceController.cs](../../Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)

当前现状：

- 两者都使用独立 Overlay
- 缺失时能运行时补齐
- 更适合在表现稳定后再做完整场景显式化

## 7. 最短验收顺序

1. 开局后能看到手牌、房间、顶栏和结束行动按钮。
2. 租客拖到房间能成功。
3. 设备拖到房间能成功。
4. 事件拖到 `Play Area` 能成功。
5. 非法释放会回弹。
6. 场上卡 Hover 正常。
7. 结算跳字按顺序播放。
8. 顶栏资金和回合会更新。

## 8. 什么时候优先改成显式配置

- 某个节点反复因为场景误配而出错。
- 某个运行时补节点开始需要美术精修。
- 某个 UI 结构已经稳定，不再频繁改版。

## 9. 什么时候可以继续保留兜底

- Overlay 类临时容器。
- 原型期还在频繁改结构的 UI 层。
- 只要缺失也不会改变玩法结果、只影响展示搭建效率的节点。
