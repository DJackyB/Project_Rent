# 项目可复用组件与抽象建议（短期项目版）

**更新日期**：2026-03-25
**重点原则**：1. 能用为主，不纠结性能。2. 积累简单、低耦合的小组件。

## 1. 已有的高价值可复用组件

这些组件已实现解耦，可直接复制到后续项目：

| 组件名 | 路径 | 说明 |
| :--- | :--- | :--- |
| **EventBus** | `Utilities/EventBus.cs` | 泛型事件总线。已加固：支持自动清理及 Domain Reload，删除死代码。 |
| **Singleton<T>** | `Core/Singleton.cs` | 标准 MonoBehaviour 泛型单例基类。 |
| **ObjectPooler** | `Utilities/ObjectPooler.cs` | 基础对象池系统，支持 `IPoolableObject` 接口回调。 |
| **Tooltip 宿主契约层** | `UI/Common/Tooltip/` | 提供 `ITooltipService`、`ITooltipContentProvider`、`TooltipTrigger`、`TooltipServices` 和 `NullTooltipService`，可在宿主项目长期保留。 |
| **Tooltip 运行时 Core** | `UI/Common/Tooltip/Runtime/` | 提供懒创建 root、定位、presenter 注册与 no-op 兼容的通用 Tooltip 运行时骨架。 |

## 2. 已启动实施的可复用组件

### A. Tooltip 系统

- **当前状态**：已落地第一版。
- **抽离边界**：
  - 宿主项目保留轻量接口、触发器和 no-op 服务。
  - Tooltip 运行时模块可整体移除，移除后游戏继续运行，只是不显示 Tooltip。
  - 当前项目卡牌放大预览位于 `CardTooltipPresenter`，作为项目适配层存在。
- **已完成的关键点**：
  - 业务侧不再直接依赖 `HoverPreviewController`。
  - Tooltip root 改为运行时懒创建，不再要求 `SampleScene` 里手工放置 `HoverPreviewRoot`。
  - 手牌、房间租客、房间设备、合同区统一走 `TooltipTrigger + ITooltipContentProvider`。
- **剩余限制**：
  - 当前项目的卡牌 presenter 仍通过复制卡牌 prefab 展示放大预览，这一部分不是通用 Core。
  - 目前只覆盖 `uGUI + EventSystem`，未扩展到 `UI Toolkit`、移动端长按或手柄焦点。

### B. UISequenceText 系统 (`UI/Common/Sequence/`)

- **价值**：按队列依次播放浮动文字（如伤害飘字、剧情对白提示）。
- **后续建议**：
  - 当前仍耦合 `UIFontCatalog`。
  - 如果后续有第二个项目复用需求，再把字体注入方式做成更独立的入口。

## 3. 架构风险点提醒（随笔）

1. **TurnManager 逻辑聚合度过高**：结算流程仍承担较多职责。若后续卡牌规则大幅复杂化，此块仍容易成为 Bug 集中点。
2. **UIManager 全局刷新**：目前 `RefreshAll` 在多个事件下触发，UI 量级变大后仍有潜在卡顿风险。
3. **Tooltip Core 与项目适配层边界要持续守住**：后续如果继续往 Tooltip Core 塞卡牌类型、文案或 prefab 假设，会重新失去跨项目复用价值。
