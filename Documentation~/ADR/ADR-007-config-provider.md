# ADR-007：Config —— 泛型键防装箱 / 索引层进 Core / 加载交游戏层 / SO 便利件可选

## 背景（Context）
Phase 4。真实游戏（BattleAgent）已出现"散落 ScriptableObject"：`GameBootstrap` 里 `lightTankStats/mediumTankStats/heavyTankStats` 三个字段靠 Inspector 逐个拖，加单位类型要同时改字段、null 检查与装配逻辑；`UnitViewConfig` 又各自手写 `foreach` 查表。需要一套通用的"配置加载 + 按键索引"机制，让"数据数量变化"不再引起"代码结构变化"。

## 问题（Problem）
1. 键类型用什么？草案锁死 `int`，但真实表用 `UnitType` 枚举，未来项目常用 string。
2. "加载"与"索引"是否分开？实现放哪层？
3. 框架要不要绑定表结构 / 提供 SO 表基类？

## 可选方案（Options）
- **键类型**：A. 锁死 `int`（草案，枚举/字符串键别扭且强转）；B. 泛型 `TKey`（int/string/enum 通用）—— 采用；C. 锁死 `string`（数值键要 ToString）。
- **索引 vs 加载**：A. Provider 既懂加载 SO 又懂索引（绑定数据模型，违反总纲）；B. 索引与加载分离，Provider 只做内存索引，加载交游戏层 —— 采用。
- **放置层**：草案把实现放 `Maple.Extensions`；本 ADR 改为把纯索引实现放 `Maple.Core`（它是零依赖基建，与 EventBus/PoolProvider 同级）。

## 最终方案（Decision）
1. **契约 `IConfigProvider`（`Maple.Core`）**：`Register/Get/TryGet/GetTable/Has/Clear`，全部带**泛型 `TKey`**。一种 `TConfig` 类型对应一张表，内部以 `typeof(TConfig)` 为 key。
2. **泛型键防装箱**：选泛型 `TKey` 而非 `object key` 的**硬理由是 GC**——`object key` 会让枚举/int 键每次查询装箱，配置查询可能较频繁，装箱=堆分配=GC 压力。泛型键在编译期特化，零装箱。
3. **实现 `ConfigProvider`（`Maple.Core/Runtime`）**：纯 C# 零依赖内存注册表，与 `EventBus`/`PoolProvider` 同属核心基建，故在 **`ServiceHub.Initialize()` 内默认注册**（随处可用、无需开关、不动 GameRoot；代价仅一个空字典）。
4. **加载交游戏层，框架不绑定数据模型**：如何把 SO/JSON/CSV 变成字典由游戏层完成后 `Register` 进来。契约只认 `IReadOnlyDictionary<TKey,TConfig>`。
5. **可选便利件 `ScriptableObjectConfigTable<TKey,TEntry>`（`Maple.Extensions`）**：把"List → Dictionary"的样板抽出（含重复键告警+保留首个）。游戏层定义具体子类（Unity 无法序列化开放泛型 SO）并实现 `GetKey`。**opt-in**，不用它手动建字典也可以。
6. **健壮性对齐现有风格**：`Get` 未命中→告警+返回 `default`（不崩游戏，同 `ResourcesLoader`/`SaveService`）；要严格处理用 `TryGet`。存放的应是**不可变配置模板**，运行时可变状态（如 `UnitData`）不得放入。

## 为什么（Rationale）
- **泛型键**：面向"跨项目复用"的框架，`int`-only 是真实天花板（string id 极常见）；且泛型避免装箱 GC，符合项目对性能的敏感。
- **索引/加载分离 + 实现进 Core**：守住总纲"框架不绑定数据模型"；纯索引就是基建，放 Core 与 EventBus 同级最一致，也让"只用 Core"的项目也能用配置索引。
- **SO 便利件可选**：满足"加载"的常见诉求，又不强加表结构。

## Pros
- 数据数量变化不再改代码结构（开闭）；配置获取有中立入口，不再绑死在 Bootstrap。
- 键类型自由且零装箱；查找/告警逻辑统一实现一次，各表复用。
- 契约零依赖，可脱离 Unity 单测。

## Cons
- `Get<TKey,TConfig>(key)` 双泛型参数略啰嗦（为类型安全+防装箱付的代价）。
- 一种 `TConfig` 类型仅一张表（同类型多表的场景需自行区分包装类型）。
- 同步、无热重载、无复杂查询（非目标；复杂查询属数据库层）。

## 未来什么时候可能修改（Revisit）
- 需要一种类型多张表 → 引入命名/分组维度或包装类型。
- 需要热重载 / 编辑期 CSV·JSON → SO 烘焙工具 → 增 Editor 工具（原草案的可选后置项）。
- 真实游戏 `UnitStats` / `UnitViewConfig` 改造 → 留待下个项目实战（本期以独立 demo 验证，不动已跑通的游戏）。

## 影响模块（Impact）
`Maple.Core`（`IConfigProvider` / `ConfigProvider` / `LogTag.CONFIG` / `ServiceHub.Initialize` 加一行注册）、`Maple.Extensions`（`ScriptableObjectConfigTable`）、游戏层 Demo（`ConfigDemo`）。

## 日期
2026-07-05（决策与记录）
