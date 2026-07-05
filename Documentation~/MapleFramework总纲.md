# MapleFramework 总纲

## 一、分层架构总览

```
╔══════════════════════════════════════════════════════════════╗
║                    GAME FRAMEWORK                            ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  Layer 0 ─ Core.Contracts          纯接口 · 零依赖            ║
║  ─────────────────────────────────────────────────────       ║
║  Layer 1 ─ Core.Runtime            基础设施 · 依赖 L0         ║
║  ─────────────────────────────────────────────────────       ║
║  Layer 2 ─ Framework               核心框架 · 依赖 L0+L1+L4   ║
║  ─────────────────────────────────────────────────────       ║
║  Layer 3 ─ UI                      UI 框架 · 依赖 L0+L1       ║
║  ─────────────────────────────────────────────────────       ║
║  Layer 4 ─ Extensions              扩展模块 · 依赖 L0+L1      ║
║                                                              ║
╠══════════════════════════════════════════════════════════════╣
║              ↑ 框架层（跨项目复用）                            ║
║              ↓ 游戏层（每个项目自行实现）                       ║
╠══════════════════════════════════════════════════════════════╣
║                                                              ║
║  Game Layer ─ 具体游戏项目                                    ║
║      引用框架层，实现具体游戏逻辑                               ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

---

## 二、各层详细内容

### Layer 0 — Core.Contracts

定位：框架的契约层。只有接口和值类型定义，不包含任何实现。所有上层都可以依赖它，它不依赖任何人。

| 系统       | 接口/类型 | 说明 |
|------------|-----------|------|
| 事件系统   | IEventBus | Subscribe / Publish |
| 对象池     | IPoolProvider, IObjectPool, IPoolable | 池化契约 |
| Tick 系统  | ITickable, IFixedTickable, ITickRegistry | 帧驱动与注册契约 |
| 服务生命周期 | **IFrameworkService**, **IGameBootstrap** | Initialize()/InitOrder、Bootstrap()/BootOrder，由 GameRoot 统一驱动 |
| 资源系统   | IResourceLoader | 资产 Load / LoadAsync / Release |
| 场景系统   | ISceneLoader, ISceneFlowService | 场景异步加载后端 + 流程编排（Phase 3） |
| 定时系统   | ITimerService | 延迟 / 循环调度、timeScale 隔离（Phase 1） |
| 存档系统   | ISaveService | 槽位整存整取（Phase 2） |
| 配置系统   | IConfigProvider | 按键索引配置表（Phase 4） |
| 序列化     | ISerializer | 对象 ↔ 字节 |
| 网络       | IRequestResponseClient | 请求-响应（REST / LLM），标准 Task |
| 音频系统   | IAudioService | PlayBGM / PlaySFX / 音量控制 |
| 输入系统   | IInputManager\<TActionMap\> | 输入抽象契约 |
| 类型定义   | EventToken, ERegisterFailReason | 通用值类型 |

asmdef: **MapleFramework.Core**（内含 Contracts / Runtime 目录，零外部引用）

> **关于实体 / 局内对象管理**：框架**不提供** Entity / EntityManager 系统（早期的空接口族已于收束阶段移除）。
> 局内对象（单位、子弹等）的管理推荐用「普通 C# 数据类 + 一个 Manager 持有集合」的朴素做法；
> 何时才真正需要一套正式实体系统（及 GameObject 组件式 vs ECS 的取舍），见 `指南-实体与局内对象管理.md`。

---

### Layer 1 — Core.Runtime

定位：框架的基础引擎。提供最底层的通用能力，所有上层模块都会用到。

| 系统     | 关键类 | 说明 |
|----------|--------|------|
| 事件总线 | EventBus, Eventer（静态门面） | 已成熟 |
| 对象池   | PoolProvider, ObjectPool\<T\>, GameObjectPool, Pooler（静态门面） | 已成熟 |
| 服务中心 | ServiceHub | Register\<T\>() / Get\<T\>() / Require\<T\>()；Initialize 默认注册 EventBus / PoolProvider / **ConfigProvider** |
| 配置索引 | **ConfigProvider**（实现 IConfigProvider） | 内存类型化表，泛型键防装箱（Phase 4） |
| 日志     | GLogger, LogTag | 支持 Tag、HideInCallstack、条件编译剥离 |
| 集合工具 | SerializableDictionary | 含 Editor Drawer |

asmdef: **MapleFramework.Core**（引用无；与 Contracts 同程序集）

---

### Layer 2 — Framework

定位：游戏运行的核心骨架。唯一入口 GameRoot，驱动 ServiceHub、资源加载、框架级 Mono 服务初始化与 Tick。

| 系统       | 关键类 | 说明 |
|------------|--------|------|
| 启动流程   | **GameRoot** | 唯一入口；无 Instance，通过 ServiceHub 注册；Awake 内完成 ServiceHub、IResourceLoader（按枚举）、ITickRegistry 注册后，扫描并初始化所有 **IFrameworkService** |
| 资源加载类型 | **EResourceLoaderType** | 枚举：None / Resources（预留 AssetBundle、Addressables） |
| 游戏状态   | GameStateManager\<TState\>, GameStateChangedEvent\<TState\> | 泛型状态机，由游戏层定义枚举 |
| 输入系统   | InputManagerBase\<TActionMap\> | 抽象基类，子类实现 OnInitialize / OnActionMapSwitch |
| 定时器     | **TimerService**（实现 ITimerService + ITickable） | 挂 GameRoot，统一 Tick 驱动（Phase 1） |
| 场景流程   | **SceneFlowService**（实现 ISceneFlowService） | 纯 C# 构造注入 ISceneLoader，UI/游戏规则无关（Phase 3） |

asmdef: **MapleFramework.Framework** — 引用 **Core + Extensions**

---

### Layer 3 — UI

定位：独立 UI 框架。Panel/Window 双层体系，由 GameRoot 通过 IFrameworkService 统一初始化，不依赖 Framework 程序集。

| 系统     | 关键类 | 说明 |
|----------|--------|------|
| UI 入口  | **UIManager** | 实现 IFrameworkService，由 GameRoot 调 Initialize()；全局唯一，自动建 Canvas 层级 |
| 控制器基类 | UIController, PanelController, WindowController | 基类 + OnShow/OnHide/OnSetProperties 回调，无内置动画 |
| 层级管理 | PanelLayer, WindowLayer | Panel 多并存；Window 栈管理、关闭恢复上一个 |
| 生命周期 | **UILifeScope** | Scene / Global，场景切换时自动清理 Scene 级 UI |
| 路径约定 | — | Panel → `UI/Panels/{类名}`，Window → `UI/Windows/{类名}`，经 IResourceLoader 加载 |

asmdef: **MapleFramework.UI** — 引用 **Core**（不引用 Framework）

---

### Layer 4 — Extensions

定位：可选扩展模块。提供常用但非核心的通用能力，项目可按需使用或替换。

| 系统     | 关键类 | 说明 |
|----------|--------|------|
| 音频服务 | AudioService（实现 IAudioService） | CrossFade、缓存、音量控制，可替换 ClipLoader |
| 资源加载 | **ResourcesLoader**, **AssetBundleLoader**（实现 IResourceLoader） | Resources 目录 / AB 包，依赖与引用计数；GameRoot 按 EResourceLoaderType 注册 ResourcesLoader |
| 场景加载 | **SceneManagerSceneLoader**（实现 ISceneLoader） | 基于 SceneManager，场景来自 Build Settings（Phase 3） |
| 存档服务 | **JsonSaveService**（实现 ISaveService） | 构造注入 ISerializer，原子写 + 版本信封（Phase 2） |
| 网络客户端 | **HttpRestClient**（实现 IRequestResponseClient） | 构造注入 ISerializer，REST / LLM 链路 |
| 序列化   | **NewtonsoftJsonSerializer**, **JsonSerializer**（实现 ISerializer） | Newtonsoft / Unity JsonUtility 两套 |
| 配置便利件 | **ScriptableObjectConfigTable\<TKey,TEntry\>** | 可选：把 List 条目 SO 转成字典喂给 IConfigProvider（Phase 4） |

asmdef: **MapleFramework.Extensions** — 引用 **Core**

---

### 框架启动与生命周期时序

1. **GameRoot.Awake** 顺序固定：  
   - ServiceHub.Initialize()（EventBus、PoolProvider 等）  
   - 按 **EResourceLoaderType** 注册 IResourceLoader（None 则不注册）  
   - 注册 GameRoot、ITickRegistry，可选 DontDestroyOnLoad  
   - **InitializeFrameworkServices()**：GetComponentsInChildren\<IFrameworkService\>，按 **InitOrder** 依次调用 **Initialize()**  
2. 依赖 IResourceLoader 的框架服务（如 **UIManager**）均实现 IFrameworkService，不在 Awake 中自初始化，保证 IResourceLoader 先于 UIManager 就绪。

---

## 三、依赖关系图

```
       Core (Contracts + Runtime)
         ▲     ▲           ▲
         │     │           │
    Extensions  Framework   UI
         ▲     ▲
         │     │
         └─────┴── Framework 引用 Extensions（GameRoot 内 new ResourcesLoader）
```

**严格规则：**

- 同层之间不互相引用（UI 不引用 Framework，Framework 不引用 UI）
- 上层不可被下层引用（Runtime 不引用 Framework）
- Framework 引用 Extensions（GameRoot 按枚举注册 ResourcesLoader）
- UI 与 Framework 通过 Core 的 **IFrameworkService** 协作：GameRoot 只依赖 Core 接口，扫描到 UIManager 后调 Initialize()

---

## 四、设计原则

| 原则 | 具体做法 |
|------|----------|
| 框架不含游戏规则 | “Boss 死播 BGM” 等逻辑永远在游戏层 |
| 接口在 L0，实现在 L1+ | 所有公共能力先定接口 |
| 唯一入口与生命周期 | 框架级 Mono 服务实现 **IFrameworkService**，由 **GameRoot** 在合适时机统一 **Initialize()**，不在 Awake 中自初始化 |
| 静态门面可选 | Eventer / Pooler 作为快捷方式；纯 C# 类可通过接口注入 |
| 框架层不暴露单例 | 不提供 XXX.Instance，通过 ServiceHub 注册 |
| 游戏层自由 | 项目可选择用单例、ServiceHub 或接 DI 容器 |
