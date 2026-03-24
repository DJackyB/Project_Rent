# UI 运行时保底剔除清单

## 背景与目标
- 当前项目里仍有不少 UI 运行时保底逻辑，包括裸 `new GameObject`、运行时 `AddComponent`、缺引用时动态补节点，以及 Hover 控制器自举等。
- 这份文档用于长期追踪 UI 去运行时保底的改造过程，同时记录每一批的代码修改、资源配置与验收结果。
- 当前策略是优先剔除 UI 侧的裸建节点和自动补组件；动态内容仍允许实例化正式 prefab。

## 总清单
| 编号 | 项目 | 状态 | 备注 |
| --- | --- | --- | --- |
| UI-01 | TopBar 固定化 | 待处理 | 去掉运行时生成文本标签 |
| UI-02 | PhasePanel 固定化 | 待处理 | 补齐 `phaseText` 和按钮壳 |
| UI-03 | GameOverPanel 固定化 | 待处理 | 补齐 `titleText / infoText / panel` |
| UI-04 | Board PlayAreaDropZone 场景化 | 待处理 | 去掉公共投放区运行时创建 |
| UI-05 | ContractPanel 预置化 | 待处理 | 去掉合同面板运行时创建 |
| UI-06 | UIManager 严格依赖场景对象 | 待处理 | 去掉控制器运行时创建 |
| UI-07 | Card.prefab 完整化 | 已完成待验收 | 本批已落地 |
| UI-08 | Card 交互组件预挂载 | 已完成待验收 | 本批已落地 |
| UI-09 | 设备紧凑卡 prefab 化 | 待处理 | 下一批再拆 |
| UI-10 | Room.prefab 完整化 | 待处理 | 补齐 drop / highlight / anchor |
| UI-11 | Slot prefab 化 | 待处理 | Tenant / Equipment Slot 拆 prefab |
| UI-12 | HandPanel 严格依赖容器 | 待处理 | 去掉 `HandContainer` runtime 创建 |
| UI-13 | HoverPreviewController 场景化 | 已完成待验收 | 本批已落地 |
| UI-14 | HoverPreviewPresenter 预置化 | 已完成待验收 | 本批已落地 |
| UI-15 | SettlementSequence 场景化 | 待处理 | 去掉 sequencePlayer / runtime bubble 自建 |
| UI-16 | DragController 严格绑定 | 待处理 | 当前仅完成卡牌 prefab 本体 |
| UI-17 | DropZone 视觉预置 | 待处理 | Board / Room 仍未处理 |
| UI-18 | 中文字体资源固化 | 待处理 | 需要正式 TMP Font Asset |
| UI-19 | CardSkinDatabase 资源固化 | 已完成待验收 | 本次已接入正式卡面与边框资源 |
| UI-20 | 占位 Sprite 正式资源化 | 待处理 | 当前仍有白图兜底 |

## 当前批次
- 批次名称：第一批，`Card.prefab` + 悬浮预览去运行时保底
- 本次追加：卡面与边框资源正式接入 `CardSkinDatabase`
- 范围：
  - `UICardView`
  - `UICardDragHandler`
  - `CardHoverPreviewPresenter`
  - `HoverPreviewController`
  - `HoverPreviewTrigger`
  - `Card.prefab`
  - `SampleScene` 的 `HoverPreviewRoot`
  - `Assets/Resources/CardSkinDatabase.asset`
- 不在本批范围：
  - TopBar / Phase / GameOver / Board 固定场景 UI
  - Room / Slot / Equipment 紧凑卡 prefab
  - SettlementSequence UI

## 代码改动记录
### 1. `UICardView` 去 runtime visuals
- 删除运行时生成 `Frame / Illustration / TypeLabel / StatsLabel` 的逻辑。
- 新增显式 prefab 视觉引用：`frameImage`、`artImage`。
- `typeText`、`statsText` 不再允许靠 runtime label 补齐。
- Hover 逻辑不再 `AddComponent<HoverPreviewTrigger>()`。
- Drag 逻辑不再 `AddComponent<UICardDragHandler>()`。
- 缺少必要引用时改为一次性报错，提示修 prefab。

### 2. `UICardDragHandler` 去 auto-add
- 不再补 `CanvasGroup`。
- 不再补 `LayoutElement`。
- `Bind` 时若缺少必备组件则直接报错并禁用。
- 增加 `[RequireComponent(typeof(LayoutElement))]`，强化 prefab 约束。

### 3. `CardHoverPreviewPresenter` 去 CanvasGroup fallback
- 预览对象仍通过实例化正式卡牌对象生成。
- 不再补 `CanvasGroup`。
- 缺少 `CanvasGroup` 时直接报错。
- 预览时统一关闭 `Graphic` 射线、按钮、布局组件、hover trigger、drag handler。

### 4. `HoverPreviewController` 去 singleton / bootstrap
- 不再运行时创建全局 `HoverPreviewController` 对象。
- 不再创建独立 Canvas、Presenter child。
- 改为只消费场景内已存在的 `HoverPreviewRoot`。
- 改为消费场景里已绑定的 `canvasRect` 和 `presenter`。

### 5. `CardSkinDatabase` 正式接入资源
- 新增 `Assets/Resources/CardSkinDatabase.asset`。
- 卡面映射已接入：
  - `Tenant -> CardBG_Tenant`
  - `Equipment -> CardBG_EquipTment`
  - `Event -> CardBG_Event`
  - `Contract -> CardBG_Contract`
- 边框映射已接入：
  - `Common -> Fram_Common`
  - `Rare -> Fram_Rare`
  - `Epic -> Fram_Epic`
- 当前卡数据里没有 `Legendary` 卡；默认边框先回落到 `Fram_Common`。
- `Card.prefab` 现已直接绑定该资源，不再只依赖 `Resources.Load` 兜底查找。

## 手动配置记录
### 1. `Card.prefab`
- 已补齐节点：
  - `Frame`
  - `Illustration`
  - `TypeLabel`
  - `StatsLabel`
- 已补齐组件：
  - `CanvasGroup`
  - `LayoutElement`
  - `HoverPreviewTrigger`
  - `UICardDragHandler`
- 已补齐 `UICardView` 绑定：
  - `background`
  - `frameImage`
  - `artImage`
  - `typeText`
  - `statsText`
  - `skinDatabase`

### 2. `SampleScene`
- 已新增 `HoverPreviewRoot`
- 已新增其子节点 `CardHoverPreview`
- 已将 `HoverPreviewController` 绑定到：
  - `canvasRect = Canvas`
  - `presenter = CardHoverPreviewPresenter`

### 3. 卡面与边框资源
- 已识别并接入 `Assets/_Assets/Arts/Sprites/Card` 下的正式资源。
- 当前资源覆盖了全部卡牌类型和当前已使用的全部稀有度档位。

## 验收记录
- [ ] 手牌正常显示
- [ ] 悬浮预览正常显示
- [ ] Console 无卡牌 / 悬浮预览相关自动补节点日志或缺组件报错
- [ ] 不同卡牌类型显示正确卡面
- [ ] 不同稀有度显示正确边框

## 下一批计划
- 优先建议处理固定场景 UI 中最明确缺引用的一组：
  - `UIPhasePanel`
  - `UIGameOverPanel`
  - `UIBoardPanel.playAreaDropZone`
- 如果继续沿 prefab 链路向下收，则下一批建议：
  - `Room.prefab`
  - `UIRoomSlotView`
  - 设备紧凑卡 prefab
