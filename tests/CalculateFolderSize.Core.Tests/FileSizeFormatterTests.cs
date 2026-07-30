using CalculateFolderSize.Core.Models;
using CalculateFolderSize.Core.Services;

namespace CalculateFolderSize.Core.Tests;

public sealed class FileSizeFormatterTests
{
    [Fact]
    public void Format_ZeroBytes_ReturnsZeroBytes()
    {
        var options = new CoreOptions(8, 2);
        var formatter = new FileSizeFormatter(options);

        var result = formatter.Format(0);

        Assert.Equal("0 B", result);
    }

    [Fact]
    public void Format_NegativeBytes_ThrowsArgumentOutOfRangeException()
    {
        var options = new CoreOptions(8, 2);
        var formatter = new FileSizeFormatter(options);

        _ = Assert.Throws<ArgumentOutOfRangeException>(() => formatter.Format(-1));
    }

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1KB")]
    [InlineData(1536, "1.5KB")]
    [InlineData(1048576, "1MB")]
    [InlineData(1073741824, "1GB")]
    [InlineData(1099511627776, "1TB")]
    [InlineData(1125899906842624, "1PB")]
    public void Format_WithTwoDecimalPlaces_FormatsCorrectly(long bytes, string expected)
    {
        var options = new CoreOptions(8, 2);
        var formatter = new FileSizeFormatter(options);

        var result = formatter.Format(bytes);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1, "1 B")]
    [InlineData(1024, "1KB")]
    [InlineData(2048, "2KB")]
    [InlineData(1536, "1.5KB")]
    [InlineData(1073741824, "1GB")]
    public void Format_WithOneDecimalPlace_FormatsCorrectly(long bytes, string expected)
    {
        var options = new CoreOptions(8, 1);
        var formatter = new FileSizeFormatter(options);

        var result = formatter.Format(bytes);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1024, "1KB")]
    [InlineData(1536, "2KB")]
    [InlineData(2048, "2KB")]
    [InlineData(512, "512 B")]
    public void Format_WithZeroDecimalPlaces_FormatsCorrectly(long bytes, string expected)
    {
        var options = new CoreOptions(8, 0);
        var formatter = new FileSizeFormatter(options);

        var result = formatter.Format(bytes);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Format_MaxLongValue_FormatsWithoutException()
    {
        var options = new CoreOptions(8, 2);
        var formatter = new FileSizeFormatter(options);

        var result = formatter.Format(long.MaxValue);

        Assert.EndsWith("EB", result);
    }

}
