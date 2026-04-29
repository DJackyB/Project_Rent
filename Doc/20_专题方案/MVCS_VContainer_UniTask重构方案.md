# MVCS + VContainer + UniTask 重构方案

> 状态：执行中  
> 创建日期：2026-04-28  
> 当前阶段：Phase 3，RewardService / ShopService 拆分完成，等待验收；Phase 4 VContainer 接入仍未验收
> 适用范围：核心流程、出牌、结算、奖励、商店、UI 表现、依赖管理和对象池重构

## 1. 总目标

本方案定义《包租婆》后续重构的目标架构和分批执行边界。重构采用 **MVCS / Application-Service 架构**，不引入 QFramework、R3、MessagePipe、Luban。

核心技术栈固定为：

- `VContainer`：依赖注入与生命周期装配。
- `UniTask`：主循环、结算、奖励、商店等异步流程编排。
- 现有 `EventBus`：跨层事实事件通知。
- `UnityEngine.Pool.ObjectPool<T>`：弹字、Ghost 卡、结算数字等临时表现对象池。
- `DOTween / Feel`：动画和手感反馈保留。
- `Martian.Localization / Martian.Save`：本地化和存档模块保留。
- `CardEffectFactory`：现有卡牌效果 Strategy + Factory 设计保留。

重构目标不是一次性推倒重写，而是把当前巨型 `TurnManager` 拆成可测试、可暂停、可分批验收的服务链路，让核心规则逐步脱离 MonoBehaviour 和 Singleton，最终让 `TurnFlowService.RunAsync` 替代 NodeCanvas 成为正式回合流程真源。

## 2. 目标架构

```text
Presentation
- UGUI / TMP / MonoBehaviour View
- Drag / Popup / Tooltip / Animation
- DOTween / Feel
- 只负责输入、显示、动画

Application
- TurnFlowService
- CardPlayService
- SettlementService
- SettlementPresentationMapper
- RewardService / ShopService
- 用 UniTask 编排流程，用 EventBus 发布事实事件

Domain
- RunState
- Card / Deck / Board / Room / Economy / Settlement / RandomEvent
- 纯 C# 状态、规则、校验、结果模型

Infrastructure
- VContainer Installer
- EventBus
- Excel Importer / ScriptableObject Database
- Martian.Localization / Martian.Save
- Unity ObjectPool<T>
```

核心规则：

- MonoBehaviour 不拥有玩法规则。
- 命令式动作直接调用注入的 Application Service。
- `EventBus` 只发布已经发生的事实事件，不用事件代替命令。
- Service 不直接依赖 UI。
- Domain 不依赖 Unity 表现层。
- 跨模块依赖通过 VContainer 注入。
- 所有主流程等待返回 `UniTask`，并传入 `CancellationToken`。

## 3. 技术选型

| 问题 | 技术选型 | 结论 |
|---|---|---|
| 主循环 / 异步流程 | UniTask | 替代 coroutine、callback、pending count |
| 依赖管理 | VContainer | 替代 `Singleton.Instance` 和场景内隐式查找 |
| 跨层事件 | 保留 EventBus | 只发事实事件，不当 Command 用 |
| UI 响应式 | 不引入 R3 | 当前事件刷新已够用 |
| FSM | 移除 NodeCanvas 主流程 | `TurnFlowService.RunAsync` 成为流程真源 |
| 动画 | DOTween 保留 | 配合 UniTask await tween 完成 |
| 手感反馈 | Feel 保留 | 作为表现层可选增强 |
| 对象池 | Unity `ObjectPool<T>` | 替换字符串 tag 池和频繁 Destroy |
| 数据管线 | 不引入 Luban | 保留 Excel -> Importer -> ScriptableObject |
| 本地化 / 存档 | Martian 模块保留 | 已接线能力不重复造 |
| 卡牌效果 | Strategy + Factory 保留 | 补 scope 和参数校验 |

## 4. 核心接口

```csharp
public interface ITurnFlowService
{
    UniTask RunAsync(CancellationToken ct);
}

public interface ICardPlayService
{
    CardPlayValidationResult ValidatePlay(CardInstance card, RoomSlot targetRoom);
    UniTask<CardPlayResult> PlayAsync(CardInstance card, RoomSlot targetRoom, CancellationToken ct);
}

public interface ISettlementService
{
    UniTask<SettlementResult> ResolveAsync(SettlementRequest request, CancellationToken ct);
}

public interface ISettlementPresentationMapper
{
    UISettlementPlaybackBatch Map(SettlementResult result);
}

public interface ISettlementPresentationService
{
    UniTask PlayAsync(UISettlementPlaybackBatch batch, CancellationToken ct);
}

public interface IRewardService
{
    UniTask<RewardChoiceResult> OfferAndWaitChoiceAsync(bool boosted, CancellationToken ct);
}

public interface IShopService
{
    bool IsOpen { get; }
    bool OpenedThisTurn { get; }
    bool ClosedThisTurn { get; }
    void Open();
    bool TryPurchase(int offerIndex);
    void Close(int turnNumber);
    void ResetForNewTurn();
    UniTask OpenAndWaitCloseAsync(CancellationToken ct);
}
```

