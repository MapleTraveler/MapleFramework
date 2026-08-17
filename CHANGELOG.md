# Changelog

本文件遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/) 与 [语义化版本](https://semver.org/lang/zh-CN/)。
自 `1.0.0` 起承诺兼容：破坏性改动必须进 major 版本，并在此明确标注。

## [1.2.0] - 2026-08-17

### Added
- `ServiceHub.Unregister<T>(T expectedInstance)`：仅当当前注册实例与期望实例相同时注销，防止旧实例误删新持有者。

### Changed
- 无参 `ServiceHub.Unregister<T>()` 的文档明确为组合根关闭专用；业务侧优先使用带期望实例的重载。

## [1.1.0] - 2026-08-17

### Added
- `ServiceHub.Unregister<T>()`：按接口类型注销服务，用于会话结束等需要「移除」而非「覆盖」的场景；未注册时返回 `false`。

### Fixed
- 修正版本号真源：`1.0.0` 发版时 `package.json` 的 `version` 仍为 `0.1.0`，与 tag `v1.0.0` 不一致。自本版起 `version` 与 tag 恒等（ADR-008）。

## [1.0.0] - 2026-07-05

首个可作为 UPM 包分发的版本，涵盖 Phase 0–4 全部能力，并完成一次收束清理。

### Added
- **配置系统（Phase 4）**：`IConfigProvider` + `ConfigProvider`，泛型键防装箱，ServiceHub 默认注册；`Maple.Extensions` 提供可选便利件 `ScriptableObjectConfigTable<TKey,TEntry>`。
- **场景系统（Phase 3）**：`ISceneLoader`（后端抽象）+ `ISceneFlowService`（切换编排，含进度/开始/完成事件，UI 与规则无关）；实现 `SceneManagerSceneLoader`、`SceneFlowService`；`GameRoot` 增加 SceneFlow 注册开关。
- **定时系统（Phase 1）**：`ITimerService` + `TimerService`（统一 Tick 驱动、句柄、timeScale 隔离）。
- **存档系统（Phase 2）**：`ISaveService` + `JsonSaveService`（原子写 + 版本信封，复用 `ISerializer`）。
- 打包与文档：`package.json`、`README.md`、本 `CHANGELOG.md`、`指南-实体与局内对象管理.md`、`playbook-抽取与升级.md`。

### Changed
- `MapleFramework总纲.md` 全面校正，补齐 Phase 1–4 服务清单，修正过时描述。
- 分发策略由「延后」改为「启用独立仓库 + UPM Git URL」，见 ADR-008（取代 ADR-003）。

### Removed
- **移除 Entity 空接口族**：`IEntity` / `IHasId` / `IIdAllocator` / `IEntityFactory` / `IEntityLifecycle` / `IEntityUpdater` / `IEntityInitializer` / `IContextApplier` / `ISystemOrder` 及 `EEntityCreateFailReason`。这些接口零实现零使用，属误导性负债；局内对象管理的推荐做法见实体指南。

### 前置依赖
- **UniTask**（Cysharp，Git URL 手动安装）—— `Maple.Framework` / `Maple.Extensions` 必需。
- **com.unity.nuget.newtonsoft-json** —— 已在 `package.json` 声明。
