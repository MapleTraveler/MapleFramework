# MapleFramework 开发进度

> 记录框架完善路线的阶段规划与完成状态。  
> `[x]` 已完成，`[ ]` 待完成，`[~]` 暂缓/搁置。

---

## 文档地图（新会话先读这里）

无上下文记忆的 AI / 新成员，按此顺序读即可无缝衔接：

1. `MapleFramework总纲.md` —— 架构是什么（5 层结构、各层职责）
2. `MapleFramework进度.md`（本文件）—— 做到哪、接下来做什么（状态 + 路线图 + 设计纪律）
3. `ADR/README.md` 及其索引 —— 关键决策"为什么这么定"（取舍、边界、何时该推翻）

> "做什么 / 做到哪"看进度文档；"为什么这样做"看 ADR。两者职责不同，不要混写。

---

## 现有基建状态（框架已可用部分）


| 模块                                                       | 位置                 | 状态                |
| -------------------------------------------------------- | ------------------ | ----------------- |
| 事件总线 `EventBus` / `Eventer`                              | `Maple.Core`       | ✅ 已实战验证           |
| 对象池 `PoolProvider` / `Pooler` / `GameObjectPool`         | `Maple.Core`       | ✅ 实现完整，游戏层尚未使用    |
| 服务中心 `ServiceHub`                                        | `Maple.Core`       | ✅ 已实战验证           |
| 日志 `GLogger` / `LogTag`                                  | `Maple.Core`       | ✅ 已实战验证           |
| 集合工具 `SerializableDictionary`                            | `Maple.Core`       | ✅ 含 Editor Drawer |
| UI 框架 `UIManager` / Panel / Window                       | `Maple.UI`         | ✅ 已实战验证           |
| 资源加载 `ResourcesLoader`                                   | `Maple.Extensions` | ✅ 已实战验证           |
| AB 包加载 `AssetBundleLoader`                               | `Maple.Extensions` | ⚠️ 实现完整，未验证       |
| 音频服务 `AudioService`                                      | `Maple.Extensions` | ⚠️ 实现完整，游戏层零使用    |
| 网络客户端 `HttpRestClient`                                   | `Maple.Extensions` | ✅ 已实战验证（LLM 链路）   |
| 序列化 `NewtonsoftJsonSerializer` / `JsonSerializer`        | `Maple.Extensions` | ✅ 已实战验证           |
| 启动骨架 `GameRoot` / `IFrameworkService` / `IGameBootstrap` | `Maple.Framework`  | ✅ 已实战验证           |
| 状态机 `GameStateManager<T>`                                | `Maple.Framework`  | ⚠️ 实现完整，游戏层零使用（FSM 实战化待真实游戏定义 `EAppState`） |
| 输入抽象 `InputManagerBase<T>`                               | `Maple.Framework`  | ✅ 已实战验证           |
| 场景加载 `ISceneLoader` / `SceneManagerSceneLoader`          | `Maple.Core` / `Maple.Extensions` | ✅ 已实战验证（Phase 3，最小闭环） |
| 场景流程 `ISceneFlowService` / `SceneFlowService`            | `Maple.Core` / `Maple.Framework`  | ✅ 已实战验证（Phase 3，最小闭环） |
| 配置索引 `IConfigProvider` / `ConfigProvider`                | `Maple.Core`       | ✅ 已实战验证（Phase 4，独立 demo） |
| 配置便利件 `ScriptableObjectConfigTable<TKey,TEntry>`         | `Maple.Extensions` | ✅ 已实战验证（Phase 4，独立 demo） |
| 实体接口族 `IEntity` / `IEntityFactory` 等                     | ~~`Maple.Core`~~   | 🗑️ 已于收束阶段移除（零实现零使用）；替代方案见 `指南-实体与局内对象管理.md` |


---

## Phase 0 — 框架基建 / 开源化准备

### Step 0a：Namespace 收敛 + asmdef 重命名

- 5 个 asmdef 重命名（`MapleFramework.*` → `Maple.*`）
- 54 个框架 `.cs` 文件 namespace 收敛到分层命名（`Maple.Core` / `Maple.UI` / `Maple.Framework` / `Maple.Extensions`）
- 11 个游戏层文件的 `using MapleFramework` 替换为精确分层 using
- Unity 编译通过 + 运行时验证（`FrameworkVerification` 按 V 全绿）

### Step 0b：GameRoot 网络栈解耦

