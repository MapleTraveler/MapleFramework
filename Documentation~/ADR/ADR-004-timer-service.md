# ADR-004：TimerService —— 统一 Tick 驱动 / 句柄管理 / timeScale 隔离 + 封顶修复

## 背景（Context）
Phase 1。框架缺少统一的定时 / 调度能力。游戏侧普遍需要：技能冷却、Buff 持续时间、UI 倒计时、暂停界面计时等。其中暂停（`Time.timeScale = 0`）时，不同计时器应有不同表现。

## 问题（Problem）
如何提供统一计时能力，并正确区分"受游戏暂停影响"与"不受影响"两类计时需求？

## 可选方案（Options）
- **A. 各处自己用协程 / `Update` 计时**：分散、重复、难统一暂停。
- **B. 中心化 TimerService，统一 Tick 驱动 + 句柄管理** —— 采用。
- **C. 引入第三方 tween / timer 库**：增加依赖，超出当前需要。

## 最终方案（Decision）
采用 **B**。要点：
- `TimerService` 为 MonoBehaviour，实现 `IFrameworkService + ITickable + ITimerService`，挂在 `GameRoot` 同 GameObject，由 `GameRoot` 自动扫描初始化；**自身不写 `Update`**，由 `GameRoot` 统一 `Update` 驱动 `Tick`。
- **`int` 句柄**标识计时器，`0` 约定为无效句柄。
- **`ignoreTimeScale`** 决定用 `deltaTime`（受暂停影响，如技能冷却）还是 `unscaledDeltaTime`（不受影响，如 UI 倒计时 / 暂停菜单）。
- **标记-后删 + 逆序移除**保证 Tick 遍历期间取消 / 移除的迭代安全。
- 循环计时用 **`Remaining += Interval`** 保留超出部分，防止低帧率累计漂移。
- 回调 **`try/catch`** 隔离，单个野回调不影响其它计时器。
- **对 `unscaledDeltaTime` 施加 `Time.maximumDeltaTime` 封顶**（`Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime)`）。

## 为什么（Rationale）
统一 Tick 避免满场景 `Update`，Profiler 更干净；句柄式取消简单可靠；`ignoreTimeScale` 精确覆盖"暂停时 UI 继续走、技能冷却停止"的真实需求。

**关于封顶修复（一个被实测暴露的真 Bug）**：`Time.deltaTime` 受 `maximumDeltaTime`（默认 0.333s）封顶，而 `Time.unscaledDeltaTime` 不受封顶。在初始化帧或卡顿帧，二者差值可达数秒，导致 `ignoreTimeScale=true` 的计时器在前几帧连续暴冲、领先于 scaled 计时器。讨论后判定：虽然正常业务（在 `Start` / 用户操作后注册计时器）几乎不触发，但这是**框架层应当自行兜底的问题，而不是要求调用方跳帧规避**，故在 `Tick` 内对齐两个时间源的最大步长。实测验证：暂停 4 秒窗口内 `[不受影响]` 连跳、`[受影响]` 完全沉默，隔离正确。

## Pros
- 统一驱动、零外部依赖、暂停语义清晰。
- 迭代安全、防漂移、回调异常不串扰。
- 两个时间源封顶对齐后，注册时机不影响正确性。

## Cons
- 句柄查找为线性 O(n)（`Cancel` / 命中），计时器数量极大时不经济。
- 循环最小间隔限制为 1ms（防止 0 间隔死触发）。
- 依赖"挂在 GameRoot 同 GO"的约定。

## 未来什么时候可能修改（Revisit）
- 同时活跃计时器数量极大、`Cancel` 成为热点 → 改字典索引句柄、或最小堆按到期时间排序。
- 需要 `async/await` 风格的等待（`await timer.Delay(...)`）→ 增加 UniTask 适配层。

## 影响模块（Impact）
`Maple.Core`（`ITimerService`）、`Maple.Framework`（`TimerService`）、`GameRoot`（Tick 驱动）。

## 日期
2026-06-26（决策与封顶修复）/ 2026-06-29（记录）
