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
  - [许可证](#许可证)

## 功能特性

- **快速并行计算**: 使用同步递归 + 并行子目录算法扫描文件夹, 计算总大小、文件数、文件夹数和磁盘占用
- **并发安全**: 基于 `SemaphoreSlim` 的 per-path 锁机制, 避免并发重复计算同一路径
- **结果缓存**: 自动缓存已计算的文件夹结果, 避免重复计算, 支持手动清除缓存
- **跨平台支持**: 统一文件系统实现, 跨 Windows、macOS、Linux 和 Android 平台工作
- **多平台 UI**:
  - **CLI**: 基于 Spectre.Console 的交互式命令行工具, 支持多路径并发计算, 彩色输出和对齐显示
  - **桌面 (Desktop)**: 基于 Avalonia UI 的桌面应用程序
  - **Android**: 基于 Avalonia UI 的 Android 应用程序
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
│   │   │   ├── IFileSystem.cs              # 文件系统抽象接口 (internal)
│   │   │   └── IFolderSizeCalculator.cs    # 文件夹大小计算器接口 (public, 继承 IDisposable)
│   │   ├── Models/                         # 数据模型
│   │   │   ├── CoreOptions.cs              # 核心配置选项
│   │   │   ├── FileEntry.cs                # 文件条目
│   │   │   ├── FolderSize.cs               # 文件夹大小结果
│   │   │   └── ProgressReport.cs           # 进度报告
│   │   ├── Services/                       # 服务实现
│   │   │   ├── FileSizeFormatter.cs        # 文件大小格式化器
│   │   │   ├── FileSystem.cs               # 统一文件系统实现, 跳过重解析点并逐文件捕获异常
│   │   │   └── FolderSizeCalculator.cs     # 文件夹大小计算器 (并行递归 + 缓存 + 并发锁 + 进度报告 + IDisposable)
│   │   └── IServiceCollectionExtensions.cs # DI 注册扩展
│   ├── CalculateFolderSize.Cli/            # 命令行工具 (基于 Spectre.Console)
│   │   ├── App.cs                          # 应用程序主循环 (支持多路径并发计算)
│   │   ├── CliOptions.cs                   # CLI 配置选项
│   │   ├── IPathNormalizer.cs              # 路径标准化接口
│   │   ├── PathNormalizer.cs               # 路径标准化实现
│   │   └── Program.cs                      # 程序入口点
│   ├── CalculateFolderSize.UI.Shared/      # UI 共享代码 (MVVM, 脚手架)
│   ├── CalculateFolderSize.UI.Desktop/     # 桌面应用入口 (脚手架)
│   └── CalculateFolderSize.UI.Android/     # Android 应用入口 (脚手架)
├── tests/
│   ├── CalculateFolderSize.Core.Tests/     # Core 层单元测试 (xunit.v3 + Moq, 覆盖 Calculator/FileSystem/Formatter/Options)
│   └── CalculateFolderSize.Cli.Tests/      # CLI 层冒烟测试
├── Directory.Build.props                   # 共享 MSBuild 属性
├── Directory.Packages.props                # NuGet 依赖集中管理 (CPM)
├── CalculateFolderSize.slnx                # 解决方案文件
└── 其他配置文件 (.editorconfig, .gitattributes, .gitignore, CHANGELOG.md, LICENSE, ReBuild.bat 等)
```

## 快速开始

### 前置要求

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- 如需构建 Android 应用, 请安装 Android 工作负载:
  ```bash
  dotnet workload install android
  ```

### 克隆并构建

```bash
git clone https://github.com/xiting910/CalculateFolderSize.git
cd CalculateFolderSize
dotnet build
```

也可以使用根目录下的 `ReBuild.bat` 脚本, 自动清理所有 `bin` 和 `obj` 目录后执行完整构建.

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
    "DecimalPlaces": 2
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
| `Cli.SizeStringLength`           | 文件大小字符串对齐长度   | 9              |
| `Cli.DirectorySeparator`         | 目标目录分隔符          | `\`            |
| `Cli.ReplacedSeparator`          | 被替换的目录分隔符      | `/`            |
| `Cli.ExitCommand`                | 退出命令               | `exit`         |
| `Cli.ClearCacheCommand`          | 清除缓存命令            | `clearcache`   |

## 许可证

[MIT License](LICENSE)

Copyright (c) 2026 xiting910
