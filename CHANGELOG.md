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

### Changed

- **API 同步化**: `IFolderSizeCalculator.GetFromFolderAsync` 重命名为 `GetFromFolder`, 由异步改为同步方法, 调用方改用 `Task.Run` 实现并行执行
- **并发安全**: `FolderSizeCalculator` 新增 per-path `SemaphoreSlim` 锁机制, 避免同时计算同一路径, 提升多线程安全性与性能
- **生命周期管理**: `IFolderSizeCalculator` 继承 `IDisposable`, `FolderSizeCalculator` 实现资源清理 (释放所有 `SemaphoreSlim`, 清空缓存与锁字典)
- **模型简化**: 移除 `DirectoryEntry` 模型, `EnumerateDirectories` 直接返回 `IEnumerable<string>` 减少不必要的对象分配
- **可访问性收紧**: `IFileSystem` 和 `FileEntry` 改为 `internal`, 仅暴露 `IFolderSizeCalculator` (public) 作为对外 API
- **Moq 支持**: 添加 `InternalsVisibleTo DynamicProxyGenAssembly2` 允许 Moq 模拟 internal 接口
- **CLI 输入**: 使用 Spectre.Console 的 `TextPrompt` 和 `Input.ReadKey` 替代 `Console.ReadLine/ReadKey`, 统一控制台交互风格
- **空值注解**: `IFileSystem.DirectoryExists` 参数添加 `[NotNullWhen(true)]` 注解, 提升可空引用类型分析精度

### Removed

- `DirectoryEntry` 模型 — 子目录枚举直接使用 `string` 类型
- `CliOptions.YesString` 配置项 — 用户确认改用 `char.TryParse` 直接检测 `y`/`Y` 字符
- `App.WaitForKeyPress()` 静态方法 — 替换为实例常量 `WaitForKeyMessage` 配合 Spectre.Console 按键输入

---

[Unreleased]: https://github.com/xiting910/CalculateFolderSize/commits/main
