# Martian Tooltip / Feedback 接入指南

## 1. 目标与边界

`Martian.Tooltip` 和 `Martian.Feedback` 是可复用模块。

- `Martian` 核心只负责通用协议、默认 runtime、少量通用 presets。
- 当前项目或未来项目的业务语义、anchor 解析、content id、事件转译，全部放在项目自己的 integration 层。
- `Martian` 核心不依赖 `EventBus`、`GameConfig`、`UIManager`、`FEEL` 或任何项目命名空间。
- `EventBus` 是项目可选入口，不是 `Martian` 的必经之路。
- `FEEL` 未来如果要接，只接在项目 integration 或自定义 backend 上，不进 `Martian` 核心。

当前仓内目录：

```text
Assets/Modules/Martian/
  Tooltip/
    Runtime/
    Presets/
  Feedback/
    Runtime/
    Presets/

Assets/Scripts/Runtime/Integration/Martian/
  Tooltip/
  Feedback/
```

## 2. 当前项目如何接入

### 2.1 Tooltip

当前项目的 Tooltip 接入点有三层：

1. 通用安装：

`[BaoZuPoMartianTooltipIntegration.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Tooltip/BaoZuPoMartianTooltipIntegration.cs)`

- 显式安装 `TooltipRuntimeInstaller`
- 注册默认文档型 tooltip preset
- 注册当前项目的卡牌预览 presenter

2. 业务内容构建：

`[UICardView.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/UI/UICardView.cs)`

- `UICardView` 实现 `ITooltipContentProvider`
- hover 时返回 `TooltipRequest`
- 当前项目通过 `BaoZuPoTooltipContentIds.CardPreview` 作为 content id

3. 项目专属 presenter：

`[BaoZuPoCardTooltipPresenter.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Tooltip/BaoZuPoCardTooltipPresenter.cs)`

- 根据 `CardPreview` content id 渲染卡牌预览
- 这类 presenter 是项目层代码，不属于 `Martian.Tooltip` 核心

当前项目启动时在 `[UIManager.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/UI/UIManager.cs)` 的 `Start()` 中调用：

```csharp
BaoZuPoMartianTooltipIntegration.Install();
```

### 2.2 Feedback

当前项目的 Feedback 接入点也有三层：

1. 配置映射：

`[BaoZuPoMartianFeedbackIntegration.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Feedback/BaoZuPoMartianFeedbackIntegration.cs)`

- 把 `GameConfig` 映射到 `FeedbackRuntimeOptions`
- 保留当前项目自己的 `enableMoneyFeedback` 语义

2. 业务转译：

`[BaoZuPoFeedbackAdapter.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Feedback/BaoZuPo/BaoZuPoFeedbackAdapter.cs)`

- 把当前项目的“房间/合同/贷款/即时收益”等业务语义转成 `FeedbackRequest` 或 `FeedbackSequenceRequest`
- 负责解析 target key、anchor、offset、category

3. 流程联动：

`[UISettlementSequenceController.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/UI/Settlement/UISettlementSequenceController.cs)`

- 监听结算事件
- 调用项目 adapter 发布反馈
- 等 `FeedbackPlaybackCoordinator.AllPlaybackCompleted` 后再刷新 UI

当前项目启动时在 `[UIManager.cs](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/UI/UIManager.cs)` 中：

- 保证存在 `FeedbackBootstrap`
- 调用 `BaoZuPoMartianFeedbackIntegration.Configure(...)`

## 3. 新项目如何接入 Tooltip

### 3.1 最小接入

如果新项目只想要通用文档型 tooltip：

1. 保留 `Assets/Modules/Martian/Tooltip`
2. 在项目启动时显式安装：

```csharp
TooltipRuntimeInstaller.Install();
TooltipDocumentPresetInstaller.Install();
```

3. 当鼠标悬浮某个 UI 元素时，返回：

```csharp
new TooltipRequest(
    owner,
    anchorRect,
    new TooltipContent(
        TooltipDocumentContentIds.Document,
        new TooltipDocument(
            title: "Iron Sword",
            subtitle: "Rare Weapon",
            summary: "A reliable weapon for early combat")));
```

4. 该 UI 元素实现 `ITooltipContentProvider`，并由 `TooltipTrigger` 驱动显示

### 3.2 自定义内容

如果新项目需要装备面板、技能词条、buff 说明等更复杂内容：

1. 在项目 integration 层定义自己的 `ContentId`
2. 实现 `ITooltipPresenter`
3. 实现 `ITooltipPresenterFactory`
4. 在项目启动时注册 factory
5. 由业务对象实现 `ITooltipContentProvider`

推荐规则：

