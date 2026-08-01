using CalculateFolderSize.Core.Models;
using Microsoft.Extensions.Configuration;

namespace CalculateFolderSize.Core.Tests;

public sealed class CoreOptionsTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_UsesConfiguredValues()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(Core)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "8",
                [$"{nameof(Core)}:{nameof(CoreOptions.DecimalPlaces)}"] = "3"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Equal(8, options.MaxDegreeOfParallelism);
        Assert.Equal(3, options.DecimalPlaces);
    }

    [Fact]
    public void Constructor_WithValidPathComparer_UsesConfiguredComparer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(Core)}:{nameof(CoreOptions.PathComparer)}"] = nameof(StringComparer.OrdinalIgnoreCase)
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Same(StringComparer.OrdinalIgnoreCase, options.PathComparer);
    }

    [Fact]
    public void Constructor_WithInvalidMaxDegreeOfParallelism_FallsBackIndependently()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(Core)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "0",
                [$"{nameof(Core)}:{nameof(CoreOptions.DecimalPlaces)}"] = "3"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism);
        Assert.Equal(3, options.DecimalPlaces);
    }

    [Fact]
    public void Constructor_WithInvalidDecimalPlaces_FallsBackIndependently()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(Core)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "4",
                [$"{nameof(Core)}:{nameof(CoreOptions.DecimalPlaces)}"] = "-1"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Equal(4, options.MaxDegreeOfParallelism);
        Assert.Equal(2, options.DecimalPlaces);
    }

    [Fact]
    public void Constructor_WithInvalidPathComparer_FallsBackToPlatformDefault()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(Core)}:{nameof(CoreOptions.PathComparer)}"] = "NotAComparer"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Same(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, options.PathComparer);
    }

    [Fact]
    public void Constructor_WithMissingSection_FallsBackToDefaults()
    {
        var config = new ConfigurationBuilder().Build();

        var options = new CoreOptions(config);

        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism);
        Assert.Equal(2, options.DecimalPlaces);
        Assert.Same(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal, options.PathComparer);
    }
}
