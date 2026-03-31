# UI运行时保底剔除清单

> 状态说明：这是一份 UI 去运行时保底的长期 backlog 与阶段记录，不是当前实现真源。当前真实结构优先看 [06_UI系统.md](./06_UI系统.md) 和 [UI手动配置与验收.md](./UI手动配置与验收.md)。

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
| UI-08 | Card 交互组件预挂载 | 已完成待验收 | `TooltipTrigger / UICardDragHandler` 已走 prefab 约束 |
| UI-09 | 设备卡 prefab 预置化 | 待处理 | 下一批再收 |
| UI-10 | Room.prefab 完整化 | 待处理 | 补齐 drop / highlight / anchor |
| UI-11 | Slot prefab 化 | 待处理 | Tenant / Equipment Slot 拆 prefab |
| UI-12 | HandPanel 严格依赖容器 | 待处理 | 去掉 `HandContainer` runtime 创建 |
| UI-13 | Tooltip 运行时场景依赖剔除 | 已完成待验收 | 已不再要求 `HoverPreviewRoot` |
| UI-14 | Tooltip 宿主 no-op 化 | 已完成待验收 | 模块缺席时宿主自动静默降级 |
| UI-15 | SettlementSequence 场景化 | 待处理 | 去掉 runtime bubble 自建 |
| UI-16 | DragController 严格绑定 | 待处理 | 当前只完成卡牌 prefab 本体 |
| UI-17 | DropZone 视觉预置 | 待处理 | Board / Room 仍未完全处理 |
| UI-18 | 旧中文字体链路移除 | 已完成 | `UIFontCatalog`、字体工具和 `Assets/Resources/Fonts` 已删除 |
| UI-19 | CardSkinDatabase 资源固化 | 已完成待验收 | 已接入正式卡面与边框资源 |
| UI-20 | 占位 Sprite 正式资源化 | 待处理 | 当前仍有白图兜底 |
