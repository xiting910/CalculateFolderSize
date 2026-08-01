namespace CalculateFolderSize.Cli.Tests;

public sealed class PathNormalizerTests
{
    [Fact]
    public void Normalize_NullPath_ReturnsNull()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(null);

        Assert.Null(result);
    }

    [Fact]
    public void Normalize_EmptyPath_ReturnsEmpty()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Normalize_WhitespacePath_ReturnsAsIs()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("   ");

        Assert.Equal("   ", result);
    }

    [Fact]
    public void Normalize_TrimsSurroundingWhitespace()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("  C:\\Temp  ");

        Assert.Equal("C:\\Temp", result);
    }

    [Fact]
    public void Normalize_ReplacesReplacedSeparator()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:/Temp/Folder");

        Assert.Equal("C:\\Temp\\Folder", result);
    }

    [Fact]
    public void Normalize_CollapsesConsecutiveSeparators()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:\\\\Temp\\\\\\Folder");

        Assert.Equal("C:\\Temp\\Folder", result);
    }

    [Fact]
    public void Normalize_MixedSeparators_AreCollapsedToSingle()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:////Temp///");

        Assert.Equal("C:\\Temp", result);
    }

    [Fact]
    public void Normalize_RemovesTrailingSeparator()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:\\Temp\\\\");

        Assert.Equal("C:\\Temp", result);
    }

    [Fact]
    public void Normalize_SingleDriveLetter_OnWindows_ReturnsDriveRoot()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C");

        Assert.Equal("C:\\", result);
    }

    [Fact]
    public void Normalize_DriveLetterWithColon_OnWindows_ReturnsDriveRoot()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:");

        Assert.Equal("C:\\", result);
    }

    [Fact]
    public void Normalize_DriveRootWithTrailingSeparator_OnWindows_ReturnsDriveRoot()
    {
        if (!OperatingSystem.IsWindows()) { return; }
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize("C:/");

        Assert.Equal("C:\\", result);
    }

    private static PathNormalizer CreateNormalizer()
    {
        return new(new(12, '\\', '/', "exit", "clearcache"));
    }
}
