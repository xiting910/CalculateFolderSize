using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CalculateFolderSize.Core;

/// <summary>
/// <see cref="Core"/> 层服务的 DI 注册扩展方法
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// <see cref="IServiceCollection"/> 类的扩展
    /// </summary>
    /// <param name="services">服务集合</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// 注册 <see cref="Core"/> 层的所有服务
        /// </summary>
        /// <remarks>
        /// 非桌面应用程序请将 <paramref name="isDesktop"/> 参数设置为 <see langword="false"/>,
        /// 并自行注册 <see cref="IFileSystem"/> 的实现
        /// </remarks>
        /// <param name="isDesktop">是否为桌面应用程序</param>
        /// <returns>服务集合</returns>
        /// <exception cref="PlatformNotSupportedException">不支持的操作系统或架构</exception>
        public IServiceCollection AddCore(bool isDesktop = true)
        {
            _ = services.AddSingleton<CoreOptions>().AddSingleton<IFileSizeFormatter, FileSizeFormatter>();
            if (isDesktop)
            {
                _ = services.AddSingleton<IFileSystem, DesktopFileSystem>();
            }
            return services.AddSingleton<IFolderSizeCalculator, FolderSizeCalculator>();
        }
    }
}
