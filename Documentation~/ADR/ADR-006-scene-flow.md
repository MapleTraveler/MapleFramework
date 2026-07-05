# ADR-006：SceneFlow —— 独立场景加载抽象 / Core 用 Task / UI 与游戏规则无关 / 编排在游戏层

## 背景（Context）
Phase 3。框架已有泛型状态机 `GameStateManager<T>` 但游戏层零使用，且缺少统一的异步场景切换能力。目标是补一个 `ISceneFlowService` 驱动场景切换，并为未来接入 FSM、切换资源后端（Phase 5 Addressables）打基础。

## 问题（Problem）
1. 场景加载走哪条路？进度文档草案写"面向 `IResourceLoader` 加载场景"——但 `IResourceLoader` 只加载 `UnityEngine.Object` 资产，加载不了场景（场景靠 `SceneManager` / `Addressables` 的专用 API）。
2. 契约的异步返回类型用什么？草案用 `UniTask` 且放在 `Maple.Core`，会给零依赖的 Core 强加 UniTask 引用。
3. 要不要提供"同步加载场景 / 同步加载资源"接口？
4. Loading 界面归谁？SceneFlow 在 `Maple.Framework`，而 UI 在 `Maple.UI`，Framework 不引用 UI。
5. 场景 key 映射、状态机联动等游戏规则放哪？

## 可选方案（Options）
- **场景加载抽象**：A. SceneFlow 直接调 `SceneManager`（Phase 5 换 Addressables 时要改，破坏"零改动"）；**B. 新增独立 `ISceneLoader` 抽象，默认 SceneManager 实现，Phase 5 补 Addressables 实现 —— 采用**；C. 硬扩 `IResourceLoader` 加场景方法（污染资产接口，语义混乱）。
- **异步类型**：**A. Core 契约用标准 `System.Threading.Tasks.Task`（与既有 `IRequestResponseClient` 一致）—— 采用**；B. callback/event 无 Task；C. 接口放 Framework 用 UniTask（破坏"契约集中在 Core"）。
- **Loading 界面**：SceneFlow 直接操作 UI（分层不允许）；**事件驱动 + 游戏层编排 Show/Hide —— 采用**。

## 最终方案（Decision）
1. **独立场景加载抽象 `ISceneLoader`（Core）**：`Task LoadSceneAsync(string sceneKey, IProgress<float> progress = null)`。默认实现 `SceneManagerSceneLoader`（`Maple.Extensions`，基于 `SceneManager.LoadSceneAsync`，场景取自 Build Settings），与 `ResourcesLoader` / 未来 `AddressablesLoader` 并列。资产走 `IResourceLoader`，场景走 `ISceneLoader`，职责分离。
2. **契约用标准 `Task`**：`ISceneLoader` / `ISceneFlowService` 均放 `Maple.Core` 且只依赖 BCL 的 `Task` / `IProgress`，保持 Core 零第三方依赖（ADR-002）。实现层 `Maple.Framework` / `Maple.Extensions` 内部可用 UniTask，但不外溢到契约。
3. **只提供异步、不提供同步场景加载**：`Addressables` 无同步场景 API，若在 `ISceneLoader` 放同步方法，Addressables 实现无法履约（只能抛异常或 `WaitForCompletion` 强等，后者在场景上极易卡死/掉帧），破坏里氏替换，1B 抽象失去意义。**资产的同步加载则保留**（`IResourceLoader.Load<T>` 已有且实战验证；Addressables 侧可用 `AsyncOperationHandle.WaitForCompletion()` 兜底，对资产可行）。一句话原则：**抽象须诚实反映后端最弱能力——资产可同步，场景只异步**。
4. **`SceneFlowService` 为纯 C# 类，构造注入 `ISceneLoader`**（ADR-001 纪律，便于单测，不需要 Tick 故不做 MonoBehaviour）。由 `GameRoot` 在组合根构造并注册，开关 `registerSceneFlow`（默认 true）。`ISceneLoader` 与 `IResourceLoader` 同源于 `EResourceLoaderType`（`None` 不注册）。
5. **SceneFlow 与 UI / 游戏规则解耦**：`SceneFlowService` 不引用 UI、不认识游戏状态枚举，只对外抛 `OnLoadStarted / OnLoadProgress / OnLoadCompleted` 事件并提供 `IsLoading`。Loading 界面的显隐、场景 key 选择、（未来的）FSM 联动全部由游戏层完成——Demo 中 `SceneFlowDemo` 按键触发、`Show/Hide<LoadingWindow>()`，`LoadingWindow` 为 `Global` 生命周期避免切场景时被清理。

