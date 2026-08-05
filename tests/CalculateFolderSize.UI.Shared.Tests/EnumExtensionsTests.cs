using CalculateFolderSize.UI.Shared.Models;
using Microsoft.Extensions.Logging;

namespace CalculateFolderSize.UI.Shared.Tests;

public sealed class EnumExtensionsTests
{
    [Theory]
    [InlineData(ThemeMode.System, "跟随系统")]
    [InlineData(ThemeMode.Light, "浅色")]
    [InlineData(ThemeMode.Dark, "深色")]
    public void GetDescription_WithDescriptionAttribute_ReturnsDescription(ThemeMode value, string expected)
    {
        Assert.Equal(expected, value.GetDescription());
    }

    [Theory]
    [InlineData(CalculateTaskStatus.Running, "运行中")]
    [InlineData(CalculateTaskStatus.Completed, "已完成")]
    [InlineData(CalculateTaskStatus.Cancelled, "已取消")]
    [InlineData(CalculateTaskStatus.DirectoryNotFound, "目录不存在")]
    [InlineData(CalculateTaskStatus.Failed, "失败")]
    public void GetDescription_TaskStatuses_ReturnsDescription(CalculateTaskStatus value, string expected)
    {
        Assert.Equal(expected, value.GetDescription());
    }

    [Fact]
    public void GetDescription_WithoutDescriptionAttribute_ReturnsName()
    {
        Assert.Equal(nameof(LogLevel.Information), LogLevel.Information.GetDescription());
    }
}
