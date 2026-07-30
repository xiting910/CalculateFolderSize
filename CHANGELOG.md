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

---

[Unreleased]: https://github.com/xiting910/CalculateFolderSize/commits/main
