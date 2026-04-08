# UI手动配置与验收

## 文档目的

- 说明当前 UI 哪些引用必须在场景或 prefab 中显式配置。
- 说明哪些节点仍允许运行时补齐，哪些已经不能再靠静默兜底。
- 给 Unity 内手工验收提供一条最短可执行路径。

## 当前原则

- 主链路优先跑通，但主链路缺失的必需引用必须尽早暴露。
- Tooltip 和反馈模块允许“缺失只影响表现，不影响流程”。
- 奖励面板不属于可选模块；缺失时应直接暴露配置错误。
- 当前固定文案真源是 [GameText.cs](../../Assets/_Assets/Scripts/UI/GameText.cs)。
- 资金 popup 文字会通过项目侧文本配置应用当前字体；通用 popup 视图本身不直接依赖多语言服务。

## 1. UIManager

目标脚本：

- [UIManager.cs](../../Assets/_Assets/Scripts/UI/UIManager.cs)

必须显式配置：

- `topBar`
- `handPanel`
- `boardPanel`
- `phasePanel`
- `gameOverPanel`
- `cardDragController`
- `_settlementSequenceController`
- `_cardRewardPanel`

当前要求：

- `cardDragController` 缺失时会报错。
- `_cardRewardPanel` 属于主流程必需引用，缺失时会 fail-fast。
- 资金 popup 不再要求 `UIManager` 配置 `_feedbackBootstrap`。

## 2. 手牌区

目标脚本：

- [UIHandPanel.cs](../../Assets/_Assets/Scripts/UI/UIHandPanel.cs)

建议显式配置：

- `cardPrefab`
- `handContainer`

重点确认：

- 手牌 prefab 挂有 `UICardView`
- 手牌 prefab 挂有 `TooltipTrigger`
- 拖拽时手牌布局不会塌陷

## 3. 棋盘区

目标脚本：

- [UIBoardPanel.cs](../../Assets/_Assets/Scripts/UI/UIBoardPanel.cs)

建议显式配置：

- `roomPrefab`
- `roomContainer`
- `roomCardEntryPrefab`

当前仍可能由运行时补齐的内容：

- `PlayAreaDropZone`
- 合同区容器

## 4. 房间 prefab

目标脚本：

- [UIRoomView.cs](../../Assets/_Assets/Scripts/UI/UIRoomView.cs)

建议显式配置：

- `titleText`
- `cardListContainer`
- `roomButton`
- `dropAnchor`
- `dropZone`
- `highlightGraphic`

重点确认：

- 槽位数量由 `RoomSlot` 容量驱动，不依赖手写死槽位布局。
- 房间容量变化后，UI 能同步刷新。

## 5. 卡牌 prefab

目标脚本：

- [UICardView.cs](../../Assets/_Assets/Scripts/UI/UICardView.cs)
- [TooltipTrigger.cs](../../Assets/_Assets/Martian/Tooltip/TooltipTrigger.cs)

建议显式配置：

- `nameText`
- `costBadgeRoot`
- `costText`
- `rentBadgeRoot`
- `descText`
- `cardButton`
- `background`
- `TooltipTrigger`
- `CanvasGroup`
- `LayoutElement`
- `frameImage`
- `artImage`
- `skinDatabase`

当前显示规则：

- 手牌卡显示完整信息，包括类型标签、费用、租金、耐久和等待。
- 租金只在租客卡且 `baseRent > 0` 时显示。
- 耐久只在当前耐久 `> 1` 时显示。

## 6. 拖拽层与公共落点

目标脚本：

- [UICardDragController.cs](../../Assets/_Assets/Scripts/UI/UICardDragController.cs)
- [UICardDropZone.cs](../../Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)

建议后续显式化：

- `DragLayer`
- `PlayAreaDropZone`
- `DropHighlight`

当前现状：

- 缺失时仍有少量运行时补齐逻辑
- 适合原型阶段，但会增加排错成本