`RunState` 是纯数据状态，不持有 `MoneyManager / BoardManager / DeckManager` 等系统引用：

```csharp
public sealed class RunState
{
    public int CurrentTurn { get; set; }
    public GamePhase CurrentPhase { get; set; }
    public bool IsGameOver { get; set; }

    public EconomyState Economy { get; set; }
    public DeckState Deck { get; set; }
    public BoardState Board { get; set; }
    public RewardState Reward { get; set; }
    public ShopState Shop { get; set; }
    public RandomEventState RandomEvent { get; set; }
}
```

`GameContext` 只作为旧卡牌效果兼容对象过渡存在；长期由 `RunState + Service` 取代。

## 5. 模块逻辑

### Boot / Composition

`GameLifetimeScope` 注册所有 Service、Repository、Adapter、Pool。`GameBootstrapper` 加载并校验配置，创建初始 `RunState`，调用 `TurnFlowService.RunAsync(ct)`。

### Turn Flow

`TurnFlowService` 替代 NodeCanvas 主流程：

```text
RunAsync
-> PrepareAsync
-> ActionAsync
-> SettleAsync
-> RewardAsync / ShopAsync / RandomEventAsync
-> EndTurnAsync
-> Loop
```

它只负责编排，不写出牌、结算、奖励细节。

### Card Play

`CardPlayService` 接管 `ValidatePlay / PlayCard`：

```text
ValidatePlay(card, target)
-> PayCost
-> PlaceCard / ResolveInstant / Discard
-> ExecuteEffect
-> Publish CardPlayed / MoneyChanged / BoardChanged
```

拖拽预览调用 `ValidatePlay`；正式提交必须调用 `PlayAsync` 并二次校验。`UICardDragController.OnEndDrag()` 直接调用注入的 `ICardPlayService.PlayAsync()`，不发布“PlayCardEvent”。

### Settlement

`SettlementService` 只算规则，只返回 `SettlementResult`：

```text
ResolveAsync
-> Room settlement
-> Contract settlement
-> Durability / destroy
-> Loan
-> SettlementResult
```

表现格式由 `SettlementPresentationMapper` 转换：

```text
SettlementResult
-> UISettlementPlaybackBatch
-> SettlementPresentationService.PlayAsync()
-> SettlementPlaybackCompleted event
```

`UISettlementPlaybackBatch` 属于 Presentation DTO，禁止在 `SettlementService` 内创建。

### Reward / Shop

`RewardService` 生成奖励选项、等待 UI 选择、发放卡牌。`ShopService` 生成商品、处理购买、关闭和临时牌清理。UI 只提交玩家选择，不直接改手牌、金钱或商店状态。

### Effect System

保留 `CardEffectFactory` 和现有 Strategy。新增：

- `EffectArgs`：统一参数解析和错误信息。
- `EffectExecutionScope`：管理 `SelectedRoom / SettlementCapture / ExtraSettlement`。
- `EffectMoney`：统一加钱、扣钱、结算捕获、日志 sourceName。

### Presentation / Pool

`UIManager` 降级为 UI 组合入口。缺失主流程 UI 引用直接 fail-fast；Tooltip、Feel、额外 popup 可降级。

建立局部类型池：

- `FeedbackPopupPool`
- `CardGhostPool`
- `SettlementNumberPool`
- `TransientEffectPool`

对象播完 `Release`，`OnGet / OnRelease` 清理 tween、事件、文本、alpha、parent、interactable 状态。

### Data / Config

继续使用：

```text
Excel
-> Editor Importer
-> CardData / CardLibrary / RandomEvent ScriptableObject
-> Runtime Database LoadAll
```

启动期校验仍是主防线：卡牌效果、目标类型、牌库引用、奖励池、商店池、随机事件库错误直接中断。

## 6. 分批迁移顺序

### Phase 0：建分支与文档真源

- 创建分支 `codex/refactor-mvcs-vcontainer-unitask`。
- 新增本专题文档。
- 更新文档索引，标记方案为执行中。

验收：

- 能在 `Doc/20_专题方案/` 下找到本方案。
- 文档明确写清不引入 QFramework、R3、MessagePipe、Luban。
- 文档明确写清 `SettlementService -> SettlementResult -> Mapper -> UISettlementPlaybackBatch -> PresentationService`。

### Phase 1：CardPlayService

- 新增 `ICardPlayService / CardPlayService`。
- 承接现有 `ValidatePlay` 和 `PlayCard` 逻辑。
- `TurnManager` 暂时保留兼容门面，内部转调 `ICardPlayService`。
- `UICardDragController` 改为注入并直接调用 `ICardPlayService.PlayAsync()`。
- 保留正式提交二次校验。

