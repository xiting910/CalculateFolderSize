using CalculateFolderSize.UI.Shared.Services;
using Microsoft.Extensions.Logging;

namespace CalculateFolderSize.UI.Shared.Tests;

public sealed class HistoriesStoreTests
{
    private static readonly string HistoriesFilePath = Constants.HistoriesFilePath;

    private static HistoriesStore CreateStore()
    {
        return new(new RecordingLogger(), new(2, 8, false, StringComparer.OrdinalIgnoreCase));
    }

    private static (bool Existed, string[]? Lines) BackupAndDeleteHistoriesFile()
    {
        _ = Directory.CreateDirectory(Constants.AppDataDirectory);
        var existed = File.Exists(HistoriesFilePath);
        var lines = existed ? File.ReadAllLines(HistoriesFilePath) : null;
        if (existed)
        {
            File.Delete(HistoriesFilePath);
        }
        return (existed, lines);
    }

    private static void RestoreHistoriesFile(bool existed, string[]? lines)
    {
        if (existed)
        {
            File.WriteAllLines(HistoriesFilePath, lines!);
        }
        else if (File.Exists(HistoriesFilePath))
        {
            File.Delete(HistoriesFilePath);
        }
    }

    [Fact]
    public async Task AddHistoriesAsync_NewPaths_InsertedAtFrontInOrder()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();

            await store.AddHistoriesAsync(["a", "b"], TestContext.Current.CancellationToken);

            Assert.Equal(["b", "a"], store.Histories);
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task AddHistoriesAsync_DuplicatePath_MovesToFront()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();
            await store.AddHistoriesAsync(["a", "b"], TestContext.Current.CancellationToken);

            await store.AddHistoriesAsync(["a"], TestContext.Current.CancellationToken);

            Assert.Equal(["a", "b"], store.Histories);
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task AddHistoriesAsync_CaseInsensitiveDuplicate_RemovedAndInserted()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();
            await store.AddHistoriesAsync(["a"], TestContext.Current.CancellationToken);

            await store.AddHistoriesAsync(["A"], TestContext.Current.CancellationToken);

            Assert.Equal(["A"], store.Histories);
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task AddHistoriesAsync_SavesHistoriesToFile()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();

            await store.AddHistoriesAsync(["a", "b"], TestContext.Current.CancellationToken);

            Assert.Equal(["b", "a"], File.ReadAllLines(HistoriesFilePath));
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task RemoveHistoriesAsync_ExistingPaths_RemovedFromListAndFile()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();
            await store.AddHistoriesAsync(["a", "b", "c"], TestContext.Current.CancellationToken);

            await store.RemoveHistoriesAsync(["b"], TestContext.Current.CancellationToken);

            Assert.Equal(["c", "a"], store.Histories);
            Assert.Equal(["c", "a"], File.ReadAllLines(HistoriesFilePath));
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task RemoveHistoriesAsync_NonExistentPaths_ListUnchanged()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();
            await store.AddHistoriesAsync(["a", "b"], TestContext.Current.CancellationToken);

            await store.RemoveHistoriesAsync(["c"], TestContext.Current.CancellationToken);

            Assert.Equal(["b", "a"], store.Histories);
            Assert.Equal(["b", "a"], File.ReadAllLines(HistoriesFilePath));
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public async Task Clear_EmptyHistoriesAndDeletesFile()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();
            await store.AddHistoriesAsync(["a", "b"], TestContext.Current.CancellationToken);

            store.Clear();

            Assert.Empty(store.Histories);
            Assert.False(File.Exists(HistoriesFilePath));
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public void Histories_FileExistsAtConstruction_LoadsAllLines()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            File.WriteAllLines(HistoriesFilePath, ["x", "y"]);

            var store = CreateStore();

            Assert.Equal(["x", "y"], store.Histories);
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    [Fact]
    public void Histories_FileNotExistsAtConstruction_Empty()
    {
        var (existed, lines) = BackupAndDeleteHistoriesFile();
        try
        {
            var store = CreateStore();

            Assert.Empty(store.Histories);
        }
        finally
        {
            RestoreHistoriesFile(existed, lines);
        }
    }

    private sealed class RecordingLogger : ILogger<HistoriesStore>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
