# CI/CD 模块

## 当前状态
- 状态：V1 已落地，待 GitHub Secrets 配置后做线上首轮验收
- 目标：在当前仓库内形成“通用契约 + 通用预设 + 项目集成”的轻量 CI/CD 结构
- 边界：当前只覆盖 `EditMode CI` 和 `Windows x64 artifact CD`，不包含 GitHub Release、渠道分发和商店上传

## 模块职责

- 用 GitHub Actions 统一当前仓库的 Unity 自动化入口
- 用一个静态 `Editor` 构建入口统一构建命令参数、场景真源和失败语义
- 把 Unity License Secrets、Unity 版本、构建场景和默认目标平台的真源集中到固定位置
- 为未来第二个 Unity 仓库复用时保留可抽离边界，但当前不提前拆独立 infra 仓库

## 目录结构

- `/.github/workflows/unity-ci.yml`
  - PR / main 自动测试工作流
- `/.github/workflows/unity-build-artifact.yml`
  - 手动触发的 Windows 构建工作流
- `Assets/Scripts/Editor/UnityCiBuildEntry.cs`
  - 当前项目的 Unity 构建入口
- `Doc/20_专题方案/轻量化通用CI-CD方案_v1.md`
  - 完整方案与交接文档

## 配置真源

### GitHub Actions Secrets

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`
- 如果后续改为 Unity Pro，再额外接入 `UNITY_SERIAL`

### Unity 版本

- GitHub Actions 侧通过 `unityVersion: auto` 读取 `ProjectSettings/ProjectVersion.txt`
- 当前版本：`6000.3.11f1`

### 构建场景

- 真源：`ProjectSettings/EditorBuildSettings.asset`
- 当前启用场景：`Assets/Scenes/SampleScene.unity`
- 若启用场景为空，`UnityCiBuildEntry` 直接失败，不做隐式兜底

### 默认构建目标

- 当前固定为 `StandaloneWindows64`
- 这是项目集成层决策，不属于通用契约层

## 当前实现

### `unity-ci`

- 触发：
  - `pull_request` 到 `main`
  - `push` 到 `main`
- 执行：
  - `game-ci/unity-test-runner@v4`
  - `testMode: EditMode`
- 产出：
  - 上传测试结果 artifact
  - 测试失败时直接阻断，不继续伪成功

### `unity-build-artifact`

- 触发：
  - `workflow_dispatch`
- 可选输入：
  - `build_name`
  - `build_version`
- 默认值来源：
  - `build_name` 默认读取 `ProjectSettings.asset` 的 `productName`
  - `build_version` 默认读取 `ProjectSettings.asset` 的 `bundleVersion`
- 执行：
  - `game-ci/unity-builder@v4`
  - `targetPlatform: StandaloneWindows64`
  - `buildMethod: BaoZuPo.Editor.UnityCiBuildEntry.BuildStandaloneWindows64`
- 产出：
  - 上传压缩后的 Windows 构建 artifact

### `UnityCiBuildEntry`

- 公共命令参数：
  - `-buildOutput`
  - `-buildName`
  - `-buildVersion`
- 固定行为：
  - 只读取 `EditorBuildSettings` 中已启用的场景
  - 构建目标固定 `StandaloneWindows64`
  - 构建输出目录为 `buildOutput/<buildName>_<buildVersion>/`
  - 使用 `BuildOptions.StrictMode`
  - 构建失败直接抛出 `BuildFailedException`

## 手工配置与验收

### 首次接入

1. 在 GitHub 仓库中配置 `UNITY_LICENSE`、`UNITY_EMAIL`、`UNITY_PASSWORD`
2. 推一个分支并发起到 `main` 的 PR，确认 `unity-ci` 会自动运行
3. 在 Actions 页手动触发 `unity-build-artifact`
4. 下载 artifact，确认目录内包含 `exe` 与同级构建输出

### 最短验收路径

1. PR 到 `main` 时出现 `EditMode Tests`
2. 测试结果 artifact 可下载
3. 手动触发构建后出现 `windows-player-*` artifact
4. 如果清空 `EditorBuildSettings` 场景，构建入口应明确失败

## 修改准则

- 不要把分支策略、版本命名策略、发布渠道规则写进通用构建入口
- 不要把项目专属发布语义写进 GitHub Actions 通用契约层
- 如果未来第二个 Unity 仓库也要接入，再抽 `workflow_call` 或 composite action
- 如果只是在当前仓库调整平台、缓存或 artifact 命名，优先改项目集成层，不要反向污染通用边界

