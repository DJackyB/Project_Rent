Martian.Localization 项目初始化说明

内置中文字体

- 字体名称：Source Han Sans SC
- 来源：Adobe 官方 Source Han Sans 发布包
- 许可证：SIL Open Font License 1.1
- 字体文件目录：`Assets/_Assets/Martian/Localization/Resources/Fonts/SourceHanSansSC/`

一键初始化工具

- 菜单入口：`Tools/Martian/Localization/Setup Project Localization`
- 工具会自动完成这些事情：
- 生成插件内置中文字体对应的 `TMP_FontAsset`
- 更新 `Resources/Localization/LocalizationFontProfile`
- 将 `zh-Hans` 绑定到默认中文字体，并把粗体字作为 fallback
- 创建项目级 `LocalizationSettings`
- 创建 `zh-Hans` 与 `en` 两个 Locale
- 创建基础字符串表：`UI`、`Common`、`Card`
- 扫描项目中的 `TMP_Text`，替换为基础中文字体

自动生成的项目资产

- Localization Settings：`Assets/_Assets/Martian/Localization/Project/Settings/Localization Settings.asset`
- Locales：`Assets/_Assets/Martian/Localization/Project/Locales/`
- String Tables：`Assets/_Assets/Martian/Localization/Project/Tables/`
- 字体配置真源：`Assets/_Assets/Martian/Localization/Resources/Localization/LocalizationFontProfile.asset`

日常使用流程

1. 新项目接入 `Martian.Localization` 后，先点击一次 `Tools/Martian/Localization/Setup Project Localization`
2. 如果只想先让中文正常显示，不需要额外手动创建 TMP 字体或本地化骨架
3. 需要替换默认中文字体时，先生成新的 `TMP_FontAsset`
4. 然后打开 `LocalizationFontProfile.asset`，把 `zh-Hans` 的 `fontAsset` 改绑到新的字体
5. 需要新增语言时，在 `LocalizationFontProfile.asset` 中继续补字体映射，并在 Localization Tables 中补对应语言表

文字多语言配置

- 文本内容本身仍然使用 Unity Localization 的 String Table
- `LocalizedTMPLabel` 默认读取 `UI` 表
- 你可以按模块继续扩展表，例如 `Card`、`Common`
- 运行时文字读取依赖 `table + entry`，不是自动翻译

注意事项

- 不要随意移动 `LocalizationFontProfile.asset`，运行时代码固定从 `Resources/Localization/LocalizationFontProfile` 读取
- 如果项目里存在损坏的 prefab 或 scene，工具会跳过该资源并继续处理其他内容
- 如果以后希望只扫描业务目录，可以把批量替换范围从整个 `Assets` 收窄到 `Assets/_Assets`
