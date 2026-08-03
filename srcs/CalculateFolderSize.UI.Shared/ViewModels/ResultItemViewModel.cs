using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using System.Linq;

namespace CalculateFolderSize.UI.Shared.ViewModels;

/// <summary>
/// 结果窗口中单个子项的视图模型
/// </summary>
public sealed class ResultItemViewModel
{
    /// <summary>
    /// 异常明细最大显示行数, 超出部分舍弃并在末尾提示
    /// </summary>
    private const int MaxErrorLines = 10;

    /// <summary>
    /// 文件大小格式化器
    /// </summary>
    private readonly IFileSizeFormatter _formatter;

    /// <summary>
    /// 创建子项视图模型
    /// </summary>
    /// <param name="formatter">文件大小格式化器</param>
    /// <param name="child">文件夹子项</param>
    /// <param name="parentTotalBytes">父目录总字节数, 用于计算占用百分比</param>
    public ResultItemViewModel(IFileSizeFormatter formatter, FolderChild child, long parentTotalBytes)
    {
        _formatter = formatter;
        IsDirectory = child is DirectoryChild;
        Name = child.Name;
        Path = child.Path;
        Size = child.Size;
        Percent = parentTotalBytes > 0 ? (double)child.Size / parentTotalBytes : 0;

        if (child is DirectoryChild dir)
        {
            FolderCount = dir.FolderCount;
            FileCount = dir.FileCount;
            if (dir.ErrorPaths.Count > 0)
            {
                ErrorCount = dir.ErrorPaths.Count;
                var lines = dir.ErrorPaths.Select(pair => $"{pair.Key}: {pair.Value.Message}").ToList();
                ErrorDetailText = string.Join("\n", lines.Take(MaxErrorLines));
                if (lines.Count > MaxErrorLines)
                {
                    ErrorDetailText += $"\n... 其余 {lines.Count - MaxErrorLines} 条未显示";
                }
            }
        }
        else if (child is FileChild file && file.Exception is not null)
        {
            ErrorCount = 1;
            ErrorDetailText = $"{child.Path}: {file.Exception.Message}";
        }
    }

    /// <summary>
    /// 是否为文件夹
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// 类型图标文本
    /// </summary>
    public string IconText => IsDirectory ? "📁" : "📄";

    /// <summary>
    /// 子项路径
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 子项名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 子项总字节数
    /// </summary>
    public long Size { get; }

    /// <summary>
    /// 子项大小的格式化文本
    /// </summary>
    public string FormattedSize => _formatter.Format(Size);

    /// <summary>
    /// 占父目录总字节数的比例
    /// </summary>
    public double Percent { get; }

    /// <summary>
    /// 文件夹数量, 仅文件夹有值
    /// </summary>
    public int? FolderCount { get; }

    /// <summary>
    /// 文件数量, 仅文件夹有值
    /// </summary>
    public int? FileCount { get; }

    /// <summary>
    /// 是否存在访问错误, 由异常数量派生, 供视图可见性绑定
    /// </summary>
    public bool HasError => ErrorCount > 0;

    /// <summary>
    /// 异常数量
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// 异常明细, 按 "路径: 异常" 逐行显示, 超过最大行数时舍弃后面的行
    /// </summary>
    public string? ErrorDetailText { get; }
}
