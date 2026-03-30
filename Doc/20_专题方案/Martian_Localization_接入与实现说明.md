# Martian Localization 接入与实现说明

## 目的

这是一份给开发者和后续 AI 用的总览文档。

主要回答三件事：

- 这套本地化现在怎么分层。
- 想改文本、字体、导表、语言切换时该看哪里。
- 当前哪些能力已经完成，哪些还需要在 Unity 里补配置。

## 当前原则

当前本地化遵循一个明确原则：

- 允许桥接层缺失时的服务保底。
- 不做配置层面的保底。
- 缺配置时尽早报错，不静默回退。

这条原则当前已经落实为下面这个边界：

1. 桥接层缺失  
如果 `Martian.Localization.Unity` 不存在，或 `com.unity.localization` 没安装，允许退回 `NullLanguageService + NullLocalizedTextService`，让项目仍可运行。

2. 本地化配置缺失  
只要桥接层存在，就要求 `Localization Settings`、`Locale`、`String Table`、entry 都必须配置完整；缺任何一项都直接报错。

3. 字体配置缺失  
字体必须通过 `LocalizationFontProfile` 显式配置；缺 profile、缺语言映射、映射无效时直接报错。

说明：

- `fallback` 字段仍然保留在数据结构里，它现在主要服务于“桥接层不存在”的情况。
- 一旦桥接层存在，系统不会再替你补齐缺失的本地化配置、表配置或字体配置。

## 分层结构

### 1. `Martian.Localization.Abstractions`

职责：

- 定义稳定接口和基础数据结构。
- 不依赖 Unity 官方本地化包。

核心文件：

- `Assets/_Assets/Martian/Localization/Abstractions/LocalizationContracts.cs`

重点类型：

- `ILanguageService`
- `ILocalizedTextService`
- `ILocalizationBootstrap`
- `LocalizationTextRef`

### 2. `Martian.Localization.Runtime`

职责：

- 提供运行时服务入口。
- 处理文本解析入口。
- 处理 TMP 文本刷新和字体切换。

核心文件：

- `Assets/_Assets/Martian/Localization/Runtime/LocalizationServices.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizationFontUtility.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizationFontProfile.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizedTMPLabel.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizedFontApplier.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizedTMPInputPlaceholder.cs`

### 3. `Martian.Localization.Unity`

职责：

- 把 Unity Localization 官方包封装成通用接口实现。
- 只有这层直接依赖 `UnityEngine.Localization`。

核心文件：

- `Assets/_Assets/Martian/Localization/Unity/UnityLocalizationBridge.cs`

说明：

- 这层是官方包桥接层。
- 没装 `com.unity.localization` 时，这层不会编译。
- 但项目启动也会因此在初始化阶段直接暴露问题，而不是自动降级。

### 4. `BaoZuPo.Localization`

职责：

- 这是项目层入口。
- 管项目自己的 key、默认文本和调用方式。

核心文件：

- `Assets/_Assets/Scripts/Localization/BaoZuPoLocalizationBootstrap.cs`
- `Assets/_Assets/Scripts/Localization/GameText.cs`
- `Assets/_Assets/Scripts/Localization/CardTextResolver.cs`

## 想改东西时先看哪里

### 改 UI 固定文案

先看：

- `Assets/_Assets/Scripts/Localization/GameText.cs`
- 对应 UI 脚本

常见调用点：

- `Assets/_Assets/Scripts/UI/UITopBar.cs`
- `Assets/_Assets/Scripts/UI/UIBoardPanel.cs`
- `Assets/_Assets/Scripts/UI/UIPhasePanel.cs`
- `Assets/_Assets/Scripts/UI/UIGameOverPanel.cs`

### 改卡牌名和描述

先看：

- `Assets/_Assets/Scripts/Card/CardData.cs`
- `Assets/_Assets/Scripts/Localization/CardTextResolver.cs`
- `Assets/_Assets/Scripts/Editor/CardDataImporter.cs`

当前卡牌文本结构：

- `defaultName`
- `defaultDescription`
- `nameTextKey`
- `descriptionTextKey`

### 改语言切换逻辑

先看：

- `Assets/_Assets/Scripts/Localization/BaoZuPoLocalizationBootstrap.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizationServices.cs`
- `Assets/_Assets/Martian/Localization/Unity/UnityLocalizationBridge.cs`

### 改字体切换

先看：

- `Assets/_Assets/Martian/Localization/Runtime/LocalizationFontUtility.cs`
- `Assets/_Assets/Martian/Localization/Runtime/LocalizationFontProfile.cs`

### 改字体生成或字库扫描

先看：

- `Assets/_Assets/Scripts/Editor/Localization/LocalizationFontTools.cs`
- `Assets/_Assets/Scripts/Editor/ChineseFontTools.cs`

