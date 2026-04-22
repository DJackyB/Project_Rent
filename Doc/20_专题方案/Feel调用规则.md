# Feel 调用规则

## 2026-04-08 当前状态

- Feel 只保留框架入口：`CompositeFeedbackPlaybackBackend`、`FeelFeedbackBackend`、`FeelFeedbackSlots`、`BaoZuPoFeelFeedbackInstaller`。
- 当前没有注册任何 `MMF_Player`，场景内也不保留 Feel 表现节点；所有 slot 调用均应 no-op。
- 当前没有 active Feel 视觉表现、测试 sprite 或一次性 Editor 工具。
- 后续恢复效果时仍按“单个 slot -> 独立 Play 验证 -> 接回 installer -> gameplay 验证”的顺序执行。

## 长期规则

- 修改 Feel/MMFeedbacks 前，先阅读官方资料并检查项目当前效果列表：
  - https://feel-docs.moremountains.com/mmfeedbacks.html
  - https://feel-docs.moremountains.com/list_mmfeedbacks.html
  - https://feel-docs.moremountains.com/API/index.html
- `Martian.Feedback` 是反馈语义和时序真源；Feel 只做可选视觉增强。
- Feel 不负责音频、haptics，也不引入 Cinemachine。音频继续归 `Martian.Audio`。
- 业务核心脚本不直接 `using MoreMountains.*`。Feel 相关依赖收口在 `Assets/Scripts/Runtime/Integration/Feel/` 和 UI 表现层。
- `CardPlay` 只能代表确认出牌成功，不能被 hover、奖励选择或泛用卡片 commit 动画触发。
- 后续 Feel 默认是“对象效果”，不是“在某个点生成粒子”：只要反馈能归属于卡牌、房间、HUD 文本、按钮、奖励卡等具体 UI 对象，就优先把表现挂到该对象的 `RectTransform` 下播放。
- 卡牌/房间/HUD 这类对象本体效果优先使用 `PlaySlotAttached(slot, targetRect, debugLabel)`；`PlaySlotAt(slot, anchor.position, debugLabel)` 只用于没有稳定宿主对象、确实需要空间落点的少数效果；只有真正全局效果才考虑 `PlaySlot()`。

## UI juice 参考原则

参考 GDC Europe 2012 `Juice It or Lose It` 的核心思路：juicy 不是单个大特效，而是“少量输入触发多层清晰响应”。用于本项目时按以下规则落地：

- 反馈必须回应玩家刚做的事：点击、拖拽、释放、成功、失败、获得、扣除，都要让玩家知道系统听见了。
- 反馈优先叠在对象上：卡牌动卡牌，房间动房间，钱动 HUD 钱文本，奖励动奖励卡；不要默认在场景坐标上凭空生成粒子。
- 一次交互允许有多层小响应：scale、alpha、颜色、轻停顿、短线/闪光、文本跳变可以组合，但每层都要有语义。
- easing 比线性移动更重要；允许轻微 overshoot / squash / settle，但不要让 UI 显得橡皮或拖泥带水。
- 停顿也属于反馈：关键动作可以先停 0.1-0.3 秒让玩家看见命中，再继续结算或销毁。
- 声音非常有效，但本项目 Feel 层不接音频；需要声音时走 `Martian.Audio`，并和视觉同一语义触发。
- 屏幕 shake / 全局抖动属于强药，只给贷款扣款、破产、重大结算等重事件；普通 UI 交互不默认使用。
- 每个新增效果都要能单独开关、单独验证；禁用 Feel 后只失去增强，不改变玩法结果。

## 当前框架入口

- Installer：`Assets/Scripts/Runtime/Integration/Feel/BaoZuPoFeelFeedbackInstaller.cs`
- 后端：`Assets/Scripts/Runtime/Integration/Feel/FeelFeedbackBackend.cs`
- Slot 常量：`Assets/Scripts/Runtime/Integration/Feel/FeelFeedbackSlots.cs`
- Composite 后端：`Assets/Scripts/Runtime/Integration/Feel/CompositeFeedbackPlaybackBackend.cs`

## 恢复单个效果的步骤

1. 先确认要恢复的 slot 和触发点。
2. 在场景 Canvas 下创建一个独立 `MMF_Player` 测试对象，运行时点 Play 必须能直接看见效果。
3. 验证这个对象不依赖隐藏脚本、不播放音频、不阻挡 raycast、不使用大色块遮挡 UI。
4. 只在测试对象通过后，把它接到 `BaoZuPoFeelFeedbackInstaller` 的显式 serialized 字段。
5. 在 `Start()` 中只注册对应 slot。
6. Play Mode 验证：启用 installer 能通过 gameplay 看到效果，禁用 installer 后效果消失，游戏逻辑和 floating text 不变；如果是对象效果，要确认播放位置跟随目标对象而不是停在旧坐标或 drop anchor。
7. 更新本文档和接入计划。

