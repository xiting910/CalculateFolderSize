using System;

namespace CalculateFolderSize.Core;

/// <summary>
/// <see cref="StringComparer"/> 扩展方法
/// </summary>
public static class StringComparerExtensions
{
    /// <summary>
    /// <see cref="StringComparer"/> 类的扩展
    /// </summary>
    extension(StringComparer)
    {
        /// <summary>
        /// 获取默认的 <see cref="StringComparer"/> 实例
        /// </summary>
        /// <returns>默认的 <see cref="StringComparer"/> 实例</returns>
        public static StringComparer GetDefault()
        {
            return OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        }

        /// <summary>
        /// 从指定配置源字符串获取 <see cref="StringComparer"/> 实例
        /// </summary>
        /// <param name="source">配置源字符串</param>
        /// <returns><see cref="StringComparer"/> 实例</returns>
        public static StringComparer GetPathComparer(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return GetDefault();
            }

            // 通过名称匹配获取对应的 StringComparer 实例
            return source switch
            {
                nameof(StringComparer.Ordinal) => StringComparer.Ordinal,
                nameof(StringComparer.OrdinalIgnoreCase) => StringComparer.OrdinalIgnoreCase,
                _ => GetDefault()
            };
        }
    }
}