## 文本入口

### UI 文案

统一走 `GameText`。

适合存放：

- 顶栏文字
- 阶段文字
- 棋盘标题
- 结算标题
- GameOver
- 空槽提示

### 卡牌文案

统一走 `CardTextResolver`。

不要再直接在 UI 或玩法代码里读取原始显示字段。

## 字体配置方式

现在已经有一个明确的字体配置入口：

- `LocalizationFontProfile`

类型文件：

- `Assets/_Assets/Martian/Localization/Runtime/LocalizationFontProfile.cs`

运行时固定读取路径：

- `Resources/Localization/LocalizationFontProfile.asset`

也就是你需要在项目里创建这个资源，并为每个语言显式配置映射。

编辑器快捷入口：

- `Tools/BaoZuPo/Localization/Create Or Select Font Profile`

当前建议配置：

- `zh-Hans` -> 中文 TMP 字体资产
- `en` -> 英文 TMP 字体资产

每个语言映射里还可以配置：

- `fallbackFontAssets`

这适合处理：

- 特殊符号
- 图标字体
- 少量补充字符集

## 当前字体行为

当前字体系统已经改成：

- 不自动创建中文 TMP 字体。
- 不回退到 TMP 默认字体。
- 不根据“中文/非中文”自动猜字体。

而是：

- 当前语言是什么，就去 `LocalizationFontProfile` 找对应映射。
- 找不到就直接报错。

这意味着字体配置现在是显式的、可控的，也更符合“让问题早暴露”的要求。

## 编辑器侧入口

### 卡牌导表

文件：

- `Assets/_Assets/Scripts/Editor/CardDataImporter.cs`

职责：

- 从 Excel 导入卡牌数据。
- 填充 `defaultName/defaultDescription`
- 填充 `nameTextKey/descriptionTextKey`

### Excel 表头整理

文件：

- `Assets/_Assets/Scripts/Editor/ModifyExcelScript.cs`

### 字体字库扫描

文件：

- `Assets/_Assets/Scripts/Editor/Localization/LocalizationFontTools.cs`

职责：

- 扫描 UI fallback、卡牌文本、Excel、Prefab、Scene 中的字符。
- 更新中文 TMP 字体资产。

## 当前已经完成的迁移

已经迁过去的主要区域：

- 顶栏
- 阶段面板
- 棋盘标题
- 房间摘要和空槽提示
- 卡牌名、描述、类型标签
- 结算标题
- GameOver
- 反馈文本

已经删除的旧入口：

- `AppLanguage`
- `LocalizationManager`
- `UIStrings`
- `UIFontCatalog`

## 当前还没完成的部分

这套代码已经可用，但 Unity 资源配置还没补齐。

当前还缺：

- `Localization Settings`
- `Locale`
- `UI` / `Cards` String Table Collection
- 正式的 `LocalizationFontProfile.asset`
- Unity 内完整编译和场景验收

## 对字库方案的建议

基于你现在的原则，我建议字库方案也遵循“编辑器预生成，运行时不兜底”。

推荐方案：

1. 每个语言都有自己明确的 TMP 字体资产  
不要指望运行时临时创建。

2. 中文用编辑器生成的正式字体资产  
建议继续使用 `SourceHanSansSC-Regular.otf` 作为源字体，在编辑器里生成和更新中文 TMP 字体资产。

3. 英文单独配一套拉丁字体  
不要默认混用 TMP 默认字体。  
如果你想风格统一，可以先用同一字体家族；如果你想英文观感更好，可以单独配一套英文字体。

4. 开发期可以用“编辑器更新字库”，不要用“运行时自动补字”  
也就是缺字时通过 `LocalizationFontTools` 补齐，而不是靠运行时生成。

5. 如果你想更早暴露缺字问题，最终可以收敛到静态字库  
开发中可以先用可更新的 TMP 字体资产提高效率，临近稳定阶段再固定字库范围。

对当前项目我更推荐的落地组合是：

- `zh-Hans`：`Source Han Sans SC` 生成的中文 TMP 字体资产
- `en`：单独一个英文 TMP 字体资产
- `LocalizationFontProfile`：统一管理语言到字体的映射
- `LocalizationFontTools`：只做编辑器阶段的字符收集和字体更新

## 一句话总结

这套本地化现在的核心思路是：

- 文本入口统一
- 字体配置显式
- 官方包桥接独立
- 缺配置直接报错

如果你只是要继续改这套系统，通常优先看：

- `GameText`
- `CardTextResolver`
- `BaoZuPoLocalizationBootstrap`
- `LocalizationFontUtility`
- `LocalizationFontProfile`
- `CardDataImporter`
- `LocalizationFontTools`
