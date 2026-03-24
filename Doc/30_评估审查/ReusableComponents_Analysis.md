# 项目可复用组件与抽象建议（短期项目版）

**更新日期**：2026-03-24
**重点原则**：1. 能用为主，不纠结性能。2. 积累简单、低耦合的小组件。

## 1. 已有的高价值可复用组件

这些组件已实现解耦，可直接复制到后续项目：

| 组件名 | 路径 | 说明 |
| :--- | :--- | :--- |
| **EventBus** | `Utilities/EventBus.cs` | 泛型事件总线。已加固：支持自动清理及 Domain Reload，删除死代码。 |
| **Singleton<T>** | `Core/Singleton.cs` | 标准 MonoBehaviour 泛型单例基类。 |
| **ObjectPooler** | `Utilities/ObjectPooler.cs` | 基础对象池系统，支持 IPoolableObject 接口回调。 |

## 2. 具有抽象价值的潜在组件

建议在有空余时间或下个项目复用时进行以下微量抽象：

### A. HoverPreview 系统 (`UI/Common/Hover/`)
- **价值**：通用的悬浮预览/Tooltip 框架。
- **抽象建议**：
  - 目前 `HoverPreviewController` 硬编码创建了 `CardHoverPreviewPresenter`。
  - **改进**：改为 `Initialize(IHoverPreviewPresenter presenter)`。这样下个项目无论是预览卡牌、道具还是角色信息，都能复用同一套触发和位置计算代码。

### B. UISequenceText 系统 (`UI/Common/Sequence/`)
- **价值**：按队列依次播放浮动文字（如伤害飘字、剧情对白提示）。
- **抽象建议**：
  - 目前耦合了 `UIFontCatalog` 处理字体。
  - **改进**：改为在初始化时注入 `TMP_FontAsset`。移除对具体项目的 UI 常量依赖，变成纯粹的飘字组件。

## 3. 架构风险点提醒（随笔）

1. **TurnManager 逻辑聚合度过高**：结算流程（SettlePhase）约 170 行，承担了太多职责。若后续卡牌规则大幅复杂化，此块容易成为 Bug 集中点。
2. **UIManager 全局刷新**：目前的 `RefreshAll` 在多个事件下触发，若后续 UI 量级变大，可能出现卡顿（当前短期项目暂不影响）。
3. **三选一机制**：目前代码中为随机自动落入一张到手牌，待与新实现的 UI 进行对接，从同步式逻辑改为异步选择流程。
