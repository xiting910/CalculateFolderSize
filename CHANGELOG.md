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
- **子项明细查询**: `IFolderSizeCalculator` 新增 `TryGetFolderChildren`, 从捕获的子项缓存中查询指定文件夹的直接子项列表, 为 UI 项目逐层下钻浏览提供数据基础
- **子项模型**: 新增 `FolderChild` 抽象基类及 `FileChild` / `DirectoryChild` 子类, 以类型区分文件与文件夹, 取代 IsDirectory 标志
- **子项捕获配置**: `CoreOptions` 新增 `CaptureChildren` 配置项, 扫描时按需捕获子项明细, 默认关闭保持 CLI 零额外开销
- **目录条目**: 重新引入 `DirectoryEntry` 模型 (携带目录名称), `EnumerateDirectories` 返回 `IEnumerable<DirectoryEntry>`
- **结构化日志**: 基于 `LoggerMessage` 源生成器的日志事件定义 (扫描开始/完成、缓存命中、目录计算、文件与子目录错误、取消、缓存清理), `FolderSizeCalculator` 注入 `ILogger<FolderSizeCalculator>`, 事件定义以独立分部类维护
- **CLI 日志基础设施**: `Program` 注册 `AddLogging`, 默认无 provider 静默运行, 由消费方配置输出
- **日志测试**: 新增记录型 `RecordingLogger` 断言日志事件, 覆盖全部可稳定触发的日志事件
- **UI.Shared 依赖**: `CalculateFolderSize.UI.Shared` 新增 `Avalonia.Controls.DataGrid` 与 `Microsoft.Extensions.Configuration.Json` 包引用, `Directory.Packages.props` 同步集中管理 DataGrid 版本 (12.1.0)
- **UI 桌面应用实现**: `CalculateFolderSize.UI.Shared` 由脚手架升级为完整 MVVM 应用, 新增主窗口/结果窗口/设置窗口及其 ViewModel、服务与视图, 提供 `AddUIShared` DI 注册与 ViewLocator 视图定位
  - 主窗口: 待计算路径列表 (输入框添加/系统文件夹选择器/覆盖选中/删除/清空), 历史记录面板 (持久化, 支持多选加入输入列表/删除/清空), 任务列表 DataGrid (路径/状态/已扫文件夹/文件数/大小/速度/耗时/开始时间, 全部列可排序), 运行中任务显示取消按钮, 双击已完成任务打开结果窗口, 缓存条目数实时刷新与一键清理 (Toast 反馈, 不弹窗)
  - 结果窗口: 基于子项缓存的逐层下钻浏览, 面包屑导航 (返回/跳转任意层级), 名称 (文件夹恒在文件前)/大小/百分比/文件夹数/文件数/错误列与排序, 双击子文件夹进入、双击文件以系统默认方式打开, 可打开当前文件夹到资源管理器
  - 设置窗口: 计算设置 (小数位数/并行度/捕获子项) 与 UI 配置 (进度节流间隔/日志级别) 保存后重启生效; 主题切换 (跟随系统/浅色/深色) 即时生效并持久化; 日志文件夹打开 (Android 端经 SAF 导出); 关于面板 (产品/版本/作者/许可证/GitHub)
