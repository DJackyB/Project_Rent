---
name: verify
description: 在 Unity Editor 中运行 EditMode 测试，验证当前改动没有破坏核心流程。改动 GameFlow、Card、Economy 相关代码后使用。
---

## 触发 Unity EditMode 测试

Unity 测试框架：`com.unity.test-framework 1.6.0`，测试位于 `Assets/_Assets/Scripts/Tests/` 或 `Assets/Tests/`（EditMode）。

**步骤：**
1. 打开 Unity Editor
2. 菜单：Window → General → Test Runner
3. 选择 EditMode 选项卡
4. 点击 "Run All" 运行所有测试，或选中特定测试类点击 "Run Selected"

**当前已知测试覆盖范围：**
- 奖励链路（reward selection）

**如果测试失败：**
- 查看 Console 窗口中的报错堆栈
- 不要绕过测试，找到根本原因后修复

**注意：** 目前没有 CI/CD，所有测试需在 Editor 中手工触发。
