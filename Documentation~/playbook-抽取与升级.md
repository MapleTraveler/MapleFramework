# Playbook：把框架抽成独立仓库 + 日后升级/同步

本文回答两件事：
1. **抽取**：怎么把 `MapleFramework/` 从本工程干净地抽成独立 Git 仓库、以 UPM Git URL 分发。
2. **升级/同步**：框架以后要改，怎么改、怎么让各个项目拿到新版、以及在项目里顺手改了框架怎么回灌。

> 头号纪律（贯穿全文）：**永远连同 `.meta` 一起搬运，绝不重新生成**。`.meta` 里存着每个脚本/资源的 GUID，场景与 Prefab 靠 GUID 引用 `GameRoot`、`WindowController` 等脚本。GUID 一变，所有引用变 `Missing`。Git 天然保留 `.meta`，只要别手动删。

---

## 零、抽取前提认知（为什么能平滑抽）

- asmdef 之间靠**程序集名**（`Maple.Core` / `Maple.Framework` …）互相引用，与文件在 `Assets/` 还是 `Packages/` **无关**。所以框架从 `Assets` 挪到 package 后，本工程里 `GameLogic` / `FrameworkDemo` 对 `Maple.*` 的引用**照常解析**，不会断。
- 脚本引用靠 GUID（存在 `.meta`）。只要 `.meta` 跟着走，场景里挂的 `GameRoot` 等组件不会丢。
- 因此「抽取」本质是：**把 `MapleFramework/` 连 `.meta` 整体搬到一个新仓库，再让本工程改从 package 引用它**。

---

## 一、抽取步骤（一次性）

### 1. 建独立仓库并放入框架
```
MapleFramework/                 ← 新仓库根 = 本工程 MapleFramework 文件夹的内容
├── package.json                ← 已就位
├── README.md / CHANGELOG.md    ← 已就位
├── LICENSE.md                  ← 新增（建议 MIT）
├── .gitignore                  ← 新增（见下）
├── Core/  UI/  Framework/      ← 各层源码 + asmdef + .meta
├── ADR/  *总纲*.md  *进度*.md  ← 文档（见「文档要不要进包」）
└── ...
```
把本工程 `Assets/Scripts/MapleFramework/` 下**全部内容（含所有 `.meta`）**复制到新仓库根目录。

`.gitignore` 最少写：
```
.DS_Store
Thumbs.db
```
（框架仓库本身不是 Unity 工程，不需要忽略 `Library/` 等；除非你为它单独建了个测试工程。）

### 2. 打首个版本
```bash
git init
git add .
git commit -m "chore: MapleFramework 0.4.0 首个可分发版本"
git tag v0.4.0
git remote add origin https://github.com/<your-org>/MapleFramework.git
git push -u origin main --tags
```
> 约定：`package.json` 的 `version` 与 tag `vX.Y.Z` **始终一致**。改版本必打 tag。

### 3. 让本工程改从 package 引用
1. 先确保本工程装了 **UniTask**（Git URL，见 README 安装节）。
2. 删除本工程内的 `Assets/Scripts/MapleFramework/`（源码已在新仓库）。
3. 在本工程 `Packages/manifest.json` 加一行（二选一）：
   - 发布消费（锁版本）：
     ```json
     "com.maple.framework": "https://github.com/<your-org>/MapleFramework.git#v0.4.0"
     ```
   - 本地联调（见第三节 Model A）：
     ```json
     "com.maple.framework": "file:../../MapleFramework"
     ```
4. 回 Unity 等待重新导入。**验证**：打开原来挂了 `GameRoot` 的场景，确认组件没变 `Missing`、能正常运行。

### 文档要不要进包
- 保持现状（`README`/`CHANGELOG`/`ADR`/`总纲`/`进度`/指南 都在包里）完全可用，只是会各自生成 `.meta`。
- 想让 Unity **不导入**文档（更干净）：把它们放进 `Documentation~/` 文件夹（后缀 `~` 让 Unity 忽略，无需 `.meta`）。`README.md`/`CHANGELOG.md`/`package.json` 建议留在包根（Package Manager 面板会识别）。
- Demo（`FrameworkDemo`）**不要**进包；它属于示例，将来要随包分发可另立 `Samples~/`。

