using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace CalculateFolderSize.Core.Tests;

public sealed class FolderSizeCalculatorLoggingTests : IDisposable
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly RecordingLogger _logger;
    private readonly FolderSizeCalculator _calculator;

    public FolderSizeCalculatorLoggingTests()
    {
        _fileSystemMock = new();
        _logger = new();
        var options = new CoreOptions(2, Environment.ProcessorCount * 2, false, StringComparer.GetDefault());
        _calculator = new(options, _fileSystemMock.Object, _logger);
    }

    public void Dispose()
    {
        _calculator.Dispose();
    }

    [Fact]
    public void GetFromFolder_LogsStartedAndCompleted()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);

        AssertEvent(1, LogLevel.Information, 1);
        AssertEvent(2, LogLevel.Information, 1);
        AssertEvent(4, LogLevel.Debug, 0);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_SecondCallWithCachedResult_LogsCacheHit()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Exactly(2));
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);

        AssertEvent(1, LogLevel.Information, 2);
        AssertEvent(2, LogLevel.Information, 2);
        AssertEvent(4, LogLevel.Debug, 1);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_FileWithException_LogsFileSizeFailed()
    {
        const string path = @"C:\Dir";
        var exception = new UnauthorizedAccessException("Access denied");
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([new FileEntry(path + @"\bad.txt", "bad.txt", 0, exception)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);

        AssertEventWithException(6, LogLevel.Debug, exception, 1);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_SubDirectoryThrows_LogsSubDirectoryFailed()
    {
        const string rootPath = @"C:\Root";
        const string goodPath = @"C:\Root\Good";
        const string badPath = @"C:\Root\Bad";
        var exception = new UnauthorizedAccessException("Access denied");
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new DirectoryEntry(goodPath, "Good"), new DirectoryEntry(badPath, "Bad")]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(goodPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(goodPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(badPath)).Throws(exception).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);

        AssertEvent(5, LogLevel.Debug, 1);
        AssertEventWithException(7, LogLevel.Debug, exception, 1);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_Cancellation_LogsScanCanceledAndThrows()
    {
        const string path = @"C:\Dir";
        using var cts = new CancellationTokenSource();
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns(() =>
        {
            cts.Cancel();
            return [];
        }).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.AtMostOnce());

        _ = Assert.Throws<OperationCanceledException>(() => _calculator.GetFromFolder(path, token: cts.Token));

        AssertEvent(9, LogLevel.Debug, 1);
        AssertEvent(2, LogLevel.Information, 0);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void ClearCache_LogsCacheCleared()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        _calculator.ClearCache();

        AssertEvent(8, LogLevel.Information, 1);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_NonexistentDirectory_LogsDirectoryNotFound()
    {
        const string path = @"C:\Missing";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(false).Verifiable(Times.Once);

        var result = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);

        Assert.Null(result);
        AssertEvent(3, LogLevel.Information, 1);
        AssertEvent(1, LogLevel.Information, 0);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_WithSubDirectories_LogsDirectoryCalculatedPerSubDir()
    {
        const string rootPath = @"C:\Root";
        const string subPath1 = @"C:\Root\Sub1";
        const string subPath2 = @"C:\Root\Sub2";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([
            new DirectoryEntry(subPath1, "Sub1"),
            new DirectoryEntry(subPath2, "Sub2")
        ]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath1)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath1)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath2)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath2)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);

        AssertEvent(5, LogLevel.Debug, 2);

        _fileSystemMock.Verify();
    }

    private void AssertEvent(int eventId, LogLevel level, int count)
    {
        Assert.Equal(count, _logger.Events.Count(e => e.EventId.Id == eventId && e.Level == level));
    }

    private void AssertEventWithException(int eventId, LogLevel level, Exception exception, int count)
    {
        Assert.Equal(count, _logger.Events.Count(e => e.EventId.Id == eventId && e.Level == level && e.Exception == exception));
    }

    private sealed class RecordingLogger : ILogger<FolderSizeCalculator>
    {
        public List<(LogLevel Level, EventId EventId, Exception? Exception, string Message)> Events { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Events.Add((logLevel, eventId, exception, formatter(state, exception)));
        }
    }
}
