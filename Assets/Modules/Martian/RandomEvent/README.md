# Martian.RandomEvent

通用随机事件插件。类似群星(Stellaris)的弹窗事件系统：从事件库触发事件、展示弹窗、玩家选择选项、回调业务逻辑。

**命名空间：** `Martian.RandomEvent`
**位置：** `Assets/Modules/Martian/RandomEvent/Runtime/`
**依赖：** `Martian.EventBus`（同项目）、Unity（UnityEngine）

---

## 模块边界

| 模块内 | 模块外（业务层负责） |
|---|---|
| 事件数据定义 (SO) | 何时触发事件 |
| 事件库 / 注册表 | 选项效果执行 |
| 触发→排队→展示→回调 | 事件链调度 |
| 选项可用性过滤 | UI Prefab 搭建 |
| 动态描述插值 | 项目桥接代码 |

---

## 文件结构

```
Runtime/
├── RandomEventData.cs       # ScriptableObject 事件定义 + RandomEventOption
├── RandomEventLibrary.cs    # ScriptableObject 事件库（权重集合）
├── RandomEventDatabase.cs   # 静态注册表，LoadAll / GetById
├── RandomEventRequest.cs    # 触发请求封装（filter + args + callback）
├── RandomEventResult.cs     # 选择结果 struct（只读）
├── RandomEventMessages.cs   # EventBus 消息 + UI 展示数据包
└── RandomEventManager.cs    # 核心 Manager（FIFO 队列 + 流转）
```

---

## 快速开始

### 1. 创建事件数据

右键 `Assets` → `Create` → `Martian` → `Random Event Data`

| 字段 | 说明 |
|---|---|
| `eventId` | 唯一字符串 ID，如 `"event_tax_audit"` |
| `titleKey` | 标题文本，支持 `{0}` 占位符 |
| `descriptionKey` | 描述文本，支持 `{0}` `{1}` 占位符 |
| `artwork` | 事件图片（Sprite） |
| `options` | 选项列表，每项有 `optionId` / `displayTextKey` / `tooltipKey` |
| `tags` | 可选标签，业务侧过滤用 |

### 2. 创建事件库（可选）

右键 → `Create` → `Martian` → `Random Event Library`

- `libraryId`：库的唯一 ID
- `events`：拖入 RandomEventData，**重复条目 = 更高权重**

### 3. 项目初始化

```csharp
// 在项目启动时（如 GameManager.InitializeSystems）
RandomEventDatabase.LoadAll();
```

### 4. 场景配置

在场景中放置一个 GameObject，挂载 `RandomEventManager` 组件。

### 5. 实现 UI 面板

```csharp
// 在 UI 面板的 OnEnable 中
EventBus.Subscribe<RandomEventTriggered>(OnRandomEventTriggered);

private void OnRandomEventTriggered(RandomEventTriggered msg)
{
    var data = msg.DisplayData;
    // data.FormattedTitle     — 格式化后的标题
    // data.FormattedDescription — 格式化后的描述
    // data.SourceData.artwork — 事件图片
    // data.Options            — 选项列表（含 IsAvailable）
    Show(data);
}

// 玩家点击选项后
EventBus.Publish(new RandomEventOptionSelected
{
    EventInstanceId   = data.EventInstanceId,
    SelectedOptionId  = "option_id",
    SelectedOptionIndex = 0
});
```

---

## API 参考

### RandomEventManager

```csharp
// 简单触发（fire-and-forget）
RandomEventManager.Instance.TriggerEvent("event_id");
RandomEventManager.Instance.TriggerEvent(eventDataSO);

// 完整触发
RandomEventManager.Instance.TriggerEvent(new RandomEventRequest
{
    Data            = eventData,
    DescriptionArgs = new object[] { 200 },          // 填充占位符
    OptionFilter    = opt => opt.optionId != "bribe" || HasMoney(500), // 选项条件
    OnComplete      = result => { /* 处理效果 */ }
});

// 从库随机抽取
RandomEventManager.Instance.TriggerRandomFromLibrary("library_id", onComplete);

// 状态查询
bool active = RandomEventManager.Instance.IsEventActive;
int queued  = RandomEventManager.Instance.QueuedCount;
```

### RandomEventDatabase

```csharp
RandomEventDatabase.LoadAll();                          // 初始化（必须首先调用）
RandomEventData evt = RandomEventDatabase.GetEventById("event_id");
RandomEventLibrary lib = RandomEventDatabase.GetLibraryById("lib_id");
```

---

## EventBus 消息

| 消息类型 | 发布方 | 订阅方 |
|---|---|---|
| `RandomEventTriggered` | `RandomEventManager` | UI 面板 |
| `RandomEventOptionSelected` | UI 面板 | `RandomEventManager`（内部）+ 业务代码（可选） |

---

## 动态描述（占位符）

```
// SO 配置
titleKey        = "税务检查"
descriptionKey  = "税务官要求缴纳 {0} 金币。拒绝将面临 {1} 金币罚款。"

// 触发时传入
DescriptionArgs = new object[] { 200, 500 }

// UI 展示
"税务官要求缴纳 200 金币。拒绝将面临 500 金币罚款。"
```

---

## 失败行为

| 场景 | 行为 |
|---|---|
| `LoadAll()` 前查询 | 抛 `InvalidOperationException` |
| `eventId` 不存在 | 返回 `null` + `LogWarning` |
| `Instance` 场景中不存在 | 返回 `null` + `LogError` |
| `TriggerEvent(null)` | 抛 `ArgumentNullException` |
| `OptionFilter` 过滤所有选项 | 展示事件，选项全部灰显（由 UI 处理） |

---

## 事件链（业务层示例）

插件本身不管理链式逻辑，由业务回调中触发下一个事件：

```csharp
RandomEventManager.Instance.TriggerEvent(new RandomEventRequest
{
    Data = RandomEventDatabase.GetEventById("chain_step_1"),
    OnComplete = result =>
    {
        if (result.SelectedOptionId == "investigate")
        {
            // 业务决定时机，立即或延迟触发下一步
            RandomEventManager.Instance.TriggerEvent("chain_step_2");
        }
    }
});
```

---

## 注意事项

- 插件**不修改**任何项目文件（GameEvents、GameManager、UIManager 等）
- `RandomEventOptionSelected` 由 **UI 面板发布**，Manager 内部订阅用于回调分发
- 如同时使用 `OnComplete` 回调和 EventBus 订阅，结果会被接收两次，选一种即可
