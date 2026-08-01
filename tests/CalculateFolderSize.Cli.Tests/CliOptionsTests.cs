using CalculateFolderSize.Core;
using Microsoft.Extensions.Configuration;

namespace CalculateFolderSize.Cli.Tests;

public sealed class CliOptionsTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_UsesConfiguredValues()
    {
        var config = CreateConfig(
            ($"{nameof(Cli)}:{nameof(CliOptions.SizeStringLength)}", "15"),
            ($"{nameof(Cli)}:{nameof(CliOptions.DirectorySeparator)}", "/"),
            ($"{nameof(Cli)}:{nameof(CliOptions.ReplacedSeparator)}", "\\"),
            ($"{nameof(Cli)}:{nameof(CliOptions.ExitCommand)}", "quit"),
            ($"{nameof(Cli)}:{nameof(CliOptions.ClearCacheCommand)}", "cc")
        );

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal(15, options.SizeStringLength);
        Assert.Equal('/', options.DirectorySeparator);
        Assert.Equal('\\', options.ReplacedSeparator);
        Assert.Equal("quit", options.ExitCommand);
        Assert.Equal("cc", options.ClearCacheCommand);
    }

    [Fact]
    public void Constructor_WithInvalidSizeStringLength_FallsBackToCalculatedDefault()
    {
        var config = CreateConfig(($"{nameof(Cli)}:{nameof(CliOptions.SizeStringLength)}", "0"));

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal(9, options.SizeStringLength);
    }

    [Fact]
    public void Constructor_WithCustomDecimalPlaces_CalculatesSizeStringLength()
    {
        var config = CreateConfig();

        var options = new CliOptions(config, new(5, 4, false, StringComparer.GetDefault()));

        Assert.Equal(12, options.SizeStringLength);
    }

    [Fact]
    public void Constructor_WithInvalidDirectorySeparator_FallsBackToBackslash()
    {
        var config = CreateConfig(($"{nameof(Cli)}:{nameof(CliOptions.DirectorySeparator)}", "ab"));

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal('\\', options.DirectorySeparator);
    }

    [Fact]
    public void Constructor_WithInvalidReplacedSeparator_FallsBackToForwardSlash()
    {
        var config = CreateConfig(($"{nameof(Cli)}:{nameof(CliOptions.ReplacedSeparator)}", "x"));

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal('/', options.ReplacedSeparator);
    }

    [Fact]
    public void Constructor_WithMissingCommands_FallsBackToDefaults()
    {
        var config = CreateConfig(($"{nameof(Cli)}:{nameof(CliOptions.ExitCommand)}", "   "));

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal("exit", options.ExitCommand);
        Assert.Equal("clearcache", options.ClearCacheCommand);
    }

    [Fact]
    public void Constructor_WithMissingSection_FallsBackToDefaults()
    {
        var config = new ConfigurationBuilder().Build();

        var options = new CliOptions(config, new(2, 4, false, StringComparer.GetDefault()));

        Assert.Equal(9, options.SizeStringLength);
        Assert.Equal('\\', options.DirectorySeparator);
        Assert.Equal('/', options.ReplacedSeparator);
        Assert.Equal("exit", options.ExitCommand);
        Assert.Equal("clearcache", options.ClearCacheCommand);
    }

    [Fact]
    public void ConsecutiveDirectorySeparators_WithBackslash_ReturnsDoubleBackslash()
    {
        var options = new CliOptions(12, '\\', '/', "exit", "clearcache");

        Assert.Equal("\\\\", options.ConsecutiveDirectorySeparators);
    }

    [Fact]
    public void ConsecutiveDirectorySeparators_WithForwardSlash_ReturnsDoubleForwardSlash()
    {
        var options = new CliOptions(12, '/', '\\', "exit", "clearcache");

        Assert.Equal("//", options.ConsecutiveDirectorySeparators);
    }

    private static IConfiguration CreateConfig(params (string Key, string? Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => v.Value))
            .Build();
    }
}
