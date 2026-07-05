# ADR-002：分层结构与可裁剪复用 —— Maple.* 命名空间 / asmdef 分层 / GameRoot 可选服务开关

## 背景（Context）
框架希望被多个项目复用，未来可能开源。不同项目对第三方依赖的需求不同：有的只要事件总线 + UI，不想被迫引入 Newtonsoft.Json 或 UniTask；有的需要完整网络栈。原状是单一扁平命名空间 `MapleFramework`。

## 问题（Problem）
如何组织代码，使得：依赖方向清晰、层间不乱引用、第三方重依赖不强加给所有使用者、且能按需裁剪取用？

## 可选方案（Options）
- **A. 单 assembly + 扁平命名空间 `MapleFramework`**（原状）。
- **B. 按层拆 asmdef + 分层命名空间** —— 采用。
- **C. 直接拆成多个独立 UPM 包**：最彻底的隔离，但当前过重（见 ADR-003）。

## 最终方案（Decision）
采用 **B**：
- 命名空间收敛为 **`Maple.Core` / `Maple.UI` / `Maple.Framework` / `Maple.Extensions`**（+ `Maple.Core.Editor`）。
- 5 个 **asmdef** 对应分层；**契约（接口）集中在 `Maple.Core`**，实现分布在上层；**同层 assembly 不互相引用**（如 `Maple.UI` 不引用 `Maple.Framework`）。
- 配合 `GameRoot` 的**可选服务开关**：`registerDefaultSerializer` / `registerHttpClient` / `registerSaveService`，默认开启（向后兼容），关闭后只用 `Maple.Core + Maple.UI` 的项目不被迫引入 Newtonsoft / UniTask。

## 为什么（Rationale）
asmdef 已能提供编译期隔离与依赖方向约束，成本远低于拆独立 UPM 包；分层命名空间更专业、可读，也为未来开源 / 包化打基础。可选开关把"是否启用重依赖服务"的决定权交还给使用方，实现真正的"可裁剪"。

## Pros
- 依赖方向清晰，编译期阻止层间乱引用。
- 增量编译更快（改一层不必全量重编）。
- 可按需裁剪：只取 Core / UI 时无需 Newtonsoft / UniTask。
- 为 ADR-003 的 UPM 化打下天然的层 → 包映射基础。

## Cons
- 目录与 asmdef 结构更复杂，asmdef 的 references 需手工维护。
- 跨层使用类型时要显式补 `using`（重构期一次性成本，已支付）。

## 未来什么时候可能修改（Revisit）
当决定正式开源 / UPM 化时（见 ADR-003），把各层映射为独立包并定义包间依赖。若层划分被证明过细或过粗，届时一并调整。

## 影响模块（Impact）
全框架（5 个 asmdef、60+ `.cs` 的命名空间）、`GameRoot`（可选开关 + 注册逻辑）、游戏层与 Demo 的 `using`。

## 日期
2026-06-29（记录；命名空间收敛与网络栈解耦于 Phase 0 完成）
