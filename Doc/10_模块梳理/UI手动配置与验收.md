# UI手动配置与验收

## 文档目的

- 说明当前 UI 哪些引用必须在场景或 prefab 中显式配置。
- 说明哪些节点仍允许运行时补齐，哪些已经不能再靠静默兜底。
- 给 Unity 内手工验收提供一条最短可执行路径。

## 当前原则

- 主链路优先跑通，但主链路缺失的必需引用必须尽早暴露。
- Tooltip 和反馈模块允许“缺失只影响表现，不影响流程”。
- 奖励面板不属于可选模块；缺失时应直接暴露配置错误。
- 当前运行时本地化真源仍是 `LocalizationManager + UIStrings + UIFontCatalog`。

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

- `cardDragController` 和 `_feedbackBootstrap` 当前已经有显式报错。
- `_cardRewardPanel` 现在已经按主链路必需引用处理，运行时缺失会直接 fail-fast。

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

- 槽位数量由 `RoomSlot` 容量驱动，不应再依赖手写死槽位布局假设
- 房间容量变化后，UI 是否能同步刷新

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

允许运行时补齐：

- `typeText`
- `statsText`
- 部分纯展示型装饰图层

## 6. 拖拽层与公共落点

目标脚本：

- [UICardDragController.cs](../../Assets/_Assets/Scripts/UI/UICardDragController.cs)
- [UICardDropZone.cs](../../Assets/_Assets/Scripts/UI/Common/Drag/UICardDropZone.cs)

建议后续显式化：

- `DragLayer`
- `PlayAreaDropZone`
- `DropHighlight`

当前现状：

- 缺失时部分节点仍会由代码补齐最小可运行版本
- 这适合原型阶段，但会增加后续排错成本

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

- Tooltip 不再要求场景里预先配置 `HoverPreviewRoot`
- Tooltip Root 会在首次显示时按所属 `Canvas` 运行时创建
- 移除 Tooltip 运行时模块后，会通过 `NullTooltipService` 安全降级
- Tooltip 模块本体位于 `Assets/_Assets/Martian/Tooltip/**`，项目接线位于 `Assets/_Assets/Scripts/Integration/Martian/Tooltip/**`
- 反馈模块关闭或未初始化时，会退化为 no-op，但不阻断结算
- 反馈模块本体位于 `Assets/_Assets/Martian/Feedback/**`，项目接线位于 `Assets/_Assets/Scripts/Integration/Martian/Feedback/**`

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
- `UIManager` 缺失 `_cardRewardPanel` 时会直接报错
- `UICardRewardPanel` 缺失槽位、按钮、标题或卡牌 prefab 时也会直接报错
- 因此奖励面板当前必须按主链路配置处理，而不是按可选模块处理

## 9. 当前本地化真源验收

必须认清当前运行时真源：

- [LocalizationManager.cs](../../Assets/_Assets/Scripts/UI/LocalizationManager.cs)
- [UIStrings.cs](../../Assets/_Assets/Scripts/UI/UIStrings.cs)
- [UIFontCatalog.cs](../../Assets/_Assets/Scripts/UI/UIFontCatalog.cs)

验收时需要确认：

- 新增固定文案是否接入 `UIStrings`
- 切语言时是否能触发对应界面刷新
- 新增 TMP 文本是否能正确应用中文字体

## 10. 最短验收顺序

1. 开局后能看到手牌、房间、顶栏和结束行动按钮
2. 手牌、房间租客、房间设备、合同区 Tooltip 正常
3. 租客拖到房间能成功
4. 设备拖到房间能成功
5. 事件拖到 `Play Area` 能成功
6. 非法拖放会回弹
7. 拖拽开始、阶段切换、回合切换、GameOver 时 Tooltip 会自动关闭
8. 结算跳字按顺序播放
9. 结算后奖励面板会弹出
10. 选择奖励或跳过都能进入下一回合
11. 切语言后，奖励面板标题、跳过按钮和其它固定文案都同步刷新

## 11. Tooltip 模块缺席时的降级验收

建议在一次验收中临时禁用 Tooltip 运行时模块，确认以下行为仍正常：

1. 项目能正常进入场景
2. 手牌、房间、合同区仍能刷新显示
3. 拖拽出牌与合法性校验不受影响
4. 阶段切换、回合切换、GameOver 不报错
5. 唯一差异只是 Tooltip 不再显示
