# ADR-008：分发策略启用 —— 独立 Git 仓库 + UPM Git URL（取代 ADR-003）

## 背景（Context）
ADR-003 当时决定**延后** UPM 化，理由是框架内容不足、API 不稳、UPM 化收益低于成本，并列出 Revisit 条件：「模块数量与质量达标、公开 API 趋于稳定；有明确外部使用者」。

如今 Phase 0–4 落地（事件/池/Tick/资源/场景/定时/存档/配置/网络/音频/UI），公开 API 趋稳，且**已有明确复用需求**——要把框架用到新项目做实战验证。ADR-003 的 Revisit 条件**已满足**。

## 问题（Problem）
现在用什么方式把框架分发给其它项目复用？

## 可选方案（Options）
- **A. 继续手工拷贝目录**：跨项目零基础设施，但无版本、无升级路径，改动难同步。
- **B. 导出 `.unitypackage`**：一次性快照，无依赖声明、无版本治理。
- **C. 独立 Git 仓库 + `package.json`，消费端用 UPM Git URL** —— 采用。
- **D. OpenUPM / 私有 registry**：体验最好，但需要额外发布基础设施与公开意愿，当前无此需求。

## 最终方案（Decision）
采用 **C**：
- 框架抽成独立 Git 仓库，包根含 `package.json`（`com.maple.framework`）。
- 消费端通过 **UPM Git URL** 引用，`#vX.Y.Z` tag 锁版本。
- 本地联调用 `file:` 本地 package（可写、实时生效）。
- 版本遵循 SemVer，`version` 与 git tag 恒等；每次发版更新 `CHANGELOG`。
- **UniTask** 无法写进 `package.json` 依赖（Git URL 包），作为**手动前置依赖**在 README 明示；**Newtonsoft** 走 `dependencies` 声明。

具体操作见 `playbook-抽取与升级.md`。

## 为什么（Rationale）
- Git URL 分发**零发布基础设施成本**：一个仓库 + tag 即可，天然带版本、可回滚、可锁 commit。
- 相比手工拷贝/`.unitypackage`，它是唯一能长期支撑「一处改、多项目升级」的方案。
- OpenUPM/registry 的额外收益（检索、免手填 URL）当前用不上，等真有公开分发需求再上（见 Revisit），避免重蹈"过早工程化"。

## 优点（Pros）
- 单一真源，版本清晰，升级/回滚可控。
- 基础设施成本近乎为零，纯 Git。
- 本地 `file:` 联调兼顾"边做游戏边打磨框架"的迭代速度。

## 缺点 / 代价（Cons）
- UniTask 只能作手动前置依赖，消费端需先自行安装。
- Git URL 包在 `PackageCache` 只读，误在其中改动会在升级时丢失（playbook 已给纪律与补救）。
- 抽取时必须严守 `.meta`/GUID 保全，否则场景/Prefab 引用会断。

## 未来什么时候可能修改（Revisit）
- 需要公开给社区、或希望更省心的检索/版本管理 → 迁移到 **OpenUPM** 或私有 registry。
- 依赖复杂到必须由包管理器解析（而非手动前置）→ 重新评估分发形态。

## 影响模块（Impact）
仓库结构、打包与分发流程、消费端 `manifest.json`；**不影响运行时代码**。

## 取代关系
本 ADR **取代 ADR-003**。ADR-003 保留作历史，其状态标注为「已被 ADR-008 取代」。

## 日期
2026-07-05