- 添加 Inspector 开关 `registerDefaultSerializer` / `registerHttpClient`（默认 true，向后兼容）
- 两行硬编码注册改为有条件执行，防重复注册
- `Maple.Core` + `Maple.UI` 可脱离 Newtonsoft / UniTask 独立使用

### Step 0c：UPM 包骨架

- [~] 暂缓。当前框架代码通过 asmdef 已实现复用隔离，等框架内容丰富、准备开源时再建独立仓库 + `package.json`。

---

## Phase 1 — TimerService（定时器 / 调度）

**目标**：提供统一的延迟回调、循环调度，支持游戏暂停时的 timeScale 隔离。  
**依赖**：`ITickRegistry`（已有），零外部依赖。  
**放置层**：契约 → `Maple.Core`，实现 → `Maple.Framework`（需要 Tick 驱动）。

### 接口设计（`ITimerService`）

```csharp
public interface ITimerService
{
    // 延迟执行，返回 handle 可用于取消
    int Schedule(float delay, Action onComplete, bool ignoreTimeScale = false);
    // 循环执行，repeatCount = -1 表示无限循环
    int ScheduleRepeating(float interval, Action onTick, int repeatCount = -1);
    void Cancel(int handle);
    void PauseAll();   // 配合游戏暂停
    void ResumeAll();
}
```

### 步骤

- `ITimerService` 接口写入 `Maple.Core/Contracts/Timer/`
- `TimerService` 实现写入 `Maple.Framework/Timer/`，实现 `IFrameworkService` + `ITickable`
- `GameRoot` 扫描自动初始化（无需手动注册，挂组件即可）
- `ServiceHub.Register<ITimerService>` 在 `TimerService.Initialize()` 内完成
- **验证**：`TestGameBootstrap.cs` 包含完整的 `ignoreTimeScale` 隔离测试，运行通过

### 已修复问题（v0.1）

> `Time.unscaledDeltaTime` 不受 `Time.maximumDeltaTime` 封顶，而 `Time.deltaTime` 受封顶。
> 在初始化帧或卡顿帧里，两者差值可达数秒，导致 `ignoreTimeScale=true` 的计时器连续触发、
> 领先于 `ignoreTimeScale=false` 的计时器。
>
> **修复**：`TimerService.Tick()` 中对 `unscaledDelta` 同样施加 `maximumDeltaTime` 封顶：
>
> ```csharp
> float unscaledDelta = Mathf.Min(Time.unscaledDeltaTime, Time.maximumDeltaTime);
> ```
>
> 两个时间源的最大步长对齐，无论注册时机如何，行为一致。

---

## Phase 2 — SaveService（存档）

**目标**：提供按槽位整存整取的持久化能力（本期范围）。  
**依赖**：`ISerializer`（已有），`Application.persistentDataPath`。  
**放置层**：契约 → `Maple.Core`，实现 → `Maple.Extensions`（复用 `ISerializer`）。

### 接口设计（`ISaveService`）

```csharp
public interface ISaveService
{
    void Save<T>(string slot, T data);
    T Load<T>(string slot, T fallback = default);
    bool Exists(string slot);
    void Delete(string slot);
}
```

### 步骤

- `ISaveService` 接口写入 `Maple.Core/Contracts/Save/`
- `JsonSaveService` 实现写入 `Maple.Extensions/Save/`（依赖 `ISerializer`，构造注入）
- `GameRoot` 注册（Inspector 开关 `registerSaveService`，构造时传入 `ISerializer`）
- 版本信封 `SaveEnvelope<T>`（存档文件带 `Version` 字段，留迁移挂载点，本期不写迁移逻辑）
- 原子写（temp → `File.Replace`/`Move`），防止写入中断损坏已有存档
- 验证：往返一致 ✓；跨 Play 会话 Save→重进→Load 数据一致 ✓（`TestGameBootstrap.cs`）

### 本期边界（已完成的部分，后续可选做的部分）

- 仅做整存整取槽位模式；轻量 KV（设置/偏好）后续再补独立接口
- 明文 JSON；加密 / 混淆作为后续可选（注入加密版 `ISerializer` 即可，`JsonSaveService` 不改）
- 无版本迁移逻辑、无异步存档、无云同步

---

## Phase 3 — SceneFlow（✅ 已完成）+ FSM 实战化（延后）

