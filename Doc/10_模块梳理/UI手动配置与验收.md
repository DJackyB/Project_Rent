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
- 当前没有语言切换，也没有运行时字体切换链路。

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
- `_feedbackBootstrap`
- `_settlementSequenceController`
- `_cardRewardPanel`

当前要求：

- `cardDragController` 和 `_feedbackBootstrap` 缺失时会报错。
- `_cardRewardPanel` 属于主流程必需引用，缺失时会 fail-fast。

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
- `costText`
- `descText`
- `cardButton`
- `background`
- `TooltipTrigger`
- `CanvasGroup`
- `LayoutElement`
- `frameImage`
- `artImage`
- `skinDatabase`

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
- [FeedbackBootstrap.cs](../../Assets/_Assets/Martian/Feedback/Runtime/FeedbackBootstrap.cs)
- [FeedbackPlaybackCoordinator.cs](../../Assets/_Assets/Martian/Feedback/Runtime/FeedbackPlaybackCoordinator.cs)
- [BaoZuPoFeedbackAdapter.cs](../../Assets/_Assets/Scripts/Integration/Martian/Feedback/BaoZuPo/BaoZuPoFeedbackAdapter.cs)
- [UISettlementSequenceController.cs](../../Assets/_Assets/Scripts/UI/Settlement/UISettlementSequenceController.cs)

当前现状：

- Tooltip 运行时模块缺失时，会通过 `NullTooltipService` 安全降级。
- 反馈模块关闭或未初始化时，会退化为 no-op，但不阻断结算。

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
- 是否没有重新引入语言切换状态
- 是否没有重新引入字体切换或中文字体依赖

## 10. 最短验收顺序

1. 开局后能看到手牌、房间、顶栏和结束行动按钮。
2. 手牌、房间租客、房间设备、合同区 Tooltip 正常。
3. 租客拖到房间能成功。
4. 设备拖到房间能成功。
5. 事件拖到 `Play Area` 能成功。
6. 非法拖放会回弹。
7. 拖拽开始、阶段切换、回合切换、GameOver 时 Tooltip 会自动关闭。
8. 结算跳字按顺序播放。
9. 结算后奖励面板会弹出。
10. 选择奖励或跳过都能进入下一回合。
