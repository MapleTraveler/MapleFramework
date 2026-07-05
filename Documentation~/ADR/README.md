# MapleFramework 架构决策记录（ADR）

> ADR = Architecture Decision Record。记录"为什么这样做"，而不是"代码怎么写"。
> 代码会变，但决策背后的取舍与边界需要长期沉淀，避免半年后自己都忘了为什么。

## 这些文档解决什么问题

AI 的记忆活在上下文里，会随聊天变长而遗忘、消耗 Token。本目录把**项目的"为什么"从 AI 上下文搬到项目自身**，于是：

- 换模型（GPT / Claude / Gemini）、换工具，都能靠读文档瞬间恢复项目背景
- 新开聊天不必复述历史，AI 只是"随时能加入的新成员"，不是唯一记得历史的人

## 新会话 onboarding —— 推荐阅读顺序

1. `README.md` —— 框架是什么 + 一分钟上手 + 服务速查 + 安装（入口）
2. `MapleFramework总纲.md` —— 架构是什么（5 层结构、各层职责）
3. `MapleFramework进度.md` —— 做到哪了、接下来做什么（状态 + 路线图 + 设计纪律）
4. `ADR/*.md`（本目录）—— 关键决策为什么这么定（取舍、边界、何时该推翻）

专题文档（按需）：
- `指南-实体与局内对象管理.md` —— 局内对象怎么管、要不要实体系统
- `playbook-抽取与升级.md` —— 抽独立仓库、UPM 分发与日后升级同步

读完前四类文档，即可无缝衔接既有工作。

## ADR 索引


| 编号                                                      | 主题                                                  | 状态  |
| ------------------------------------------------------- | --------------------------------------------------- | --- |
| [ADR-001](ADR-001-dependency-access-service-locator.md) | 依赖获取方式：ServiceHub 服务定位 vs DI 容器                     | 已采纳 |
| [ADR-002](ADR-002-layering-and-modular-reuse.md)        | 分层结构与可裁剪复用：Maple.* 命名空间 / asmdef / 可选服务开关           | 已采纳 |
| [ADR-003](ADR-003-distribution-strategy.md)             | 框架分发策略：asmdef 现用，UPM / 开源延后                         | 已被 ADR-008 取代 |
| [ADR-004](ADR-004-timer-service.md)                     | TimerService：统一 Tick 驱动 / 句柄 / timeScale 隔离 + 封顶修复  | 已采纳 |
| [ADR-005](ADR-005-save-service.md)                      | SaveService：槽位明文 JSON / 原子写 / 版本信封 / 复用 ISerializer | 已采纳 |
| [ADR-006](ADR-006-scene-flow.md)                        | SceneFlow：独立 ISceneLoader 抽象 / Core 用 Task / 场景只异步 / 编排在游戏层 | 已采纳 |
| [ADR-007](ADR-007-config-provider.md)                   | Config：泛型键防装箱 / 索引层进 Core / 加载交游戏层 / SO 便利件可选 | 已采纳 |
| [ADR-008](ADR-008-distribution-upm-git.md)              | 分发启用：独立 Git 仓库 + UPM Git URL（取代 ADR-003） | 已采纳 |


## ADR 模板（新增时复制）

```
# ADR-00X：<标题>
- 背景（Context）
- 问题（Problem）
- 可选方案（Options）
- 最终方案（Decision）
- 为什么（Rationale）
- 优点（Pros）
- 缺点/代价（Cons）
- 未来什么时候可能修改（Revisit）
- 影响模块（Impact）
- 日期
```

## 规则

- 一篇 ADR 只记一个决策；决策被推翻时，新增一篇并在旧篇标注"已被 ADR-0YY 取代"，**不删除历史**。
- ADR 是"决策快照"，记录当时的判断；不必随代码持续更新（那是进度文档的事）。