> ⚠️ 本节原草案与最终实现有出入，已按实际决策（见 **ADR-006**）纠正：
> ① 场景**不走** `IResourceLoader`（它加载不了场景），改为独立的 `ISceneLoader` 抽象；
> ② 契约用**标准 `Task`** 而非 `UniTask`，以守住 `Maple.Core` 零第三方依赖（ADR-002）；
> ③ FSM 实战化**延后**到有真实游戏定义 `EAppState` 时再做，本期只交付 SceneFlow 最小闭环。

**目标**：补一套异步场景切换能力，抽象出可替换的场景加载后端，为 Phase 5（Addressables）与未来 FSM 联动打基础。  
**依赖**：`SceneManager`（引擎自带）；无第三方依赖。  
**放置层**：契约 → `Maple.Core`；场景后端实现 → `Maple.Extensions`；流程编排 → `Maple.Framework`。

### 接口设计（最终版）

```csharp
// Maple.Core —— 场景加载后端抽象（可替换：SceneManager / 未来 Addressables）
public interface ISceneLoader
{
    Task LoadSceneAsync(string sceneKey, IProgress<float> progress = null);
}

// Maple.Core —— 场景流程服务：UI 无关、游戏规则无关，只切场景 + 抛事件
public interface ISceneFlowService
{
    bool IsLoading { get; }
    Task LoadAsync(string sceneKey);
    event Action<string> OnLoadStarted;
    event Action<float>  OnLoadProgress;   // 0..1
    event Action<string> OnLoadCompleted;
}
```

### 已完成内容

- `ISceneLoader` / `ISceneFlowService` 写入 `Maple.Core/Contracts/Scene/`（标准 `Task`，零依赖）
- `SceneManagerSceneLoader`（`Maple.Extensions/Scene/`）实现 `ISceneLoader`，基于 `SceneManager.LoadSceneAsync`（单场景模式），场景取自 Build Settings
- `SceneFlowService`（`Maple.Framework/Scene/`）为纯 C# 类，构造注入 `ISceneLoader`（ADR-001），防重入 + 回调 `try/catch` 兜底
- `GameRoot` 注册：`ISceneLoader` 与 `IResourceLoader` 同源于 `EResourceLoaderType`；`ISceneFlowService` 经开关 `registerSceneFlow`（默认 true）注册
- `LogTag` 新增 `SCENE`
- **最小闭环 Demo**（游戏层 `FrameworkDemo`）：`SceneFlowDemo` 按 `N` 键在两场景间切换，切换期间 `Show/Hide<LoadingWindow>()`；`LoadingWindow` 为 `Global` 生命周期，订阅 `OnLoadProgress` 刷新进度条

### 本期边界与未来扩展点（详见 ADR-006）

- **只做单场景模式（`Single`）**；Additive 叠加加载 / 卸载延后
- **只做异步**：场景无同步加载接口（Addressables 无同步场景 API，抽象须反映后端最弱能力）；资产的同步加载仍保留在 `IResourceLoader.Load<T>`
- **FSM 实战化延后**：定义 `EAppState`、用 `GameStateManager<EAppState>` 驱动"主菜单→Loading→战斗→结算"全链路，留待真实游戏项目落地；届时只需把 Demo 的按键触发换成订阅 `GameStateChangedEvent<EAppState>` 后调 `LoadAsync`，框架侧零改动
- **激活闸门（allowSceneActivation）/ CancellationToken / Addressables 后端** 均为已登记扩展点

### 手动配置（Unity 侧，需你操作一次）

- 新建两个场景（如 `Boot` / `Battle`），都加入 **Build Settings**；仅启动场景挂 `GameRoot(DontDestroyOnLoad=true)`
- 把 `SceneFlowDemo` 挂到启动场景中 **GameRoot 所在 GameObject** 上（随 GameRoot 跨场景存活），按需设置 `sceneA`/`sceneB` 名
- 制作 `LoadingWindow` 预制体放 `Resources/UI/Windows/LoadingWindow`（含 `CanvasGroup` + `LoadingWindow` 组件，可选挂 `Slider`/`Text` 并在 Inspector 关联）

---

## Phase 4 — Config / 表驱动（✅ 已完成）

> ⚠️ 本节原草案与最终实现有出入，已按实际决策（见 **ADR-007**）纠正：
> ① 键类型 `int` → **泛型 `TKey`**（int/string/enum 通用，且枚举/int 键不装箱，省 GC）；
> ② 纯索引实现放 **`Maple.Core`**（零依赖基建，与 EventBus/PoolProvider 同级），非草案的 Extensions；
> ③ 命名从 `ScriptableObjectConfigProvider` 拆为"索引层 `ConfigProvider`（Core）"+"可选 SO 便利件（Extensions）"，
>    索引与加载分离，框架不绑定数据模型。