---

## 二、版本号怎么定（SemVer）

`0.x` 阶段：
- **Patch（0.4.0→0.4.1）**：修 bug、补文档，无 API 变化。
- **Minor（0.4.0→0.5.0）**：加功能、或有小的破坏性改动（0.x 允许）。
- 到 API 稳定、想承诺兼容时再上 **1.0.0**，此后破坏性改动才必须进 major。

每次发版固定动作：改代码 → 更新 `CHANGELOG.md` → 改 `package.json` `version` → commit → `git tag vX.Y.Z` → push tags。

---

## 三、日后怎么改框架、怎么同步（关键）

### Model A（推荐）：框架仓库为唯一真源，本地路径联调
适合你这种"一边做游戏、一边打磨框架"的节奏。

1. 把框架仓库 clone 到本机，比如与工程平级：
   ```
   D:\GameWorks\MapleFramework\           ← 框架仓库
   D:\GameWorks\UnityProjects\BattleAgentProject\   ← 消费工程
   ```
2. 消费工程 `manifest.json` 用**本地路径**引用：
   ```json
   "com.maple.framework": "file:../../../MapleFramework"
   ```
   （`file:` 路径相对于工程的 `Packages/` 文件夹，按实际层级数 `..`。）
3. 这样在 Unity 里**改框架源码会实时生效**（本地 package 可编辑），但改动记录进的是**框架仓库**的 git 历史，不混进游戏工程。
4. 一段工作告一段落：在框架仓库 `commit` → 更新 CHANGELOG/版本 → `git tag` → push。
5. 要发布 / 交给别的项目时，把该项目的引用从 `file:` 换成 `git URL#vX.Y.Z`。

> 注意：`file:` 本地 package 是"可写"的；`git URL` 拉下来的 package 在 `Library/PackageCache` 里是**只读**的，别在那儿改（改了不会进版本库，升级即丢）。

### Model B：直接消费 Git URL，改动走 PR/提交
1. 平时项目只用 `git URL#tag`，package 只读。
2. 要改框架：去框架仓库改 → 提交 → 打新 tag → 回项目把 `manifest.json` 的 `#vX.Y.Z` 升上去（或 Package Manager 里 Update）。
3. 干净、边界清楚，但每次改框架都要切仓库，联调不如 Model A 顺手。

### 如果你在只读 package 里手滑改了代码怎么办
`Library/PackageCache` 里的改动升级后会丢。补救：把改动 `diff` 出来，回框架仓库重新落一遍、提交、发版。所以**养成在真源（框架仓库 / 本地 `file:` package）里改**的习惯。

---

## 四、升级消费端（拿新版）

- 锁 tag 的项目：把 `manifest.json` 里 `#vX.Y.Z` 改成新 tag，回 Unity 重新解析；或 Package Manager → 该包 → Update。
- 升级后务必：看 `CHANGELOG` 的 **Removed / Changed** 有没有破坏性改动；跑一遍关键场景验证。
- Git URL 的包，Unity 会把解析到的 commit 记进 `Packages/packages-lock.json`；想强制重取可删对应锁项或改 tag。

---

## 五、抽取质量自查清单

- [ ] `MapleFramework/` 全部 `.meta` 都进了新仓库（用 `git status` 确认没漏 `.meta`）。
- [ ] `package.json` 在包根，`version` 与 tag 一致。
- [ ] 消费工程已装 UniTask；Newtonsoft 随 `package.json` 依赖到位。
- [ ] 删掉工程内旧 `Assets/Scripts/MapleFramework` 后，挂 `GameRoot` 的场景无 `Missing` 脚本。
- [ ] 编译无 `Maple.*` 程序集找不到的报错。
- [ ] `Demo`、游戏 `GameLogic` 运行正常。
- [ ] 加了 `LICENSE.md`（建议 MIT）。
