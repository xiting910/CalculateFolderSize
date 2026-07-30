namespace CalculateFolderSize.Core.Models;

/// <summary>
/// 文件夹大小计算进度报告
/// </summary>
/// <param name="BytesSoFar">已处理的字节数</param>
/// <param name="FoldersProcessed">已处理的文件夹数量</param>
/// <param name="FilesProcessed">已处理的文件数量</param>
public readonly record struct ProgressReport(long BytesSoFar, int FoldersProcessed, int FilesProcessed);
