# 轻量化通用 CI/CD 方案 v1

> 状态：V1 已落地到当前仓库，尚未完成 GitHub Secrets 配置后的线上首轮验收
> 目标：先把 Unity 项目的轻量 CI/CD 做成可复用结构，再在第二个 Unity 项目出现时抽离共享层

## 1. 方案摘要

这次落地的目标不是做一个“全自动发布平台”，而是先把当前仓库需要的最小闭环搭起来：

- PR / `main` 自动跑 `EditMode` 测试
- 手动触发 `Windows x64` 构建
- 自动上传测试结果和构建产物
- 统一 Unity 构建入口、命令参数和失败语义
- 把项目语义留在项目集成层，避免一开始就把通用层做重

## 2. 已确认决策

### 2.1 三层边界

1. 通用契约层
   - GitHub Actions 使用统一的 Unity Secret 名称
   - Unity 构建入口固定支持 `-buildOutput`、`-buildName`、`-buildVersion`
   - 构建失败和场景缺失直接失败，不做隐式 fallback

2. 通用预设层
   - 一个 `EditMode CI` 工作流
   - 一个 `Build Artifact CD` 工作流

3. 项目集成层
   - 当前默认平台固定 `StandaloneWindows64`
   - 当前构建场景真源固定 `EditorBuildSettings`
   - 当前构建命名默认来自 `ProjectSettings.asset`

### 2.2 V1 不做

- 不做 GitHub Release
- 不做渠道分发
- 不做商店上传
- 不单独创建 infra 仓库
- 不为了“以后可能跨技术栈”而提前抽象

## 3. 本轮已落地内容

### 工作流

- `/.github/workflows/unity-ci.yml`
  - 覆盖 `pull_request -> main` 和 `push -> main`
  - 跑 `game-ci/unity-test-runner@v4`
  - 仅执行 `EditMode`
  - 上传测试 artifact

- `/.github/workflows/unity-build-artifact.yml`
  - 使用 `workflow_dispatch`
  - 支持 `build_name`、`build_version` 两个可选输入
  - 跑 `game-ci/unity-builder@v4`
  - 上传压缩构建 artifact

### Unity 构建入口

- `Assets/_Assets/Scripts/Editor/UnityCiBuildEntry.cs`
  - 当前唯一 CI/CD 构建入口
  - 只认 `EditorBuildSettings` 的启用场景
  - 输出目录统一收敛到 workflow 传入的 `buildOutput`
  - 明确失败，不做兜底

### 文档入口

- `Doc/10_模块梳理/12_CI-CD模块.md`
  - 当前实现说明与维护入口
- 本文档
  - 作为完整方案、交接说明和后续接手入口

## 4. 当前真源与默认值

### GitHub 真源

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

### Unity 真源

- Unity 版本：`ProjectSettings/ProjectVersion.txt`
- 构建场景：`ProjectSettings/EditorBuildSettings.asset`
- 默认 `build_name`：`ProjectSettings.asset` 的 `productName`
- 默认 `build_version`：`ProjectSettings.asset` 的 `bundleVersion`

### 当前默认输出

- 平台：`StandaloneWindows64`
- 构建目录：`build/StandaloneWindows64/<buildName>_<buildVersion>/`
- artifact 名称：`windows-player-<buildName>-<buildVersion>`

## 5. 首轮验收清单

### CI 验收

1. 配好 Unity Secrets
2. 建一个到 `main` 的 PR
3. 确认 `unity-ci` 自动运行
4. 确认测试结果 artifact 可下载

### CD 验收

1. 在 Actions 手动触发 `unity-build-artifact`
2. 可选填 `build_name` 与 `build_version`
3. 确认上传的 artifact 可下载
4. 解压后确认包含 `exe` 和配套构建目录

### 失败路径验收

1. 去掉 Unity Secrets 后，工作流应在激活/启动阶段失败
2. 清空 `EditorBuildSettings` 启用场景后，构建入口应明确失败
3. 引入一个故意失败的 EditMode 测试后，CI 应阻断

## 6. 如果中断，下一次从哪里接

如果后续工作被暂停，下一位接手时按这个顺序继续：

1. 先看 `Doc/10_模块梳理/12_CI-CD模块.md`
   - 快速恢复“当前真实实现”上下文
2. 再看本文档第 5 节
   - 先完成 GitHub 侧首轮验收
3. 如果验收通过，再决定是否进入下一阶段：
   - 给 `main` 加分支保护，把 `unity-ci` 设为 required check
   - 给构建 workflow 增加缓存
   - 为第二个 Unity 仓库提取共享工作流

## 7. 后续演进建议

### 短期

- 把 `unity-ci` 配成 `main` 的 required check
- 根据实际耗时决定是否加 `Library` 缓存
- 补一轮 `PlayMode` 测试可行性评估

### 中期

- 当第二个 Unity 仓库也需要接入时，提取：
  - 共享 `workflow_call`
  - 或共享 composite action
- 保留项目集成层只负责：
  - 平台
  - 命名
  - 触发策略
  - 项目专属 secrets / registry 接入

### 长期

- 如果发布链路稳定，再加：
  - GitHub Release
  - 渠道分发
  - 多平台矩阵

## 8. 明确未决项

以下内容本轮故意不做决定，避免把 V1 做重：

- 是否引入 `workflow_call`
- 是否开启 `Library` 缓存
- 是否扩到 `PlayMode`
- 是否引入 Release 自动发布
- 是否接私有 UPM registry / SSH 私仓

这些都应在“当前 V1 已跑通”的前提下，再单独开下一轮方案。
