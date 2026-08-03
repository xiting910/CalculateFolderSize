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
    /// <param name="stringComparer">字符串比较器</param>
    extension(StringComparer stringComparer)
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

        /// <summary>
        /// 将 <see cref="StringComparer"/> 实例转换为 JSON 字符串
        /// </summary>
        /// <returns>JSON 字符串</returns>
        public string ToJsonString()
        {
            return ReferenceEquals(stringComparer, StringComparer.Ordinal)
                ? nameof(StringComparer.Ordinal)
                : ReferenceEquals(stringComparer, StringComparer.OrdinalIgnoreCase)
                    ? nameof(StringComparer.OrdinalIgnoreCase)
                    : OperatingSystem.IsWindows()
                        ? nameof(StringComparer.OrdinalIgnoreCase)
                        : nameof(StringComparer.Ordinal);
        }
    }
}
