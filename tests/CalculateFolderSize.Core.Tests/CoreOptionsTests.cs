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
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "8",
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.DecimalPlaces)}"] = "3"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Equal(8, options.MaxDegreeOfParallelism);
        Assert.Equal(3, options.DecimalPlaces);
    }

    [Fact]
    public void Constructor_WithInvalidMaxDegreeOfParallelism_FallsBackIndependently()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "0",
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.DecimalPlaces)}"] = "3"
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
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.MaxDegreeOfParallelism)}"] = "4",
                [$"{nameof(CoreOptions)}:{nameof(CoreOptions.DecimalPlaces)}"] = "-1"
            })
            .Build();

        var options = new CoreOptions(config);

        Assert.Equal(4, options.MaxDegreeOfParallelism);
        Assert.Equal(2, options.DecimalPlaces);
    }

    [Fact]
    public void Constructor_WithMissingSection_FallsBackToDefaults()
    {
        var config = new ConfigurationBuilder().Build();

        var options = new CoreOptions(config);

        Assert.Equal(Environment.ProcessorCount * 2, options.MaxDegreeOfParallelism);
        Assert.Equal(2, options.DecimalPlaces);
    }
}