## 7. Tooltip 与结算反馈

目标脚本：

- [TooltipServices.cs](../../Assets/_Assets/Martian/Tooltip/TooltipServices.cs)
- [TooltipRuntimeService.cs](../../Assets/_Assets/Martian/Tooltip/Runtime/TooltipRuntimeService.cs)
- [BaoZuPoCardTooltipPresenter.cs](../../Assets/_Assets/Scripts/Integration/Martian/Tooltip/BaoZuPoCardTooltipPresenter.cs)
- [UIFeedbackPopupLayer.cs](../../Assets/_Assets/Scripts/UI/Common/FeedbackPopup/UIFeedbackPopupLayer.cs)
- [UIFeedbackPopupView.cs](../../Assets/_Assets/Scripts/UI/Common/FeedbackPopup/UIFeedbackPopupView.cs)
- [BaoZuPoFeedbackAdapter.cs](../../Assets/_Assets/Scripts/Integration/Martian/Feedback/BaoZuPo/BaoZuPoFeedbackAdapter.cs)
- [UISettlementSequenceController.cs](../../Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)

当前现状：

- Tooltip 运行时模块缺失时，会通过 `NullTooltipService` 安全降级。
- 资金反馈固定走 `UIFeedbackPopupLayer`；旧 `FeedbackRequest / FeedbackSequenceRequest` 跳字兜底已清理。
- `UIFeedbackPopupLayer.popupPrefab` 可选，不填时会运行时代码生成默认 popup。

钱栏 popup 配置：

- 在 `UITopBar` 上配置 `Money Target Anchor`，推荐指向手摆的 `MoneyPopupAnchor`。
- `Play Cost Popup Vertical Gap` 控制出牌扣款和贷款扣款离钱栏锚点的高度。
- `Settlement Total Popup Vertical Gap` 控制结算最终入账离钱栏锚点的高度。
- `Use Runtime Generated Layout` 默认关闭；关闭时保留场景中已绑定 TopBar 文本的位置和父物体。

## 8. 奖励面板

目标脚本：

- [UIManager.cs](../../Assets/_Assets/Scripts/UI/UIManager.cs)
- [UICardRewardPanel.cs](../../Assets/_Assets/Scripts/UI/UICardRewardPanel.cs)

必须显式配置：

- `UIManager._cardRewardPanel`
- `UICardRewardPanel._panelRoot`
- `UICardRewardPanel._titleText`
- `UICardRewardPanel._cardSlot0`
- `UICardRewardPanel._cardSlot1`
- `UICardRewardPanel._cardSlot2`
- `UICardRewardPanel._cardPrefab`
- `UICardRewardPanel._skipButton`
- `UICardRewardPanel._skipButtonText`

当前现状：

- 奖励链路不会自动补一个最小可运行奖励 UI
- 缺失时会直接报错

## 9. 文案真源验收

必须认清当前运行时真源：

- [GameText.cs](../../Assets/_Assets/Scripts/UI/GameText.cs)

验收时需要确认：

- 新增固定文案是否接入 `GameText`
- popup 文案是否能走 `GameText` 或 `CardText`
- 中文字体是否能通过 popup layer 的默认文本配置正常显示

## 10. 最短验收顺序

1. 开局后能看到手牌、房间、顶栏和结束行动按钮。
2. 手牌、房间租客、房间设备、合同区 Tooltip 正常。
3. 租客拖到房间能成功。
4. 设备拖到房间能成功。
5. 事件拖到 `Play Area` 能成功。
6. 非法拖放会回弹。
7. 拖拽开始、阶段切换、回合切换、GameOver 时 Tooltip 会自动关闭。
8. 结算跳字按顺序播放。
9. 出牌扣款、贷款扣款和结算最终入账都出现在 `Money Target Anchor` 上方。
10. 结算后奖励面板会弹出。
11. 选择奖励或跳过都能进入下一回合。