- **历史记录存储**: 新增 `IHistoriesStore` / `HistoriesStore`, 扫描路径持久化到 AppData 下 `histories.txt`, 按路径比较器去重, 最近路径在前
- **设置存储**: 新增 `ISettingsStore` / `SettingsStore`, Core/UI 配置持久化到 AppData 下 `settings.json`
- **进度节流**: 新增 `ICalculateProgress` / `CalculateProgress`, 按可配置间隔节流进度更新, 以 EMA 平滑扫描速度, 完成时上报最终速度
- **UI 配置模型**: 新增 `UIOptions` (日志级别/主题/进度节流间隔) 及 `CalculateTaskStatus` / `ThemeMode` 枚举, 附带中文描述与枚举转换器
- **Toast 提示**: 新增 `ToastViewModelBase` 与 `ToastView`, 右下角短暂提示 3 秒后自动消失
- **系统打开辅助**: 新增 `SystemOpener`, 按平台调用系统默认方式打开文件/文件夹 (Windows/macOS/Linux 系统命令, Android 经 Launcher)
- **桌面端文件日志**: `Program` 启用 NReco.Logging.File 文件日志, 启动时轮转日志 (latest.log 重命名为时间戳), 仅保留最近 5 个日志文件
- **程序集元数据**: `Directory.Build.props` 新增版本号 1.0.0 与 AssemblyMetadata (作者/GitHub 仓库/许可证/产品名), 供设置窗口"关于"面板展示
- **路径比较器序列化**: `StringComparerExtensions` 新增 `ToJsonString`, 供设置持久化路径比较器配置

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
- **可访问性调整**: `IFileSystem` / `FileEntry` 由 internal 改为 public, 为 Android 等平台注入自定义文件系统实现 (SAF) 预留扩展点
- **桌面文件系统**: `FileSystem` 重命名为 `DesktopFileSystem`, `AddCore` 新增 `isDesktop` 参数, 非桌面应用由调用方自行注册 `IFileSystem`
- **CoreOptions 参数**: 位置参数调整为 `(DecimalPlaces, MaxDegreeOfParallelism, CaptureChildren, PathComparer)`
- **测试适配**: 新增子项捕获与日志生命周期测试; 现有测试同步适配模型与构造签名变更; `FileSystemTests` 重命名为 `DesktopFileSystemTests`
- **安全缓存清理**: `IFolderSizeCalculator.ClearCache` 改为 `TryClearCache`, 返回布尔值指示是否成功清理; 有计算任务进行中时拒绝清理, 避免计算过程中清空缓存导致结果不准确
- **日志本地化与级别调整**: 日志消息由英文改为中文; `FileSizeFailed` / `SubDirectoryFailed` / `ChildrenCacheFailed` 事件级别由 Debug 提升为 Information, 便于消费方识别问题; `DirectoryCalculated` 事件现覆盖根目录自身的扫描结果
- **CLI 缓存清理反馈**: `clearcache` 命令适配 `TryClearCache`, 清理被拒绝时输出红色失败提示
- **测试适配**: 新增计算进行中清理缓存被拒绝的并发测试 (含日志断言与对象释放断言); 现有测试适配 API 重命名与日志事件编号调整
- **路径比较器选项收紧**: `GetPathComparer` 仅保留 `Ordinal` / `OrdinalIgnoreCase` 两个选项, 移除 `CurrentCulture` / `InvariantCulture` 等区域相关比较器, 避免同一路径在不同区域设置下被识别为不同路径
- **UI.Shared 依赖调整**: 移除 `Microsoft.Extensions.Configuration.Json` 包引用, 配置加载职责移至桌面端入口
- **桌面端配置与 DI**: `Program` 从 AppData 下 `settings.json` 读取配置并构建服务容器 (Logging/Core/UI.Shared), `OutputType` 由 Exe 改为 WinExe
- **依赖版本**: `Avalonia.Controls.DataGrid` 由 12.1.0 升级至 12.1.2; 新增 NReco.Logging.File 1.4.0 集中管理
- **CLI 依赖清理**: 移除 `Microsoft.Extensions.Configuration` 冗余包引用
- **日志事件格式整理**: `FolderSizeCalculator.Logging.cs` 的 `[LoggerMessage]` 特性参数改为多行书写 (纯格式调整, 无行为变化)

### Removed

- `DirectoryEntry` 模型 — 子目录枚举直接使用 `string` 类型
- `CliOptions.YesString` 配置项 — 用户确认改用 `char.TryParse` 直接检测 `y`/`Y` 字符
- `App.WaitForKeyPress()` 静态方法 — 替换为实例常量 `WaitForKeyMessage` 配合 Spectre.Console 按键输入
- `App.ParsePaths` 私有方法 — 解析逻辑移至 `UserInputProcessor` 服务
- `SmokeTest.cs` — 被覆盖真实场景的 CLI 层单元测试替代
- `InternalsVisibleTo("DynamicProxyGenAssembly2")` — `IFileSystem` 公开后 Moq 可直接模拟 public 接口, 不再需要

### Fixed

- **Windows 路径大小写重复计算**: 输入 D 与 d 时同一盘符被计算两次, 缓存与路径锁改用平台感知的 PathComparer (Windows 大小写不敏感, 其余平台大小写敏感)
- **嵌套目录错误未汇总**: 子目录内部产生的访问错误未合并到父级 `ErrorPaths`, 导致 CLI 错误汇总遗漏深层错误; 现子目录结果中的错误在递归时逐级合并至根节点, 并新增三层嵌套错误的回归测试

---

[Unreleased]: https://github.com/xiting910/CalculateFolderSize/commits/main