验收：

- 合法卡仍能正常出牌。
- 非法目标仍回弹。
- 金钱不足仍提示。
- 0 费用卡不产生误导 warning。
- `TurnManager.ValidatePlay / PlayCard` 只作为兼容入口存在。

### Phase 2：SettlementService + Mapper + PresentationService

- 新增 `ISettlementService.ResolveAsync()`，只返回 `SettlementResult`。
- 新增 `ISettlementPresentationMapper.Map(SettlementResult)`。
- 新增或重构 `ISettlementPresentationService.PlayAsync(UISettlementPlaybackBatch, ct)`。
- 禁止 `SettlementService` 创建 `UISettlementPlaybackBatch`。

验收：

- 房间收益、合同收益、耐久销毁、贷款逻辑结果不变。
- 结算跳字顺序不变。
- `SettlementService` 不引用 UI 命名空间。
- `SettlementPresentationMapper` 是唯一 UI batch 映射点。

### Phase 3：RewardService / ShopService

- 新增 `IRewardService.OfferAndWaitChoiceAsync(bool boosted, ct)`。
- 新增 `IShopService.OpenAndWaitCloseAsync(ct)`。
- UI 只提交玩家选择，不直接改手牌、金钱或商店状态。
- `TurnManager` 暂时通过服务转调，保持旧主流程可跑。
- 过渡期 `TurnManager` 仍保留默认 service 实例和 `Construct(...)` 手动注入入口；正式 VContainer 注入留到 Phase 4 单独验收。

验收：

- 结算后奖励三选一正常出现。
- 选择奖励、跳过奖励都能进入下一回合。
- 商店打开、购买、关闭行为不变。
- 奖励/商店逻辑不再直接堆在 `TurnManager`。

### Phase 4：VContainer 基础接入

- 引入 VContainer。
- 新增 `GameLifetimeScope`。
- 注册 `CardPlayService`、`SettlementService`、`RewardService`、`ShopService`、Presentation Services。
- 先不强行删除所有 Singleton，只迁移已拆出的服务。
- 新增玩法服务必须构造注入，不新增玩法型 `XxxManager`。

验收：

- 场景启动能解析所有已注册服务。
- 缺关键服务时启动期 fail-fast。
- 已拆服务通过 VContainer 获取依赖，不靠运行时 `Find`。

### Phase 5：TurnFlowService 替代 NodeCanvas 主流程

- 新增 `ITurnFlowService.RunAsync(ct)`。
- 主流程改为 `PrepareAsync -> ActionAsync -> SettleAsync -> Reward / Shop / RandomEvent -> EndTurnAsync -> Loop`。
- NodeCanvas 保留为调试/原型工具，但不再驱动正式回合。
- 旧 `TurnManager` 只保留兼容门面，确认无调用后删除。

验收：

- 开局能进入准备阶段。
- 抽牌、行动、结算、奖励、下一回合完整闭环。
- `TurnEnded` 只发布一次。
- GameOver 能停止 async loop。
- 禁用 NodeCanvas 后主流程仍能跑。

### Phase 6：ObjectPool<T> 表现对象池

- 新增局部类型池：`FeedbackPopupPool`、`CardGhostPool`、`SettlementNumberPool`、`TransientEffectPool`。
- 弹字、抽牌 Ghost、弃牌 Ghost、结算数字播完后 Release。
- `OnGet / OnRelease` 清理 tween、事件、文本、alpha、parent、interactable。

验收：

- 连续结算多回合无残留 popup。
- Ghost 卡不残留在层级中。
- Release 后再次取出状态干净。
- 旧全局字符串 `ObjectPooler` 不再服务这些 UI 表现对象。

## 7. 子任务分派原则

- 主 AI 保留架构边界、关键接口、集成回收和最终验收。
- 可并行子任务优先交给子 agent：测试样例整理、映射表整理、静态扫描、局部 pool 改造。
- 出牌链路、结算链路、VContainer 装配、TurnFlow 主循环由主 AI 主控。
- 每个阶段完成后暂停，不继续下一阶段，直到用户确认。

## 8. 最终验收场景

- 新开局完整跑 3 回合。
- 每回合包含抽牌、合法出牌、非法拖拽、结算、奖励选择。
- 至少一次商店流程。
- 至少一次随机事件流程。
- 至少一次贷款扣款。
- 至少一次卡牌销毁。
- Tooltip 运行时模块缺失时只降级显示，不阻断主流程。
- 存档读写仍可用。
- 项目文档同步更新：专题方案、核心流程、UI 系统、配置与数据管线相关说明。

## 9. 明确不做

- 不引入 QFramework。
- 不引入 R3。
- 不引入 MessagePipe。
- 不引入 Luban。
- 不在 Phase 0 修改业务代码。
- 不在阶段未验收前继续推进下一阶段。
