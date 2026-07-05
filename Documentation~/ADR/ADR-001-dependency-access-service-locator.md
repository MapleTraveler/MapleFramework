# ADR-001：依赖获取方式 —— ServiceHub 服务定位，而非 DI 容器

## 背景（Context）
框架内有多类服务需要被各处获取：UI、资源加载、序列化、网络、计时、存档等。项目由毕设起家，规模中小，团队极小（基本单人），但目标是长期可维护、甚至开源。需要一种"注册 + 获取"机制贯穿全框架。

## 问题（Problem）
用什么机制管理服务的注册与获取？是否要引入正式的依赖注入（DI）容器（VContainer / Zenject）？

## 可选方案（Options）
- **A. 纯手动构造 + 层层传参**：无中心，谁用谁 new、靠参数传递。
- **B. Service Locator（ServiceHub 静态注册表）** —— 采用。中心化注册，`ServiceHub.Get<T>() / Require<T>()` 获取。
- **C. DI 容器（VContainer / Zenject）**：构造注入 + 容器管理生命周期 / scope。

## 最终方案（Decision）
采用 **B（ServiceHub）**，但叠加一条**实现纪律**：服务之间的依赖通过**构造函数注入**传递（如 `JsonSaveService(ISerializer)`、`HttpRestClient(ISerializer)`），**不在实现内部到处 `ServiceHub.Get`**。ServiceHub 只承担"组合根 / 入口检索"的角色，在 `GameRoot` 与 `Bootstrap` 处集中注册与取用。

## 为什么（Rationale）
复杂度与收益的权衡。VContainer 带来 scope 生命周期、与 MonoBehaviour 集成、学习成本等额外心智负担，对当前规模收益不明显。ServiceHub 足够简单直观；而"构造注入纪律"保留了可测试性与清晰的依赖关系，使得未来若要迁移到 DI 容器，成本可控（不会满地都是隐式 `Get` 调用需要清理）。

## Pros
- 实现简单、零额外第三方依赖、上手快、调试直观。
- MonoBehaviour 型服务可 Inspector 挂载 + `GameRoot` 自动扫描初始化（`IFrameworkService`）。
- 配合构造注入纪律，单个服务可脱离容器单独 new 出来做测试。

## Cons
- 本质是全局可变状态，存在被滥用成"满地 `ServiceHub.Get`"的风险（靠纪律约束，非编译期强制）。
- 依赖关系是隐式的，编译期看不出"谁依赖谁"。
- 生命周期 / scope 能力弱，多实例、子容器、按场景隔离都不方便。

## 未来什么时候可能修改（Revisit）
出现以下任一信号时，评估迁移到 VContainer：
- 多场景 / 多对局需要独立 scope 与生命周期隔离；
- 团队扩大，需要编译期强约束依赖关系、防止 Service Locator 被滥用；
- 单元测试覆盖要求显著提高。
迁移前提：始终保持构造注入纪律，使迁移成本最小化。

## 影响模块（Impact）
`ServiceHub`、`GameRoot`、所有 `IFrameworkService` 实现，以及游戏层取用服务处。

## 日期
2026-06-29（记录）