**目标**：提供「注册 + 按键索引」通用机制，让"数据数量变化"不再引起"代码结构变化"；表结构由游戏层定义。  
**依赖**：无（索引层零依赖）；SO 便利件依赖 UnityEngine。  
**放置层**：契约 + 索引实现 → `Maple.Core`；可选 SO 便利件 → `Maple.Extensions`；加载 → 游戏层。

### 接口设计（最终版）

```csharp
public interface IConfigProvider
{
    void Register<TKey, TConfig>(IReadOnlyDictionary<TKey, TConfig> table);
    TConfig Get<TKey, TConfig>(TKey key);                 // 未命中：告警 + 返回 default
    bool TryGet<TKey, TConfig>(TKey key, out TConfig config);
    IReadOnlyDictionary<TKey, TConfig> GetTable<TKey, TConfig>();
    bool Has<TConfig>();
    void Clear();
}
```

### 已完成内容

- `IConfigProvider`（`Maple.Core/Contracts/Config/`）+ `ConfigProvider`（`Maple.Core/Runtime/Config/`，纯 C# 零依赖内存注册表）
- `ServiceHub.Initialize()` 默认注册 `IConfigProvider`（随处可用，无需开关）
- `LogTag` 新增 `CONFIG`
- 可选便利件 `ScriptableObjectConfigTable<TKey,TEntry>`（`Maple.Extensions/Config/`）：抽掉"List→字典"样板，含重复键告警+保留首个；游戏层定义具体子类并实现 `GetKey`
- **独立验证 Demo**（`FrameworkDemo/ConfigDemo`）：按 `C` 键跑一轮 `[P]/[F]`，全部代码内构造数据、零手动配置，覆盖 Register/Get命中/Get未命中/TryGet/GetTable/Has 与 SO 便利件 BuildTable（含重复键）

### 本期边界与未来扩展点（详见 ADR-007）

- **只做同步内存索引**：无热重载、无复杂查询（复杂查询属数据库层，非目标）
- **一种 `TConfig` 类型一张表**：同类型多表需自行用包装类型区分
- **只存不可变配置模板**：运行时可变状态（如 `UnitData`）不得放入
- **真实游戏 `UnitStats` / `UnitViewConfig` 改造**：留待下个项目实战（本期以独立 demo 验证，不动已跑通的游戏）
- **CSV/JSON → SO 烘焙 Editor 工具**：可选后置

### SO 便利件用法（三步接入真实项目）

```csharp
// 第一步：定义条目（普通 [Serializable] 类，不继承 ScriptableObject）
[Serializable]
public class SpellConfig
{
    public ESpellId Id;   // 作为键的字段
    public float Cooldown;
    public int Power;
}

// 第二步：定义具体子表（必须闭合泛型，Unity 才能存成 .asset 资源）
[CreateAssetMenu(menuName = "YourGame/Spell Config Table")]
public class SpellConfigTable : ScriptableObjectConfigTable<ESpellId, SpellConfig>
{
    protected override ESpellId GetKey(SpellConfig entry) => entry.Id;
}
```

```csharp
// 第三步：启动时加载一次并注册（如 GameBootstrap）
var table = ServiceHub.Require<IResourceLoader>()
                      .Load<SpellConfigTable>("Config/SpellConfigTable");
ServiceHub.Require<IConfigProvider>().Register(table.BuildTable());

// 之后任意系统按键取用
var fireball = ServiceHub.Require<IConfigProvider>()
                         .Get<ESpellId, SpellConfig>(ESpellId.Fireball);
```

> **注意**：只放静态配置模板（不可变）。运行时可变状态（血量、冷却倒计时等）不要放进配置表。

### 验证 Demo

| 按键 | 脚本 | 内容 | 需手动配置 |
|------|------|------|-----------|
| `C` | `ConfigDemo` | **纯代码内存字典**：Register/Get/TryGet/GetTable/Has（不涉及 SO） | 否 |
| `O` | `ConfigSoDemo` | **手动配置的 SO 资源**：加载 → BuildTable → Register → Get 往返一致，并打印内容供核对 | 是 |

两个 Demo 数据类型独立（`WeaponConfig` vs `SpellConfig`），互不干扰。均挂到含 `GameRoot` 的场景里任意 GameObject。

