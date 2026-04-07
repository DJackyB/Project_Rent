# Feel 调用规则

本文档规范项目中 Feel（MoreMountains Feedbacks）的使用方式，确保所有 Feel 调用收口在集成层，不污染业务逻辑。

## 核心原则

1. **Feel 是可选视觉增强层**，不是反馈系统的真源。真源仍是 `Martian.Feedback`。
2. **Feel 不参与时序控制**。所有 handle 由主后端（FloatingText）提供，Feel 只做 fire-and-forget 播放。
3. **Feel 不播放音频**。音频真源是 `Martian.Audio` + `AudioEventBridge` + `AudioCatalog`。所有 MMF_Player 中的 Audio feedback 必须禁用。
4. **玩法核心脚本不直接依赖 `MoreMountains.*`**。所有 Feel 调用收口在 `Assets/_Assets/Scripts/Integration/Feel/`。

## 两种调用路径

### 路径 A：走现有反馈链路（自动触发）

适用于已通过 `BaoZuPoFeedbackAdapter` 接入的反馈类型：金钱增减、结算步骤、贷款扣款。

```
业务层 → BaoZuPoFeedbackAdapter → FeedbackService.Publish()
  → FeedbackPlaybackCoordinator
    → CompositeFeedbackPlaybackBackend
      ├── FloatingText（主，控时序）
      └── FeelFeedbackBackend（副，视觉增强）
```

**不需要改任何业务代码**。只要 `BaoZuPoFeelFeedbackInstaller` 已挂载且对应 slot 的 MMF_Player 已配置，Feel 效果就会自动触发。

对应 slot 映射：

| FeedbackCategory | FeelFeedbackSlots 常量 |
|---|---|
| `Money` | `MoneyDelta` |
| `Cost` | `MoneyDelta` |
| `Settlement` | `SettlementStep` |
| `Loan` | `LoanPayment` |

### 路径 B：UI 层直接调用（手动触发）

适用于不走 FeedbackCategory 的纯 UI 视觉效果：出牌成功、奖励揭示。

```csharp
// 获取 Installer 引用（通过 SerializeField 或 FindFirstObjectByType 缓存）
[SerializeField] private BaoZuPoFeelFeedbackInstaller _feelInstaller;

// 优先带位置播放，让视觉反馈贴近卡牌或面板锚点
_feelInstaller.FeelBackend.PlaySlotAt(FeelFeedbackSlots.CardPlay, cardTransform.position, "CardPlay");
_feelInstaller.FeelBackend.PlaySlotAt(FeelFeedbackSlots.RewardReveal, panelTransform.position, "RewardReveal");
```

**调用位置建议**：
- `UICardDragController.CommitPlay()` 出牌成功时 → `FeelFeedbackSlots.CardPlay`，优先取 `DropAnchor.position`，没有则退回卡牌当前位置
- `UICardRewardPanel.Show()` → 面板中心触发 `RewardReveal`
- `UICardRewardPanel.OnCardClicked()` → 所选卡牌位置再次触发 `RewardReveal`

**注意**：调用前检查 `_feelInstaller != null`（Installer 可能不存在），确保 Feel 缺失时不影响功能。

## 新增 Feel 效果的步骤

### 新增一个走反馈链路的 slot

1. 在 `FeelFeedbackSlots.cs` 添加新常量。
2. 在 `FeelFeedbackBackend.ResolveSlot()` 添加 `FeedbackCategory → slot` 映射。
3. 在 `BaoZuPoFeelFeedbackInstaller` 添加对应的 `[SerializeField] private MMF_Player` 字段。
4. 在 `RegisterPlayers()` 和 `ValidatePlayerReferences()` 中注册新字段。
5. 创建 MMF_Player 预制体，放在 `Assets/_Assets/Prefabs/Feedback/Feel/`。
6. 在场景中 Installer 组件上拖入**场景内 MMF_Player 实例**引用；prefab 仅作为复用模板，不让场景字段直接指向 prefab asset。

### 新增一个 UI 直接调用的 slot

1. 在 `FeelFeedbackSlots.cs` 添加新常量。
2. 在 `BaoZuPoFeelFeedbackInstaller` 添加 `[SerializeField] private MMF_Player` 字段。
3. 在 `RegisterPlayers()` 和 `ValidatePlayerReferences()` 中注册。
4. 创建 MMF_Player 预制体。
5. 在 UI 脚本中通过 `_feelInstaller.FeelBackend.PlaySlotAt(FeelFeedbackSlots.XXX, position, debugLabel)` 调用；只有不关心位置时才退回 `PlaySlot()`。

## MMF_Player 预制体配置规范

- 放置路径：`Assets/_Assets/Prefabs/Feedback/Feel/`
- 命名：`{SlotName}Feedback.prefab`（如 `MoneyDeltaFeedback`、`CardPlayFeedback`）
- **禁止**在 MMF_Player 中配置 Audio / Sound 类 feedback
- **禁止**使用 Camera Shake（首轮不接 Cinemachine）
- 首轮只使用 Canvas/UI 级效果：Scale、Color、Position、CanvasGroup Alpha 等
- 每个 MMF_Player 应能独立播放，不依赖外部状态

## 当前场景接线

- 场景：`Assets/Scenes/SampleScene.unity`
- Installer：`Canvas/FeelFeedbackInstaller`
- Player Root：`Canvas/FeelFeedbackPlayers`
- 场景内播放器：
  - `MoneyDeltaFeedbackPlayer`
  - `SettlementStepFeedbackPlayer`
  - `LoanPaymentFeedbackPlayer`
  - `CardPlayFeedbackPlayer`
  - `RewardRevealFeedbackPlayer`
- 已接 UI：
  - `UICardDragController.feelFeedbackInstaller`
  - `UICardRewardPanel._feelFeedbackInstaller`

## 禁止事项

| 禁止 | 原因 |
|------|------|
| 业务脚本直接 `using MoreMountains.Feedbacks` | 依赖不收口，拆除 Feel 时需要改业务代码 |
| MMF_Player 中启用 Audio feedback | 音频真源是 Martian.Audio，不能有第二套 |
| Feel 后端返回有效 handle 参与时序 | FeedbackPlaybackHandle.Complete() 是 internal，且设计上 Feel 不控制时序 |
| 在 `Martian/Feedback/` 目录下写 Feel 相关代码 | 模块本体和项目集成必须分离 |
| 直接构造 `FeelFeedbackBackend` 绕过 Installer | Installer 负责生命周期管理和 Composite 组装 |

## 降级行为

| 场景 | 行为 |
|------|------|
| Installer 未挂载 | 完全透明，FloatingText 照常，无 Feel 增强 |
| 某 MMF_Player 未配置 | 该 slot 跳过，其他 slot 正常 |
| MMF_Player.PlayFeedbacks() 抛异常 | Composite 吞掉异常 + LogError，主后端不受影响 |
| Feel 包被移除 | 删除 `Integration/Feel/` 目录 + 移除 asmdef 引用即可，零业务代码改动 |
