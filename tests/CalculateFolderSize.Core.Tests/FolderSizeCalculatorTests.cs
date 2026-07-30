using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.Core.Services;
using Moq;

namespace CalculateFolderSize.Core.Tests;

public sealed class FolderSizeCalculatorTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly CoreOptions _options;
    private readonly FolderSizeCalculator _calculator;

    public FolderSizeCalculatorTests()
    {
        _fileSystemMock = new();
        _options = new(MaxDegreeOfParallelism: Environment.ProcessorCount * 2, DecimalPlaces: 2);
        _calculator = new(_options, _fileSystemMock.Object);
    }

    [Fact]
    public async Task GetFromFolderAsync_NonExistentDirectory_ReturnsNull()
    {
        const string path = @"C:\NonExistent";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(false).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);

        Assert.Null(result);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_EmptyDirectory_ReturnsFolderSizeWithZeroFiles()
    {
        const string path = @"C:\EmptyDir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(path, result.Path);
        Assert.Equal(0, result.TotalBytes);
        Assert.Equal(0, result.FolderCount);
        Assert.Equal(0, result.FileCount);
        Assert.Empty(result.ErrorPaths);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_DirectoryWithFiles_CalculatesCorrectSize()
    {
        const string path = @"C:\DirWithFiles";
        var files = new[]
        {
            new FileEntry(@"C:\DirWithFiles\file1.txt", 100, null),
            new FileEntry(@"C:\DirWithFiles\file2.txt", 200, null)
        };

        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns(files).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(path, result.Path);
        Assert.Equal(300, result.TotalBytes);
        Assert.Equal(0, result.FolderCount);
        Assert.Equal(2, result.FileCount);
        Assert.Empty(result.ErrorPaths);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_FileWithException_RecordsError()
    {
        const string path = @"C:\DirWithErrors";
        var exception = new UnauthorizedAccessException("Access denied");
        var files = new[]
        {
            new FileEntry(@"C:\DirWithErrors\bad_file.txt", 0, exception),
            new FileEntry(@"C:\DirWithErrors\good_file.txt", 50, null)
        };

        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns(files).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(50, result.TotalBytes);
        var (errorPath, errorException) = Assert.Single(result.ErrorPaths);
        Assert.Equal(@"C:\DirWithErrors\bad_file.txt", errorPath);
        Assert.Same(exception, errorException);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_WithSubDirectories_CalculatesRecursively()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        var rootFiles = new[]
        {
            new FileEntry(@"C:\Root\root_file.txt", 100, null)
        };
        var subDirEntries = new[]
        {
            new DirectoryEntry(subPath)
        };
        var subDirFiles = new[]
        {
            new FileEntry(@"C:\Root\SubDir\sub_file.txt", 200, null)
        };

        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns(rootFiles).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns(subDirEntries).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns(subDirFiles).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(rootPath, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(rootPath, result.Path);
        Assert.Equal(300, result.TotalBytes);
        Assert.Equal(1, result.FolderCount);
        Assert.Equal(2, result.FileCount);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_SubDirectoryThrowsException_RecordsErrorAndContinues()
    {
        const string rootPath = @"C:\Root";
        const string goodPath = @"C:\Root\Good";
        const string badPath = @"C:\Root\Bad";
        var exception = new UnauthorizedAccessException("Access denied");

        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new(goodPath), new(badPath)]).Verifiable(Times.Once);

        _fileSystemMock.Setup(fs => fs.EnumerateFiles(goodPath)).Returns([new(goodPath + @"\f.txt", 100, null)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(goodPath)).Returns([]).Verifiable(Times.Once);

        _fileSystemMock.Setup(fs => fs.EnumerateFiles(badPath)).Throws(exception).Verifiable(Times.Once);

        var result = await _calculator.GetFromFolderAsync(rootPath, token: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(100, result.TotalBytes);
        Assert.Equal(1, result.FileCount);
        Assert.Equal(1, result.FolderCount);
        var (errorPath, errorException) = Assert.Single(result.ErrorPaths);
        Assert.Equal(badPath, errorPath);
        Assert.Same(exception, errorException);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task ClearCache_RemovesAllCachedEntries()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);
        Assert.Equal(1, _calculator.CacheCount);

        _calculator.ClearCache();

        Assert.Equal(0, _calculator.CacheCount);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_CachedResult_UsesCache()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Exactly(2));
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        var result1 = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);
        var result2 = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);

        Assert.Equal(result1, result2);

        _fileSystemMock.Verify();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task CacheCount_PropertyChanged_IsRaised(int expectedCalls)
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Exactly(expectedCalls + 1));
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        var eventRaisedCount = 0;
        _calculator.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IFolderSizeCalculator.CacheCount))
            {
                eventRaisedCount++;
            }
        };

        FolderSize? oldResult = null, result;
        for (var i = 0; i < expectedCalls + 1; i++)
        {
            result = await _calculator.GetFromFolderAsync(path, token: TestContext.Current.CancellationToken);
            if (i > 0)
            {
                Assert.Equal(oldResult, result);
            }
            oldResult = result;
        }

        Assert.Equal(1, eventRaisedCount);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_ProgressReport_MatchesFinalResult()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        var rootFiles = new[]
        {
            new FileEntry(@"C:\Root\f1.txt", 100, null),
            new FileEntry(@"C:\Root\f2.txt", 200, null)
        };
        var subDirEntries = new[] { new DirectoryEntry(subPath) };
        var subDirFiles = new[] { new FileEntry(subPath + @"\f3.txt", 300, null) };

        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns(rootFiles).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns(subDirEntries).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns(subDirFiles).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);

        var reports = new List<ProgressReport>();
        var progress = new SynchronousProgress<ProgressReport>(reports.Add);

        var result = await _calculator.GetFromFolderAsync(rootPath, progress, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(rootFiles.Length + subDirEntries.Length + subDirFiles.Length, reports.Count);
        Assert.Equal(result.TotalBytes, reports[^1].BytesSoFar);
        Assert.Equal(result.FileCount, reports[^1].FilesProcessed);
        Assert.Equal(result.FolderCount, reports[^1].FoldersProcessed);

        _fileSystemMock.Verify();
    }

    [Fact]
    public async Task GetFromFolderAsync_Cancellation_ThrowsOperationCanceledException()
    {
        const string path = @"C:\Dir";
        var files = new[]
        {
            new FileEntry(@"C:\Dir\file1.txt", 100, null),
            new FileEntry(@"C:\Dir\file2.txt", 200, null),
            new FileEntry(@"C:\Dir\file3.txt", 300, null)
        };

        using var cts = new CancellationTokenSource();
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns(() =>
        {
            cts.Cancel();
            return files;
        }).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.AtMostOnce());

        _ = await Assert.ThrowsAsync<OperationCanceledException>(() => _calculator.GetFromFolderAsync(path, token: cts.Token));

        _fileSystemMock.Verify();
    }

    private sealed class SynchronousProgress<T>(Action<T> _handler) : IProgress<T>
    {
        public void Report(T value)
        {
            _handler(value);
        }
    }
}
