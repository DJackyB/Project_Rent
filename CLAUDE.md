# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概况

Unity 6000.3.11f1 卡牌游戏（包租婆），目标平台 PC Windows。3 个月短周期，优先稳主循环（准备→行动→结算→奖励→下一回合）而非提前框架。

## C# 编码规则

禁止：
- `Find()`、`FindObjectOfType()`、`SendMessage()`
- `public` 字段（一律改为 `[SerializeField] private`）
- `Update()` 中频繁 `GetComponent<>()`
- 深层 MonoBehaviour 继承

必须：
- 在 `Awake()` 中缓存组件引用
- Unity 对象判空用 `== null`，不用 `is null`
- 代码中不创建 UI 节点——UI 在 Editor 里拼 Prefab，代码只持有 `[SerializeField]` 引用

## 配置与 .asset 文件

- **非必要不改 `.asset`**。配置类资产由用户在 Inspector 手动调整；确需改动时必须说明原因并控制范围。
- 配置/数据错误直接报错中断，不做静默 fallback。

## 数据管线（卡牌导表）

Excel (`Assets/_Assets/Data/Excel/CardData.xlsx`) → 在 Unity Editor 手动运行 `CardDataImporter` → 生成 `Resources/Cards/*.asset`。无自动化构建，每次改表后需手工触发导表。

## 文案

- 玩家可见文案走 `GameText` 统一入口，不硬编码字符串。
- 玩家可见文案中文，代码标识符（类名、字段名、路径）保持英文。

## 模块状态

同一能力只允许一个真源。每个模块必须明确标记：**已导入未接线** 或 **已接线启用**。当前未接线模块：`Martian.Localization`、`com.unity.localization`（运行时文案真源仍为 `GameText`）。

## 文档维护

改动模块后同步更新 `Doc/10_模块梳理/` 对应文档。
项目原则见 `@Doc/00_必读/项目开发必读.md`。

## Git 交付

提交、PR、交付说明使用 `/chinese-git-delivery` skill。
