# Tooltip模块抽离与复用方案_v1

**更新时间**：2026-03-25  
**当前状态**：已完成 v1 落地，当前项目已切换到“宿主 no-op 契约层 + 可抽离运行时模块 + 卡牌适配层”结构。

## 1. 目标

- 让 Tooltip 从当前项目中变成可选模块。
- 当 Tooltip 运行时模块缺席时，游戏流程、UI 刷新、拖拽出牌、结算推进都继续运行。
- 未来迁移到别的项目时，优先复用通用 Tooltip Core，只替换内容 provider 与 presenter。

## 2. 抽离边界

### 2.1 宿主稳定层

位置：`Assets/_Assets/Scripts/UI/Common/Tooltip/`

保留内容：

- `ITooltipService`
- `ITooltipContentProvider`
- `TooltipRequest`
- `TooltipContent`
- `TooltipPlacementMode`
- `TooltipTrigger`
- `TooltipServices`
- `NullTooltipService`

职责：

- 作为业务层的长期稳定接口。
- 即使 Tooltip 运行时模块被整体移除，宿主也仍能编译、运行和静默降级。

### 2.2 Tooltip 运行时 Core

位置：`Assets/_Assets/Scripts/UI/Common/Tooltip/Runtime/`

职责：

- 在运行时自动注册 `TooltipRuntimeService`。
- 首次显示时根据 `Anchor` 所在 `Canvas` 懒创建 `TooltipRuntimeRoot`。
- 通过 presenter 注册表选择内容展示实现。
- 提供 `FollowPointer` 和 `AnchorRect` 定位能力。

约束：

- 不直接依赖当前项目的卡牌类型、文案常量、prefab 路径或场景节点。
- 不要求 `SampleScene` 预先存在 `HoverPreviewRoot`。

### 2.3 当前实现中的卡牌适配

位置：`Assets/_Assets/Scripts/UI/Common/Tooltip/Runtime/CardTooltipPresenter.cs`

职责：

- 处理 `TooltipContentKind.Card`。
- 复用当前项目卡牌 prefab 视觉，生成卡牌放大 Tooltip。

当前实现说明：

- 为了保证 Tooltip Runtime 能整块移除而不让宿主反向依赖，卡牌 presenter 目前放在 Runtime 模块内。
- presenter 通过反射读取 `UICardView` 与 `CardViewContext.TooltipPreview`，避免在宿主主程序集之外再引入新的强编译依赖。
- 宿主项目只需要负责产出 `TooltipContentKind.Card` 和对应 payload。

## 3. 运行时行为

### 3.1 正常安装时

- `TooltipRuntimeService` 在启动时自动注册到 `TooltipServices.Current`。
- `TooltipTrigger` 在指针进入时向 provider 索取 `TooltipRequest`。
- 运行时 Core 选择匹配 presenter 并显示 Tooltip。

### 3.2 模块缺席时

- `TooltipServices.Current` 自动退回 `NullTooltipService`。
- `TooltipTrigger`、`UIManager`、`UICardDragController` 等调用链继续存在。
- 所有 Tooltip 调用静默 no-op，不影响游戏逻辑。

## 4. 当前项目接入点

- `UICardView` 负责生成卡牌 Tooltip 请求。
- 手牌、房间租客、房间设备、合同区统一通过 `TooltipTrigger` 触发。
- `UIManager` 与 `UICardDragController` 在刷新或状态切换时统一调用 `TooltipServices.Current.HideAll()`。
- `SampleScene` 不再依赖手工放置的 Tooltip Overlay 根节点。

## 5. 迁移到新项目时的建议步骤

1. 复制宿主稳定层与 Tooltip Runtime Core。
2. 在新项目里实现新的 content provider。
3. 针对目标内容注册新的 presenter factory。
4. 验证 no-op 降级：移除 runtime Core 后宿主项目是否仍能运行。

## 6. 当前未做项

- 未支持 `UI Toolkit`。
- 未支持移动端长按触发。
- 未支持手柄焦点 Tooltip。
- 未把 `TooltipContentKind` 扩展到卡牌以外的内容类型。
