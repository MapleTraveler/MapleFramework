# ADR-003：框架分发策略 —— 现阶段靠 asmdef 工程内复用，UPM / 开源延后

> **状态：已被 [ADR-008](ADR-008-distribution-upm-git.md) 取代（2026-07-05）。** 本文所列 Revisit 条件已满足，分发策略改为「独立 Git 仓库 + UPM Git URL」。以下内容保留作历史决策快照。

## 背景（Context）
框架希望可复用、未来可能开源。曾计划立即把框架抽成独立 UPM 包（原 Phase 0 的 Step 0c）。在推演具体步骤时，发现操作"草台班子"感很强、坑多收益低。

## 问题（Problem）
现在就把框架抽成独立仓库 + `package.json` 做成 UPM 包，还是延后？

## 可选方案（Options）
- **A. 立即抽独立仓库 + `package.json` + UPM 分发**。
- **B. 现阶段靠 asmdef 在工程内复用，UPM / 开源延后** —— 采用。
- **C. 导出 `.unitypackage`**：一次性快照，无版本与依赖管理能力。

## 最终方案（Decision）
采用 **B**。当前用 asmdef 实现模块隔离与工程内复用；UPM 化与开源**延后**到框架内容足够丰富、API 趋于稳定后再做。

## 为什么（Rationale）
"这步怎么这么草台班子"的直觉是对的。现在做 UPM 包要面对：手工搬运文件、维护 MonoBehaviour 的 GUID 不丢失、UniTask 经 Git URL 安装无法作为 `package.json` 的直接依赖声明（只能作为手动前置依赖）、包版本 / CHANGELOG / LICENSE 等一整套工程开销。而这些此刻**不带来任何功能收益**——asmdef 已满足本地复用。过早 UPM 化是为形式牺牲迭代速度。

## Pros
- 保持单仓库快速迭代，重构无包边界阻力。
- 避免过早处理 GUID / 包依赖 / 版本治理等高成本低收益的事。

## Cons
- 暂不能被外部项目通过 UPM（Git URL / OpenUPM）直接引用。
- 跨项目复用目前需手工拷贝目录。

## 未来什么时候可能修改（Revisit）
当满足以下条件时执行 UPM 化 / 开源：
- 模块数量与质量达标、公开 API 趋于稳定；
- 有明确的外部使用者或开源计划。
执行清单：独立 GitHub 仓库 → 各层映射为包 + `package.json` → `README` / `CHANGELOG` / `LICENSE(MIT)` → OpenUPM 提交；并在文档中明确 UniTask 为手动前置依赖。

## 影响模块（Impact）
仓库结构、构建与分发流程；**不影响运行时代码**。

## 日期
2026-06-29（记录）
