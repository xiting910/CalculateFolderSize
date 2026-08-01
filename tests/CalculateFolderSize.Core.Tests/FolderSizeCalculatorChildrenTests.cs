using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using CalculateFolderSize.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace CalculateFolderSize.Core.Tests;

public sealed class FolderSizeCalculatorChildrenTests : IDisposable
{
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly CoreOptions _options;
    private readonly FolderSizeCalculator _calculator;

    public FolderSizeCalculatorChildrenTests()
    {
        _fileSystemMock = new();
        _options = new(2, Environment.ProcessorCount * 2, true, StringComparer.GetDefault());
        _calculator = new(_options, _fileSystemMock.Object, NullLogger<FolderSizeCalculator>.Instance);
    }

    public void Dispose()
    {
        _calculator.Dispose();
    }

    [Fact]
    public void TryGetFolderChildren_AfterCapturedScan_ReturnsFileAndDirectoryChildren()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([new FileEntry(rootPath + @"\f1.txt", "f1.txt", 100, null)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new DirectoryEntry(subPath, "SubDir")]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);
        var found = _calculator.TryGetFolderChildren(rootPath, out var children);

        Assert.True(found);
        Assert.NotNull(children);
        Assert.Equal(2, children.Count);
        var fileChild = Assert.Single(children.OfType<FileChild>());
        Assert.Equal(rootPath + @"\f1.txt", fileChild.Path);
        var directoryChild = Assert.Single(children.OfType<DirectoryChild>());
        Assert.Equal(subPath, directoryChild.Path);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_FileChild_HasCorrectFields()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([new FileEntry(path + @"\file1.txt", "file1.txt", 100, null)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(path, out var children);

        Assert.NotNull(children);
        var child = Assert.Single(children);
        Assert.Equal(new FileChild(path + @"\file1.txt", "file1.txt", 100, null), child);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_FileWithException_CarriesException()
    {
        const string path = @"C:\Dir";
        var exception = new UnauthorizedAccessException("Access denied");
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([new FileEntry(path + @"\bad.txt", "bad.txt", 0, exception)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(path, out var children);

        Assert.NotNull(children);
        var child = Assert.Single(children);
        Assert.Equal(new FileChild(path + @"\bad.txt", "bad.txt", 0, exception), child);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_DirectoryChild_AggregatesSubtree()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new DirectoryEntry(subPath, "SubDir")]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns([
            new FileEntry(subPath + @"\a.txt", "a.txt", 100, null),
            new FileEntry(subPath + @"\b.txt", "b.txt", 200, null)
        ]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(rootPath, out var children);

        Assert.NotNull(children);
        var child = Assert.Single(children.OfType<DirectoryChild>());
        Assert.Equal(subPath, child.Path);
        Assert.Equal("SubDir", child.Name);
        Assert.Equal(300, child.Size);
        Assert.Equal(0, child.FolderCount);
        Assert.Equal(2, child.FileCount);
        Assert.Empty(child.ErrorPaths);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_FailedSubDirectory_IsExcludedFromChildren()
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

        var result = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(rootPath, out var children);

        Assert.NotNull(result);
        Assert.NotNull(children);
        var child = Assert.Single(children);
        Assert.Equal(goodPath, child.Path);
        Assert.DoesNotContain(children, c => c.Path == badPath);
        var (errorPath, errorException) = Assert.Single(result.ErrorPaths);
        Assert.Equal(badPath, errorPath);
        Assert.Same(exception, errorException);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void GetFromFolder_TotalsMatchChildrenAggregation()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([
            new FileEntry(rootPath + @"\f1.txt", "f1.txt", 100, null),
            new FileEntry(rootPath + @"\f2.txt", "f2.txt", 200, null)
        ]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new DirectoryEntry(subPath, "SubDir")]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns([new FileEntry(subPath + @"\f3.txt", "f3.txt", 300, null)]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);

        var result = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(rootPath, out var children);

        Assert.NotNull(result);
        Assert.NotNull(children);
        Assert.Equal(children.Sum(c => c.Size), result.TotalBytes);
        Assert.Equal(children.OfType<DirectoryChild>().Sum(c => c.FileCount) + children.OfType<FileChild>().Count(), result.FileCount);
        Assert.Equal(children.OfType<DirectoryChild>().Sum(c => c.FolderCount) + children.OfType<DirectoryChild>().Count(), result.FolderCount);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_WithoutCapture_ReturnsFalse()
    {
        const string path = @"C:\Dir";
        var options = _options with { CaptureChildren = false };
        using var calculator = new FolderSizeCalculator(options, _fileSystemMock.Object, NullLogger<FolderSizeCalculator>.Instance);
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        var found = calculator.TryGetFolderChildren(path, out _);

        Assert.False(found);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_NotComputed_ReturnsFalse()
    {
        var found = _calculator.TryGetFolderChildren(@"C:\Unknown", out _);

        Assert.False(found);
    }

    [Fact]
    public void TryGetFolderChildren_AfterClearCache_ReturnsFalse()
    {
        const string path = @"C:\Dir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(path)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(path)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(path)).Returns([]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(path, token: TestContext.Current.CancellationToken);
        Assert.True(_calculator.TryGetFolderChildren(path, out _));

        _calculator.ClearCache();

        Assert.False(_calculator.TryGetFolderChildren(path, out _));

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_SubDirectoryCachedFirst_RootChildrenStillComplete()
    {
        const string rootPath = @"C:\Root";
        const string subPath = @"C:\Root\SubDir";
        _fileSystemMock.Setup(fs => fs.DirectoryExists(subPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(subPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(subPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.DirectoryExists(rootPath)).Returns(true).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateFiles(rootPath)).Returns([]).Verifiable(Times.Once);
        _fileSystemMock.Setup(fs => fs.EnumerateDirectories(rootPath)).Returns([new DirectoryEntry(subPath, "SubDir")]).Verifiable(Times.Once);

        _ = _calculator.GetFromFolder(subPath, token: TestContext.Current.CancellationToken);
        _ = _calculator.GetFromFolder(rootPath, token: TestContext.Current.CancellationToken);
        _ = _calculator.TryGetFolderChildren(rootPath, out var children);

        Assert.NotNull(children);
        var child = Assert.Single(children.OfType<DirectoryChild>());
        Assert.Equal(subPath, child.Path);

        _fileSystemMock.Verify();
    }

    [Fact]
    public void TryGetFolderChildren_Disposed_ThrowsObjectDisposedException()
    {
        var calculator = new FolderSizeCalculator(_options, _fileSystemMock.Object, NullLogger<FolderSizeCalculator>.Instance);
        calculator.Dispose();

        _ = Assert.Throws<ObjectDisposedException>(() => calculator.TryGetFolderChildren(@"C:\Dir", out _));
    }
}
