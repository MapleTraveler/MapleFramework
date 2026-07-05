# ADR-005：SaveService —— 槽位明文 JSON / 原子写 / 版本信封 / 复用 ISerializer

## 背景（Context）
Phase 2。框架需要持久化能力：游戏进度、玩家设置等。项目已有 `ISerializer`（Newtonsoft 实现），网络 / LLM 链路在用同一套序列化。

## 问题（Problem）
存储粒度如何？用什么格式？如何防止写入中断损坏存档？如何应对未来数据结构演进？如何避免重复造一套序列化逻辑？

## 可选方案（Options）
- **A. 直接用 `PlayerPrefs`**：只适合零散小设置，不适合结构化存档，跨平台行为弱。
- **B. 自己写 `JsonConvert` 落文件**：能用，但与已有 `ISerializer` 重复，且易漏掉原子写 / 版本。
- **C. 槽位式 + 复用 `ISerializer` + 原子写 + 版本信封** —— 采用。

## 最终方案（Decision）
采用 **C**：
- **契约 `ISaveService`** 在 `Maple.Core`，本期**只做整存整取的槽位（slot）模式**（轻量 KV 延后）；`Load` 带 `fallback`，首次无档直接返回默认值。
- **实现 `JsonSaveService`** 在 `Maple.Extensions`，**构造注入 `ISerializer`**（不在内部 `ServiceHub.Get`）；只负责"字节 ↔ 磁盘"，"对象 ↔ 字节"交给 `ISerializer`。
- **明文 JSON**，文件位于 `Application.persistentDataPath/<子目录>/<slot>.json`（加密延后）。
- **原子写**：先写 `*.tmp`，再 `File.Replace`（目标存在）/ `File.Move`（首次不存在）原子重命名。
- **版本信封 `SaveEnvelope<T> { int Version; T Payload; }`**：留迁移挂载点，本期不写迁移逻辑。
- **健壮性**：`Load` 遇损坏 / 异常返回 `fallback`，不让游戏崩溃；`Save` / `Delete` 异常仅记日志。
- `GameRoot` 经 `registerSaveService` 开关注册，且仅当 `ISerializer` 已存在时才注册。

## 为什么（Rationale）
- **复用而非内联**：序列化复用 `ISerializer`，避免重复 `JsonConvert` 逻辑；未来换二进制 / 加密序列化时，`JsonSaveService` 一行不改（它只认 `byte[]`）。这是"组合优于内联"。
- **原子写是"玩具"与"能上线"的分水岭**：防止写一半断电 / 崩溃损坏既有存档。代价仅几行。
- **版本信封但不写迁移**：为未来留口子，但当前没有 v2，写迁移是浪费（YAGNI）。
- **明文**：开发期可直接打开文件查看 / 手改调试，符合当前阶段。

## Pros
- 复用序列化、可替换后端（二进制 / 加密）而上层不改。
- 抗写入中断；存档损坏不崩游戏。
- 明文可调试；契约零依赖、数据结构由游戏层定义。

## Cons
- 明文可被玩家篡改。
- 仅整存整取；频繁的小 KV 写不经济。
- 同步 IO，大存档可能卡主线程。
- 无索引，槽位线性管理。

## 未来什么时候可能修改（Revisit）
- 需要防篡改 → 注入加密版 `ISerializer`（AES 装饰器），架构不变。
- 需要频繁小设置读写 → 补独立的轻量 KV 接口。
- 出现大体积存档 → 增加 `SaveAsync` 重载（UniTask）。
- 数据结构升级 → 在信封 `Version` 上实现迁移分支。

## 影响模块（Impact）
`Maple.Core`（`ISaveService`）、`Maple.Extensions`（`JsonSaveService`）、`GameRoot`（注册 + 开关）、`LogTag`（新增 `SAVE`）。

## 日期
2026-06-29（决策与记录）
