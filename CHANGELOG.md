# Changelog

本文件记录了项目的所有重要变更。每个版本的变更都应在发布时记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/),
版本号遵循 [Semantic Versioning](https://semver.org/lang/zh-CN/).

---

## [Unreleased]

### Added

- 初始化项目: 建立基于 .NET 10.0 的跨平台文件夹大小计算工具
- 项目结构:
  - `CalculateFolderSize.Core` — 核心计算库, 含接口 (IFileSystem / IFolderSizeCalculator / IFileSizeFormatter)、模型 (FolderSize / FileEntry / DirectoryEntry / ProgressReport / CoreOptions) 和服务实现 (FolderSizeCalculator / FileSystem / FileSizeFormatter)
  - `CalculateFolderSize.Cli` — 基于 Spectre.Console 的交互式命令行工具, 支持多路径并发计算、结果缓存、路径标准化和彩色输出
  - `CalculateFolderSize.UI.Shared` / `.UI.Desktop` / `.UI.Android` — 基于 Avalonia UI 的跨平台 UI 脚手架
  - `CalculateFolderSize.Core.Tests` / `.Cli.Tests` — 基于 xunit.v3 + Moq 的单元测试
- CI/CD: GitHub Actions 工作流 (CI / CodeQL / 依赖审查 / NuGet 发布与删除)
- 项目配置: 集中包管理 (CPM)、可空引用类型、XML 文档生成、.editorconfig
- CLI 输入处理: 新增 `IUserInputProcessor` 接口与 `UserInputProcessor` 实现, 将多路径解析逻辑从 App 中提取为可单元测试的独立服务
- CLI 层单元测试: 新增 `AppTests` / `UserInputProcessorTests` / `PathNormalizerTests` / `CliOptionsTests`, 覆盖主循环交互、路径解析、路径标准化与配置加载
- CoreOptions.PathComparer 配置项: 支持通过配置指定路径比较器 (Ordinal / OrdinalIgnoreCase 等), 默认按平台选择 (Windows 大小写不敏感, 其余平台大小写敏感)
- Core 层单元测试: 新增 PathComparer 配置映射与回退用例, 新增大小写变体路径的缓存共享/独立计算回归用例

### Changed

- **API 同步化**: `IFolderSizeCalculator.GetFromFolderAsync` 重命名为 `GetFromFolder`, 由异步改为同步方法, 调用方改用 `Task.Run` 实现并行执行
- **并发安全**: `FolderSizeCalculator` 新增 per-path `SemaphoreSlim` 锁机制, 避免同时计算同一路径, 提升多线程安全性与性能
- **生命周期管理**: `IFolderSizeCalculator` 继承 `IDisposable`, `FolderSizeCalculator` 实现资源清理 (释放所有 `SemaphoreSlim`, 清空缓存与锁字典)
- **模型简化**: 移除 `DirectoryEntry` 模型, `EnumerateDirectories` 直接返回 `IEnumerable<string>` 减少不必要的对象分配
- **可访问性收紧**: `IFileSystem` 和 `FileEntry` 改为 `internal`, 仅暴露 `IFolderSizeCalculator` (public) 作为对外 API
- **Moq 支持**: 添加 `InternalsVisibleTo DynamicProxyGenAssembly2` 允许 Moq 模拟 internal 接口
- **CLI 输入**: 使用 Spectre.Console 的 `TextPrompt` 和 `Input.ReadKey` 替代 `Console.ReadLine/ReadKey`, 统一控制台交互风格
- **空值注解**: `IFileSystem.DirectoryExists` 参数添加 `[NotNullWhen(true)]` 注解, 提升可空引用类型分析精度
- **输入解析重构**: `App` 改用注入的 `IUserInputProcessor.ParsePaths` 处理用户输入, 移除内联解析逻辑
- **状态复用**: `paths` / `validResults` / `tasks` 列表与 `Stopwatch` 移出主循环, 每次迭代前重置, 减少重复分配
- **CLI 细节**: 输入提示符新增 `>` 前缀; 访问错误汇总新增错误总数显示; 结果括号颜色由 white 调整为 blue
- **CoreOptions API**: 新增 PathComparer 位置参数, 构造签名由 (int, int) 变更为 (int, int, StringComparer)
- **枚举开销优化**: `FileSystem.EnumerateFiles` / `EnumerateDirectories` 移除冗余的目录存在性检查, 该方法为 Core 层内部调用, 已在上层保证目录存在性, 减少重复系统调用
- **CLI 输出**: 计算完成提示由 "计算完成" 改为 "全部计算完成", 与多路径并发计算的语义一致
- **文档修正**: `CliOptions.Create` 的 XML 文档中 `</param>>` 笔误修正为 `</param>`

### Removed

- `DirectoryEntry` 模型 — 子目录枚举直接使用 `string` 类型
- `CliOptions.YesString` 配置项 — 用户确认改用 `char.TryParse` 直接检测 `y`/`Y` 字符
- `App.WaitForKeyPress()` 静态方法 — 替换为实例常量 `WaitForKeyMessage` 配合 Spectre.Console 按键输入
- `App.ParsePaths` 私有方法 — 解析逻辑移至 `UserInputProcessor` 服务
- `SmokeTest.cs` — 被覆盖真实场景的 CLI 层单元测试替代

### Fixed

- **Windows 路径大小写重复计算**: 输入 D 与 d 时同一盘符被计算两次, 缓存与路径锁改用平台感知的 PathComparer (Windows 大小写不敏感, 其余平台大小写敏感)
- **嵌套目录错误未汇总**: 子目录内部产生的访问错误未合并到父级 `ErrorPaths`, 导致 CLI 错误汇总遗漏深层错误; 现子目录结果中的错误在递归时逐级合并至根节点, 并新增三层嵌套错误的回归测试

---

[Unreleased]: https://github.com/xiting910/CalculateFolderSize/commits/main