## 为什么（Rationale）
- **独立抽象而非塞进 IResourceLoader**：场景与资产是两套引擎 API，硬合并会让接口语义分裂。拆开后 Phase 5 换 Addressables 时，资产后端与场景后端可各自替换，上层零改动的承诺对两者都成立。
- **Core 用 Task**：延续框架既有决策（`IRequestResponseClient` 就用 Task），守住 ADR-002 的"只取 Core/UI 不被迫引入 UniTask"。
- **异步-only 场景加载**：见 Decision 3，这是被 Addressables 能力边界倒逼的诚实设计。
- **事件驱动 + 游戏层编排**：既遵守"Framework 不引用 UI"的分层，又满足"Loading 归 UI 框架"（它就是个 Window），同时保证 `SceneFlowService` 可跨项目复用。

## Pros
- 场景 / 资产后端可分别替换，Phase 5 零改动目标对两者都成立。
- Core 保持零第三方依赖。
- SceneFlow UI 无关、游戏规则无关，可复用、可单测。
- 防重入（`IsLoading`）、回调 `try/catch` 兜底，加载失败不卡死流程。

## Cons
- 多一层 `ISceneLoader` 抽象与一个枚举分支，结构略复杂。
- 不支持同步切场景（本期无此需求）。
- 本期仅单场景模式（`LoadSceneMode.Single`），无 Additive 叠加加载。
- `SceneManagerSceneLoader` 用 `while(!isDone) await Task.Yield()` 轮询，依赖 Unity 主线程 `SynchronizationContext`（Play 模式下成立）。

## 未来什么时候可能修改（Revisit）—— 已知扩展点
- **Additive 叠加加载 / 卸载**：需要常驻场景 + 动态子场景时，给 `ISceneLoader` 增加带 `LoadSceneMode` 的重载或独立 `UnloadSceneAsync`，而非改单场景语义。
- **激活时机控制（allowSceneActivation）**：需要"加载完成后停在 90%、等 Loading 动画/最短展示时长走完再激活"时，扩展 `ISceneLoader` 暴露激活闸门；届时进度语义需重新约定。
- **FSM 实战化落地**：定义游戏层 `enum EAppState`，用 `GameStateManager<EAppState>` 驱动，订阅 `GameStateChangedEvent<EAppState>` 后调 `LoadAsync`，把当前 Demo 的按键触发替换为状态驱动。框架侧零改动。
- **Addressables 后端（Phase 5）**：新增 `AddressablesSceneLoader : ISceneLoader`，`EResourceLoaderType` 加 `Addressables` 项，`GameRoot` switch 补分支。验收：上层与 Demo 一行不动仍能切场景。
- **async/await 友好化**：若游戏层需要 `await sceneFlow.LoadAsync(...)` 的取消/超时，可增加 `CancellationToken` 重载。

## 影响模块（Impact）
`Maple.Core`（`ISceneLoader` / `ISceneFlowService`、`LogTag.SCENE`）、`Maple.Extensions`（`SceneManagerSceneLoader`）、`Maple.Framework`（`SceneFlowService`、`GameRoot` 注册 + `registerSceneFlow` 开关）、游戏层 Demo（`LoadingWindow` / `SceneFlowDemo`）。

## 日期
2026-07-04（决策与记录）
