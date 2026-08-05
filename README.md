# CalculateFolderSize

一个跨平台的文件夹大小计算工具, 支持 CLI、桌面 (Avalonia UI) 和 Android 三个平台, 基于 .NET 10.0 构建.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![CI](https://github.com/xiting910/CalculateFolderSize/actions/workflows/ci.yml/badge.svg)](https://github.com/xiting910/CalculateFolderSize/actions/workflows/ci.yml)
[![CodeQL](https://github.com/xiting910/CalculateFolderSize/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/xiting910/CalculateFolderSize/actions/workflows/codeql-analysis.yml)
[![Dependency Review](https://github.com/xiting910/CalculateFolderSize/actions/workflows/dependency-review.yml/badge.svg)](https://github.com/xiting910/CalculateFolderSize/actions/workflows/dependency-review.yml)

## 目录

- [CalculateFolderSize](#calculatefoldersize)
  - [目录](#目录)
  - [功能特性](#功能特性)
  - [项目结构](#项目结构)
  - [快速开始](#快速开始)
    - [前置要求](#前置要求)
    - [克隆并构建](#克隆并构建)
    - [运行 CLI 工具](#运行-cli-工具)
    - [运行桌面应用](#运行桌面应用)
    - [构建 Android 应用](#构建-android-应用)
    - [运行测试](#运行测试)
  - [配置](#配置)
    - [桌面应用配置](#桌面应用配置)
  - [许可证](#许可证)

## 功能特性

- **快速并行计算**: 使用同步递归 + 并行子目录算法扫描文件夹, 计算总大小、文件数、文件夹数和磁盘占用
- **并发安全**: 基于 `SemaphoreSlim` 的 per-path 锁机制, 避免并发重复计算同一路径
- **结果缓存**: 自动缓存已计算的文件夹结果, 避免重复计算, 支持手动清除缓存 (计算进行中会拒绝清理, 避免结果不准确)
- **子项明细查询**: 可选捕获每个文件夹的直接子项 (文件与子文件夹及其大小), 通过 `TryGetFolderChildren` 查询, 为桌面与 Android 的逐层下钻浏览提供数据基础
- **结构化日志**: 基于 `LoggerMessage` 源生成器的日志事件 (扫描、缓存、错误、取消、清理等), 默认静默, 由消费方 (CLI/UI) 配置输出
- **跨平台支持**: 统一文件系统实现, 跨 Windows、macOS、Linux 和 Android 平台工作
- **多平台 UI**:
  - **CLI**: 基于 Spectre.Console 的交互式命令行工具, 支持多路径并发计算, 彩色输出和对齐显示
  - **桌面 (Desktop)**: 基于 Avalonia UI 的桌面应用, 单窗口壳架构 (主视图/结果视图栈/设置抽屉/目录选择器覆盖层), 支持多任务并行计算 (实时进度/速度/耗时, 任务列可排序), 结果视图逐层下钻浏览, 历史记录与设置持久化, 文件日志自动轮转
  - **Android**: 基于 Avalonia UI 的 Android 应用, 完整入口 (服务容器注入/文件日志/启动画面), 全部文件访问权限引导与内置目录选择器, 与桌面端共用壳视图架构, 发布产物为正式签名 APK
- **可读的文件大小**: 自动将字节数转换为 B / KB / MB / GB / TB / PB / EB 等单位
- **错误容忍**: 遇到无权访问的目录或文件时继续扫描, 计算完成后汇总显示错误信息

## 项目结构

```
CalculateFolderSize/
├── .github/
│   └── workflows/                          # CI/CD 工作流 (CI, CodeQL, 依赖审查, 发布等)
├── srcs/
│   ├── CalculateFolderSize.Core/           # 核心计算库
│   │   ├── Interfaces/                     # 核心接口定义
│   │   │   ├── IFileSizeFormatter.cs       # 文件大小格式化器接口
│   │   │   ├── IFileSystem.cs              # 文件系统抽象接口
│   │   │   └── IFolderSizeCalculator.cs    # 文件夹大小计算器接口
│   │   ├── Models/                         # 数据模型
│   │   │   ├── CoreOptions.cs              # 核心配置选项
│   │   │   ├── DirectoryChild.cs           # 文件夹子项
│   │   │   ├── DirectoryEntry.cs           # 目录条目
│   │   │   ├── FileChild.cs                # 文件子项
│   │   │   ├── FileEntry.cs                # 文件条目
│   │   │   ├── FolderChild.cs              # 文件夹子项抽象基类
│   │   │   ├── FolderSize.cs               # 文件夹大小结果
│   │   │   └── ProgressReport.cs           # 进度报告
│   │   ├── Services/                       # 服务实现
│   │   │   ├── FileSystem.cs               # 文件系统实现, 跳过重解析点并逐文件捕获异常
│   │   │   ├── FileSizeFormatter.cs        # 文件大小格式化器
│   │   │   ├── FolderSizeCalculator.cs     # 文件夹大小计算器 (并行递归 + 缓存 + 并发锁 + 进度报告 + IDisposable)
│   │   │   └── FolderSizeCalculator.Logging.cs # 日志事件定义 (LoggerMessage 源生成器)
│   │   ├── IServiceCollectionExtensions.cs # DI 注册扩展
│   │   └── StringComparerExtensions.cs     # 路径比较器扩展
│   ├── CalculateFolderSize.Cli/            # 命令行工具 (基于 Spectre.Console)
│   │   ├── App.cs                          # 应用程序主循环
│   │   ├── CliOptions.cs                   # CLI 配置选项
│   │   ├── IPathNormalizer.cs              # 路径标准化接口
│   │   ├── IUserInputProcessor.cs          # 用户输入处理接口
│   │   ├── PathNormalizer.cs               # 路径标准化实现
│   │   ├── UserInputProcessor.cs           # 用户输入处理实现
│   │   └── Program.cs                      # 程序入口点
│   ├── CalculateFolderSize.UI.Shared/      # UI 共享代码 (基于 Avalonia UI 的 MVVM 应用)
│   │   ├── Assets/                         # 应用图标 (logo.ico 窗口/exe, Icon.png Android)
│   │   ├── App.axaml(.cs)                  # 应用程序类, 创建壳窗口/壳视图并应用已保存的主题
│   │   ├── Constants.cs                    # 常量 (数据/日志目录, 限制范围, 刷新间隔)
│   │   ├── EnumDescriptionConverter.cs     # 枚举描述文本转换器
│   │   ├── EnumExtensions.cs               # 枚举扩展 (获取中文描述)
│   │   ├── ITopLevelProvider.cs            # 顶层视图提供器接口
│   │   ├── IServiceCollectionExtensions.cs # UI 共享层 DI 注册扩展 (AddUIShared)
│   │   ├── TopLevelProvider.cs             # 顶层视图提供器实现
│   │   ├── SystemOpener.cs                 # 跨平台系统默认方式打开文件/文件夹
│   │   ├── ViewLocator.cs                  # ViewModel 到 View 的视图定位器
│   │   ├── Interfaces/                     # 服务接口
│   │   │   ├── ICalculateProgress.cs       # 计算进度接口
│   │   │   ├── IHistoriesStore.cs          # 历史记录存储接口
│   │   │   ├── ISettingsStore.cs           # 设置存储接口
│   │   │   └── IStorageAccessService.cs    # 存储访问权限服务接口
│   │   ├── Models/                         # 数据模型
│   │   │   ├── CalculateProgressUpdateEventArgs.cs # 进度更新事件参数
│   │   │   ├── CalculateTaskStatus.cs      # 计算任务状态枚举
│   │   │   ├── ThemeMode.cs                # 主题模式枚举
│   │   │   └── UIOptions.cs                # UI 层配置选项
│   │   ├── Services/                       # 服务实现
│   │   │   ├── CalculateProgress.cs        # 计算进度 (节流 + EMA 平滑速度)
│   │   │   ├── HistoriesStore.cs           # 历史记录存储 (AppData/histories.txt)
│   │   │   └── SettingsStore.cs            # 设置存储 (AppData/settings.json)
│   │   ├── ViewModels/                     # 视图模型 (CommunityToolkit.Mvvm)
│   │   │   ├── BreadcrumbItemViewModel.cs  # 结果视图面包屑条目
│   │   │   ├── CalculateTaskViewModel.cs   # 单个计算任务 (执行/进度/状态/取消)
│   │   │   ├── DirectoryPickerViewModel.cs # 目录选择器 (安卓端浏览共享存储)
│   │   │   ├── MainViewModel.cs            # 主视图 (输入列表/历史/任务列表/缓存/权限横幅)
│   │   │   ├── ResultItemViewModel.cs      # 结果视图子项
│   │   │   ├── ResultViewModel.cs          # 结果视图 (下钻浏览/面包屑导航/排序)
│   │   │   ├── SettingsViewModel.cs        # 设置视图 (配置/主题/日志/关于)
│   │   │   ├── ShellViewModel.cs           # 壳视图模型 (主视图/结果栈/设置抽屉/目录选择器)
│   │   │   └── ToastViewModel.cs           # 全局 Toast 短暂提示
│   │   └── Views/                          # 视图 (Avalonia XAML)
│   │       ├── DirectoryPickerView.axaml(.cs) # 目录选择视图 (安卓端)
│   │       ├── MainView.axaml(.cs)        # 主视图
│   │       ├── ResultView.axaml(.cs)      # 结果视图
│   │       ├── SettingsView.axaml(.cs)    # 设置视图
│   │       ├── ShellView.axaml(.cs)       # 壳视图 (主视图与各覆盖层容器)
│   │       ├── ShellWindow.axaml(.cs)     # 桌面端壳窗口
│   │       └── ToastView.axaml(.cs)       # 右下角全局 Toast 提示
│   ├── CalculateFolderSize.UI.Desktop/     # 桌面应用入口 (服务容器与配置加载, 文件日志与轮转, Windows 兼容清单)
│   │   ├── DesktopStorageAccessService.cs  # 桌面存储访问服务 (恒已授权)
│   │   ├── Program.cs                      # 程序入口点 (配置加载/服务容器/文件日志与轮转)
│   │   └── app.manifest                    # Windows 应用清单 (supportedOS Windows 10)
│   └── CalculateFolderSize.UI.Android/     # Android 应用入口 (MainActivity/MainApplication/清单/启动画面资源)
│       ├── MainActivity.cs                 # Activity 入口 (AvaloniaMainActivity)
│       ├── MainApplication.cs              # 应用类 (服务容器注入与文件日志, AvaloniaAndroidApplication<App>)
│       ├── StorageAccessService.cs         # 存储访问服务 (全部文件访问权限检查与授权引导)
│       ├── Properties/AndroidManifest.xml  # 应用清单 (全部文件访问权限/应用名/图标)
│       └── Resources/                      # Android 资源 (启动画面/主题/颜色/动画)
├── tests/
│   ├── CalculateFolderSize.Core.Tests/     # Core 层单元测试 (xunit.v3 + Moq, 覆盖 Calculator/子项查询/日志/FileSystem/Formatter/Options)
│   ├── CalculateFolderSize.Cli.Tests/      # CLI 层单元测试 (xunit.v3 + Moq, 覆盖 App/输入解析/路径标准化/配置)
│   └── CalculateFolderSize.UI.Shared.Tests/ # UI.Shared 层单元测试 (xunit.v3 + Moq, 覆盖 UIOptions/进度节流/历史记录/设置存储/枚举扩展)
├── Directory.Build.props                   # 共享 MSBuild 属性
├── Directory.Packages.props                # NuGet 依赖集中管理 (CPM)
├── CalculateFolderSize.slnx                # 解决方案文件
└── 其他配置文件 (.editorconfig, .gitattributes, .gitignore, CHANGELOG.md, LICENSE, ReBuild.bat 等)
```

## 快速开始

### 前置要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 如需构建 Android 应用, 请安装 Android 工作负载与本地 Android 构建环境 (JDK 与 Android SDK 需自行安装):
  ```bash
  dotnet workload install android
  ```

### 克隆并构建

```bash
git clone https://github.com/xiting910/CalculateFolderSize.git
cd CalculateFolderSize
dotnet build
```

也可以使用根目录下的 `ReBuild.bat` 脚本, 自动清理所有 `bin` / `obj` / `publish` 目录, 执行还原、构建并发布三个平台到根目录 `publish` 文件夹.

### 运行 CLI 工具

```bash
dotnet run --project srcs/CalculateFolderSize.Cli
```

### 运行桌面应用

```bash
dotnet run --project srcs/CalculateFolderSize.UI.Desktop
```

### 构建 Android 应用

```bash
dotnet publish srcs/CalculateFolderSize.UI.Android -c Release
```

### 运行测试

```bash
dotnet test
```

## 配置

CLI 工具支持通过 `appsettings.json` 配置:

```json
{
  "Core": {
    "MaxDegreeOfParallelism": 16,
    "DecimalPlaces": 2,
    "CaptureChildren": false,
    "PathComparer": "OrdinalIgnoreCase"
  },
  "Cli": {
    "SizeStringLength": 12,
    "DirectorySeparator": "\\",
    "ReplacedSeparator": "/",
    "ExitCommand": "exit",
    "ClearCacheCommand": "clearcache"
  }
}
```

| 配置项                           | 说明                    | 默认值         |
| -------------------------------- | ---------------------- | -------------- |
| `Core.MaxDegreeOfParallelism`    | 最大并行度              | CPU 核心数 x 2 |
| `Core.DecimalPlaces`             | 文件大小小数位数         | 2              |
| `Core.CaptureChildren`           | 是否捕获子项明细, 供 UI 逐层下钻查询 | `false` |
| `Core.PathComparer`               | 路径比较器              | 按平台: Windows 为 OrdinalIgnoreCase, 其余为 Ordinal |
| `Cli.SizeStringLength`           | 文件大小字符串对齐长度   | 9              |
| `Cli.DirectorySeparator`         | 目标目录分隔符          | `\`            |
| `Cli.ReplacedSeparator`          | 被替换的目录分隔符      | `/`            |
| `Cli.ExitCommand`                | 退出命令               | `exit`         |
| `Cli.ClearCacheCommand`          | 清除缓存命令            | `clearcache`   |

### 桌面应用配置

桌面应用从 `%APPDATA%/CalculateFolderSize/settings.json` 读取配置, 可在应用内"设置"抽屉中修改. `Core` 配置项含义与 CLI 相同, `UI` 配置项如下:

| 配置项                          | 说明                             | 默认值         |
| ------------------------------- | -------------------------------- | -------------- |
| `UI.Level`                      | 日志级别 (None 表示不写日志文件)   | `Information`  |
| `UI.Theme`                      | 主题模式 (System / Light / Dark) | `System`       |
| `UI.ThrottleIntervalMilliseconds` | 进度节流间隔 (毫秒)              | `200`          |
| `UI.ToastDurationSeconds`       | Toast 提示显示时间 (秒)           | `3`            |

日志文件写入 `%APPDATA%/CalculateFolderSize/logs/`, 每次启动时轮转, 仅保留最近 5 个日志文件.

## 许可证

[MIT License](LICENSE)

Copyright (c) 2026 xiting910
