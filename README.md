# MapleFramework

一个**可裁剪、零游戏规则**的 Unity 游戏框架。用分层 + 服务定位（ServiceHub）把「基础设施」和「游戏逻辑」彻底隔开：框架只提供事件、对象池、Tick、资源、场景、定时、存档、配置、网络、音频、UI 这些**通用能力**，玩法规则一律留在游戏层。

- 架构详解：[`MapleFramework总纲.md`](Documentation~/MapleFramework总纲.md)
- 进度与路线图：[`MapleFramework进度.md`](Documentation~/MapleFramework进度.md)
- 关键决策（为什么这么定）：[`ADR/README.md`](Documentation~/ADR/README.md)
- 局内对象怎么管 / 要不要实体系统：[`指南-实体与局内对象管理.md`](Documentation~/指南-实体与局内对象管理.md)

> 环境：Unity 2022.3（LTS）。C# 契约层零外部依赖；实现层用 UniTask。

---

## 一分钟上手

1. 场景里建一个空物体，挂 **`GameRoot`**（`Maple.Framework`）。它是唯一入口：`Awake` 里初始化 `ServiceHub`、按需注册内置服务、扫描并初始化子物体上的 `IFrameworkService`、驱动所有 `ITickable`。
2. 需要 MonoBehaviour 型服务（`TimerService`、`AudioService`、`UIManager` 等）时，把它们挂在 **GameRoot 的子物体**上——`GameRoot` 会按 `InitOrder` 自动初始化，服务在 `Initialize()` 里把自己注册进 `ServiceHub`。
3. 任意处取用服务：

```csharp
using Maple.Core;

// 取服务：Get 找不到返回 null；Require 找不到抛异常（推荐用于必需依赖）
var save   = ServiceHub.Require<ISaveService>();
var config = ServiceHub.Require<IConfigProvider>();

// 事件 / 对象池有静态门面，省去每次 Get
Eventer.Publish(new SomeEvent());
var go = Pooler.Get<MyPoolable>();
```

`GameRoot` Inspector 上的开关（都可关）：资源加载后端 `EResourceLoaderType`、默认序列化器、HTTP 客户端、存档服务、场景流程。不需要的功能关掉即可，不产生依赖。

---

## 服务速查表

| 能力 | 契约 | 所在层 | 获取方式 | 备注 |
|------|------|--------|----------|------|
| 事件总线 | `IEventBus` | Core | `ServiceHub.Get` 或静态 `Eventer` | ServiceHub 默认注册 |
| 对象池 | `IPoolProvider` | Core.Runtime | `ServiceHub.Get` 或静态 `Pooler` | ServiceHub 默认注册 |
| 配置表 | `IConfigProvider` | Core.Runtime | `ServiceHub.Get` | ServiceHub 默认注册；泛型键防装箱 |
| 帧驱动 | `ITickRegistry` / `ITickable` | Core / Framework | `ServiceHub.Get<ITickRegistry>()` | GameRoot 即注册中心 |
| 资源加载 | `IResourceLoader` | Extensions | `ServiceHub.Get` | GameRoot 按 `EResourceLoaderType` 注册 |
| 场景加载后端 | `ISceneLoader` | Extensions | `ServiceHub.Get` | 与资源后端同源，自动注册 |
| 场景切换编排 | `ISceneFlowService` | Framework | `ServiceHub.Get` | UI/规则无关，含进度事件 |
| 定时器 | `ITimerService` | Framework | `ServiceHub.Get` | MonoBehaviour 服务，需挂在 GameRoot 下 |
| 序列化 | `ISerializer` | Extensions | `ServiceHub.Get` | 默认 Newtonsoft，可换 |
| 网络请求 | `IRequestResponseClient` | Extensions | `ServiceHub.Get` | REST/LLM，依赖 `ISerializer` |
| 存档 | `ISaveService` | Extensions | `ServiceHub.Get` | 原子写 + 版本信封，依赖 `ISerializer` |
| 音频 | `IAudioService` | Extensions | `ServiceHub.Get` | MonoBehaviour 服务，需挂在 GameRoot 下 |
| 输入 | `IInputManager<TActionMap>` | Framework | 游戏层实现 | 抽象基类 `InputManagerBase` |
| UI 窗口 | `UIManager` / `WindowController` | UI | MonoBehaviour | 分层窗口管理 |

> **不提供**：Entity/实体系统（早期空接口已移除，理由与替代方案见实体指南）；玩法规则、具体游戏状态枚举（交给游戏层）。

---

## 分层与 asmdef（依赖单向向下）

| asmdef | 依赖 | 说明 |
|--------|------|------|
| `Maple.Core` | 无（仅 UnityEngine） | 契约 + 纯 C# 运行时，零第三方依赖 |
| `Maple.UI` | `Maple.Core` | 窗口/UI 框架 |
| `Maple.Extensions` | `Maple.Core`, `UniTask` | 各契约的具体实现（资源、场景、存档、网络、序列化、音频、配置便利件） |
| `Maple.Framework` | `Maple.Core`, `Maple.Extensions`, `UniTask` | 启动/编排（GameRoot、状态机、SceneFlow、Timer、输入基类） |
| `Maple.Core.Editor` | `Maple.Core`（Editor Only） | 编辑器扩展 |

---

## 安装（作为独立 UPM 包时）

### 前置依赖（必须先装，否则编译不过）

1. **UniTask**（Cysharp）——`Maple.Framework` / `Maple.Extensions` 按程序集名 `UniTask` 引用。
   Package Manager → Add package from git URL：
   ```
   https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
   ```
   > UniTask 走 Git URL 安装，**无法**写进本包 `package.json` 的 `dependencies`，只能作为手动前置依赖。
2. **Newtonsoft Json**——`NewtonsoftJsonSerializer` 依赖。已在本包 `package.json` 声明为依赖（`com.unity.nuget.newtonsoft-json`），一般会随包自动安装；若项目未装可手动 Add by name。

> 当前**不依赖** Addressables（`AssetBundleLoader` 用的是 AB 原生 API，非 Addressables）。将来做 Phase 5 再引入。

### 安装本框架

装好前置依赖后，Package Manager → Add package from git URL：

```
https://github.com/MapleTraveler/MapleFramework.git
```

需要锁版本时加 tag：`...MapleFramework.git#v1.0.0`。

---

## 抽取与升级

框架从本工程抽成独立仓库、以及日后如何升级/回灌改动，见 [`playbook-抽取与升级.md`](playbook-抽取与升级.md)。
