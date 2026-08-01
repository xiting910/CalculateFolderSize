namespace CalculateFolderSize.Cli.Tests;

public sealed class UserInputProcessorTests
{
    [Fact]
    public void ParsePaths_SinglePath_AddsNormalizedPath()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("C:/Temp/", paths);

        Assert.Equal(["C:\\Temp"], paths);
    }

    [Fact]
    public void ParsePaths_MultipleQuotedPaths_AddsAll()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\"C:\\A\" \"C:\\B\"", paths);

        Assert.Equal(["C:\\A", "C:\\B"], paths);
    }

    [Fact]
    public void ParsePaths_QuotedPathWithSpaces_KeepsPathIntact()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\"C:\\My Folder\\A\"", paths);

        Assert.Equal(["C:\\My Folder\\A"], paths);
    }

    [Fact]
    public void ParsePaths_QuotedPath_TrimsWhitespace()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\" C:\\A \"", paths);

        Assert.Equal(["C:\\A"], paths);
    }

    [Fact]
    public void ParsePaths_UnclosedQuote_OnlyParsesCompletePairs()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\"C:\\A\" \"C:\\B", paths);

        Assert.Equal(["C:\\A"], paths);
    }

    [Fact]
    public void ParsePaths_AdjacentQuotes_ProducesNoPaths()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\"\"", paths);

        Assert.Empty(paths);
    }

    [Fact]
    public void ParsePaths_SecondCall_ClearsPreviousResults()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("\"C:\\A\"", paths);
        processor.ParsePaths("\"C:\\B\"", paths);

        Assert.Equal(["C:\\B"], paths);
    }

    [Fact]
    public void ParsePaths_EmptyInput_AddsEmptyPath()
    {
        var paths = new List<string>();
        var processor = CreateProcessor();

        processor.ParsePaths("", paths);

        Assert.Equal([""], paths);
    }

    private static UserInputProcessor CreateProcessor()
    {
        return new(new PathNormalizer(new(12, '\\', '/', "exit", "clearcache")));
    }
}