`ConfigSoDemo` 是**真实的手动配置路径**验证，需先：
1. 菜单 `Create → BattleAgent Demo → Spell Config Table` 生成 `.asset`
2. 在资源 Inspector 的 `entries` 列表手动填几条技能数据
3. 把资源拖到 `ConfigSoDemo` 的 `Spell Table` 字段
4. 进 Play 按 `O`，Console 应全 `[P]` 并打印出你填的内容

---

## Phase 5 — AddressablesLoader（资源后端验收）〔已暂缓〕

> **状态：暂缓**（2026-07-05）。当前项目范围用不上 Addressables，且验收需要较重的手工资源配置。抽象（`IResourceLoader` / `ISceneLoader` + `EResourceLoaderType`）已就位，将来真需要时按下述步骤补 `Addressables` 实现即可，上层代码零改动。

**目标**：新增 `IResourceLoader` 的 Addressables 实现，验证 Phase 3/4 的上层代码零改动能切换资源后端。  
**依赖**：`com.unity.addressables` 包。  
**放置层**：`Maple.Extensions/`，新增 `EResourceLoaderType.Addressables` 枚举项。

### 步骤

- 工程安装 `com.unity.addressables` 包
- `AddressablesLoader` 实现 `IResourceLoader`，写入 `Maple.Extensions/`
- `EResourceLoaderType` 枚举加 `Addressables` 项
- `GameRoot` switch 分支补对应注册
- **验收标准**：切换到 Addressables 后，Phase 3/4 的代码一行不动仍能运行

---

## Phase 6 — Entity 系统（已决策：不做，空接口已移除）

**结论**（2026-07-05）：早期 `Maple.Core` 里的实体空接口族（`IEntity` / `IEntityFactory` / `IIdAllocator` 等）**已全部删除**——零实现、零使用，属误导性负债。框架**不提供**实体系统。

**替代方案与何时才真正需要**：见 `指南-实体与局内对象管理.md`。要点：

- 局内对象用「普通数据类 + 一个 Manager 持有集合 + 逻辑/表现分离」即可（本项目 BattleAgent 就是范例）。
- 只有当「对象种类多且共享统一机制」「横切系统爆炸」「海量实体需极致性能」等信号同时出现，才考虑正式实体系统（GameObject 组件式 vs ECS 的取舍见指南）。
- 纪律：先在游戏层跑通、被多个项目验证，再上升为框架接口——不重蹈空接口覆辙。

---

## 分发形态〔独立仓库 + UPM Git URL〕

**决策**：见 [ADR-008](ADR/ADR-008-distribution-upm-git.md)（取代 ADR-003）。分发形态为「独立 Git 仓库 + UPM Git URL」，OpenUPM 按需再上。

**真源仓库**：`https://github.com/MapleTraveler/MapleFramework.git`。仓库根即包根，含 `package.json`（`com.maple.framework`）、`README.md`、`CHANGELOG.md`、`LICENSE.md`。当前版本 **1.1.0**，`version` 与 tag `vX.Y.Z` 恒等。

**前置依赖**：UniTask（Git URL 手动安装）、Newtonsoft（已在 `package.json` 声明）。

**消费方接入方式**：

| 项目 | 接入方式 | 说明 |
| --- | --- | --- |
| MELTDOWN | git subtree，落在 `Packages/com.maple.framework` | 工程侧只执行 `git subtree pull`；框架改动一律回真源仓库做，不在工程内的副本里直接改 |
| BattleAgentCore | 工程内目录 `Assets/Scripts/MapleFramework` | 框架发源工程，当前作只读参考，不随真源仓库自动更新 |

**发版固定动作**（操作细节见 `playbook-抽取与升级.md`）：

改代码 → 更新 `CHANGELOG.md` → 改 `package.json` 的 `version` → commit → `git tag vX.Y.Z` → push（含 tags）→ 消费工程按各自接入方式取新版。

---

## 设计纪律（贯穿所有阶段）


| 纪律                     | 说明                                    |
| ---------------------- | ------------------------------------- |
| 每个模块必须有 Demo 验证        | 未经运行的框架代码 = 负债                        |
| 新模块构造注入依赖              | 不在实现内部 `ServiceHub.Get`，便于单独测试        |
| 框架不含游戏规则               | 战斗、AI、关卡逻辑永远在游戏层                      |
| 接口在 `Maple.Core`，实现在上层 | 不破坏依赖方向                               |
| 同层程序集不互相引用             | `Maple.UI` 不引用 `Maple.Framework`，反之亦然 |


