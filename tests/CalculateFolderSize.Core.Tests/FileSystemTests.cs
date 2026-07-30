using CalculateFolderSize.Core.Services;

namespace CalculateFolderSize.Core.Tests;

public sealed class FileSystemTests
{
    [Fact]
    public void DirectoryExists_NonExistentPath_ReturnsFalse()
    {
        var fileSystem = new FileSystem();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        var result = fileSystem.DirectoryExists(nonExistentPath);

        Assert.False(result);
    }

    [Fact]
    public void DirectoryExists_ExistingPath_ReturnsTrue()
    {
        var fileSystem = new FileSystem();

        var result = fileSystem.DirectoryExists(Path.GetTempPath());

        Assert.True(result);
    }

    [Fact]
    public void EnumerateFiles_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var fileSystem = new FileSystem();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        _ = Assert.Throws<DirectoryNotFoundException>(() =>
        {
            _ = fileSystem.EnumerateFiles(nonExistentPath).ToList();
        });
    }

    [Fact]
    public void EnumerateDirectories_NonExistentDirectory_ThrowsDirectoryNotFoundException()
    {
        var fileSystem = new FileSystem();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        _ = Assert.Throws<DirectoryNotFoundException>(() =>
        {
            _ = fileSystem.EnumerateDirectories(nonExistentPath).ToList();
        });
    }

    [Fact]
    public void EnumerateFiles_ExistingDirectory_ReturnsFilesWithCorrectSize()
    {
        var fileSystem = new FileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var tempDirInfo = Directory.CreateDirectory(tempDir);
        try
        {
            var content = "Hello, World!";
            var tempFile = Path.Combine(tempDir, "test.txt");
            File.WriteAllText(tempFile, content);

            var files = fileSystem.EnumerateFiles(tempDir).ToList();
            var file = files.First(f => f.FullName == tempFile);

            Assert.Equal(content.Length, file.Size);
            Assert.Null(file.Exception);
        }
        finally
        {
            tempDirInfo.Delete(recursive: true);
        }
    }

    [Fact]
    public void EnumerateDirectories_ExistingDirectory_ReturnsSubDirectories()
    {
        var fileSystem = new FileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var tempDirInfo = Directory.CreateDirectory(tempDir);
        try
        {
            var subDir = Path.Combine(tempDir, "subdir");
            _ = Directory.CreateDirectory(subDir);

            var dirs = fileSystem.EnumerateDirectories(tempDir).ToList();

            Assert.NotEmpty(dirs);
            Assert.Contains(dirs, d => d.FullName == subDir);
        }
        finally
        {
            tempDirInfo.Delete(recursive: true);
        }
    }
}