- `Martian.Tooltip` 里只放跨项目通用的 payload 和 presenter
- 项目语义型 presenter 永远放在 integration
- 不要把装备、技能、卡牌等业务枚举写死进 `Martian.Tooltip`

## 4. 新项目如何接入 Feedback

### 4.1 最小接入

如果新项目只想直接触发默认浮字反馈：

1. 保留 `Assets/Modules/Martian/Feedback`
2. 在 UI 根节点下挂一个 `FeedbackBootstrap`
3. 启动时配置：

```csharp
bootstrap.Configure(new FeedbackRuntimeOptions
{
    EnableFeedback = true
});
```

4. 直接发请求：

```csharp
FeedbackServiceLocator.Current.Publish(new FeedbackRequest
{
    TargetKey = "player:hp",
    Anchor = playerHead,
    Text = "-12",
    NumericDelta = -12,
    Category = FeedbackCategory.Cost
});
```

### 4.2 使用通用 presets

如果只是简单的数字跳字或序列：

```csharp
var request = FeedbackPresets.SignedNumber("player:hp", -12, FeedbackCategory.Cost, playerHead);
FeedbackServiceLocator.Current.Publish(request);
```

```csharp
var sequence = FeedbackPresets.Sequence(
    "reward:chest",
    FeedbackPresets.Step("Gold", 120, FeedbackCategory.Money),
    FeedbackPresets.Step("Bonus", 150, FeedbackCategory.Settlement));

FeedbackServiceLocator.Current.PublishSequence(sequence);
```

### 4.3 项目适配层

当项目业务变复杂时，推荐做一层 adapter：

- 领域事件或业务代码产出项目语义
- integration 层把它翻译成 `FeedbackRequest`
- `Martian.Feedback` 只负责播

所以大项目里推荐：

```text
业务事件 -> integration adapter -> FeedbackRequest -> Martian.Feedback
```

小项目里可以直接：

```text
业务代码 -> FeedbackServiceLocator.Current.Publish(...)
```

### 4.4 自定义 backend

如果未来你想让反馈表现升级成 FEEL、特殊 Shader、特定项目动画系统：

1. 实现 `IFeedbackPlaybackBackend`
2. 在项目启动时：

```csharp
bootstrap.SetBackend(customBackend);
```

这样替换的是“怎么播”，不是“业务怎么发请求”。

## 5. 推荐的长期演进方式

建议把能力分成三层：

1. `Martian` 核心

- 协议
- runtime
- 少量跨项目通用 presets

2. `Martian.*.Presets`

- 真正已经跨多个项目复用过的高频预设
- 例如通用 signed number、generic sequence、document tooltip

3. 项目 integration

- content id
- presenter 注册
- anchor 解析
- 业务语义翻译
- 项目专属开关和配置映射

判断标准：

- 如果某能力离开当前项目还能成立，就考虑进 `Martian`
- 如果它依赖项目语义，就放 integration

## 6. 注意事项

- `Tooltip` 现在是显式安装，不会再自动 bootstrap。项目忘记安装时，trigger 会存在但不会真正显示内容。
- `Feedback` 的 `EnableMoneyFeedback` 不应再写进 `Martian` 核心语义；如果项目需要类似开关，放 integration 层做。
- `TooltipTrigger`、`FeedbackBootstrap`、`FeedbackPlaybackCoordinator` 已加迁移标记，目的是尽量降低旧命名空间迁移带来的脚本丢失风险。
- `Martian` 目录里如果出现项目命名空间引用，说明模块边界被污染，应优先回退到 integration。
- 当前默认 Feedback backend 是浮字/序列实现，不代表模块只能做 floating text；未来完全可以替换为 FEEL 或别的播放后端。

## 7. 当前仓内关键文件

- Tooltip 核心：
  `[Martian/Tooltip](/E:/SDL/PJ/Project_Rent/Assets/Modules/Martian/Tooltip)`
- Feedback 核心：
  `[Martian/Feedback](/E:/SDL/PJ/Project_Rent/Assets/Modules/Martian/Feedback)`
- 当前项目 Tooltip 接入：
  `[Scripts/Integration/Martian/Tooltip](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Tooltip)`
- 当前项目 Feedback 接入：
  `[Scripts/Integration/Martian/Feedback](/E:/SDL/PJ/Project_Rent/Assets/Scripts/Runtime/Integration/Martian/Feedback)`
- Tooltip 测试：
  `[Tests/EditMode/Tooltip](/E:/SDL/PJ/Project_Rent/Assets/Tests/EditMode/Tooltip)`
- Feedback 测试：
  `[Tests/EditMode/Feedback](/E:/SDL/PJ/Project_Rent/Assets/Tests/EditMode/Feedback)`

