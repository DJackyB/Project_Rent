# UI 运行时保底剔除清单

## 背景与目标

- 当前项目里仍有不少 UI 运行时保底逻辑，包括裸 `new GameObject`、运行时 `AddComponent`、缺引用时动态补节点，以及部分临时容器自建。
- 这份文档用于长期追踪 UI 去运行时保底的改造过程，同时记录每一批的代码修改、资源配置与验收结果。
- 当前策略是优先剔除“高频、易错、影响排查效率”的 UI 保底；动态内容仍允许实例化正式 prefab。

## 总清单

| 编号 | 项目 | 状态 | 备注 |
| --- | --- | --- | --- |
| UI-01 | TopBar 固定化 | 待处理 | 去掉运行时生成文本标签 |
| UI-02 | PhasePanel 固定化 | 待处理 | 补齐 `phaseText` 和按钮壳 |
| UI-03 | GameOverPanel 固定化 | 待处理 | 补齐 `titleText / infoText / panel` |
| UI-04 | Board PlayAreaDropZone 场景化 | 待处理 | 去掉公共投放区运行时创建 |
| UI-05 | ContractPanel 预置化 | 待处理 | 去掉合同面板运行时创建 |
| UI-06 | UIManager 严格依赖场景对象 | 待处理 | 去掉控制器运行时创建 |
| UI-07 | Card.prefab 完整化 | 已完成待验收 | 已落地 |
| UI-08 | Card 交互组件预挂载 | 已完成待验收 | `TooltipTrigger` / `UICardDragHandler` 已走 prefab 约束 |
| UI-09 | 设备紧凑卡 prefab 化 | 待处理 | 下一批再拆 |
| UI-10 | Room.prefab 完整化 | 待处理 | 补齐 drop / highlight / anchor |
| UI-11 | Slot prefab 化 | 待处理 | Tenant / Equipment Slot 拆 prefab |
| UI-12 | HandPanel 严格依赖容器 | 待处理 | 去掉 `HandContainer` runtime 创建 |
| UI-13 | Tooltip 运行时场景依赖剔除 | 已完成待验收 | 已不再要求 `HoverPreviewRoot` |
| UI-14 | Tooltip 宿主 no-op 化 | 已完成待验收 | 模块缺席时宿主自动静默降级 |
| UI-15 | SettlementSequence 场景化 | 待处理 | 去掉 sequencePlayer / runtime bubble 自建 |
| UI-16 | DragController 严格绑定 | 待处理 | 当前仅完成卡牌 prefab 本体 |
| UI-17 | DropZone 视觉预置 | 待处理 | Board / Room 仍未处理 |
| UI-18 | 中文字体资源固化 | 待处理 | 需要正式 TMP Font Asset |
| UI-19 | CardSkinDatabase 资源固化 | 已完成待验收 | 已接入正式卡面与边框资源 |
| UI-20 | 占位 Sprite 正式资源化 | 待处理 | 当前仍有白图兜底 |

## 当前批次

- 批次名称：Tooltip 模块可选化与抽离
- 范围：
  - `UI/Common/Tooltip/`
  - `UI/Common/Tooltip/Runtime/`
  - `UICardView`
  - `UIEquipmentCardView`
  - `UIManager`
  - `UICardDragController`
  - `Card.prefab`
  - `SampleScene`
- 不在本批范围：
  - TopBar / Phase / GameOver / Board 固定场景 UI
  - Room / Slot / Equipment 紧凑卡 prefab
  - SettlementSequence UI

## 代码改动记录

### 1. Tooltip 宿主稳定层

- 新增 `ITooltipService`、`ITooltipContentProvider`、`TooltipRequest`、`TooltipContent`、`TooltipServices`、`NullTooltipService`。
- 宿主项目现在即使缺失 Tooltip 运行时模块，也能通过 `NullTooltipService` 安全运行。

### 2. Tooltip Runtime

- Tooltip root 改为运行时按所属 `Canvas` 懒创建。
- 不再要求场景中存在 `HoverPreviewRoot`。
- presenter 通过注册表选择，不再由业务代码直接持有具体 controller。

### 3. 业务侧接入

- `UICardView` 改为输出 `TooltipRequest`。
- 手牌、房间租客、房间设备、合同区统一走 `TooltipTrigger + ITooltipContentProvider`。
- `UIManager`、`UICardDragController` 改为统一调用 `TooltipServices.Current.HideAll()`。

## 手动配置记录

### 1. `Card.prefab`

- 应预挂：
  - `UICardView`
  - `TooltipTrigger`
  - `UICardDragHandler`
  - `CanvasGroup`
  - `LayoutElement`
- `UICardView` 继续要求显式绑定：
  - `background`
  - `frameImage`
  - `artImage`
  - `skinDatabase`

### 2. `SampleScene`

- 已移除旧的 `HoverPreviewRoot` 依赖。
- 不再要求手工放置 Tooltip Overlay 根节点。
- Tooltip Runtime 会在首次显示时自动创建 `TooltipRuntimeRoot`。

## 验收记录

- [ ] 手牌正常显示
- [ ] 房间、合同区正常显示
- [ ] 手牌 / 房间租客 / 房间设备 / 合同区 Tooltip 正常显示
- [ ] 拖拽开始、阶段切换、回合切换、GameOver 时 Tooltip 自动关闭
- [ ] Console 无 Tooltip 自动补节点异常或缺组件报错
- [ ] 临时移除 Tooltip Runtime 后，项目仍能正常进入场景并完成主流程

## 下一批计划

- 优先建议处理固定场景 UI 中最明确缺引用的一组：
  - `UIPhasePanel`
  - `UIGameOverPanel`
  - `UIBoardPanel.playAreaDropZone`
- 如果继续沿 prefab 链路向下收，则下一批建议：
  - `Room.prefab`
  - `UIRoomSlotView`
  - 设备紧凑卡 prefab
