using CalculateFolderSize.Core.Models;
using CalculateFolderSize.UI.Shared.Models;
using CalculateFolderSize.UI.Shared.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CalculateFolderSize.UI.Shared.Tests;

public sealed class SettingsStoreTests
{
    private static readonly string SettingsFilePath = Constants.SettingsFilePath;

    private static SettingsStore CreateStore()
    {
        return new(new RecordingLogger(), new(2, 8, false, StringComparer.OrdinalIgnoreCase),
            new(ThemeMode.System, 200, 3, LogLevel.Information));
    }

    private static (bool Existed, string? Content) BackupSettingsFile()
    {
        var existed = File.Exists(SettingsFilePath);
        return (existed, existed ? File.ReadAllText(SettingsFilePath) : null);
    }

    private static void RestoreSettingsFile(bool existed, string? content)
    {
        if (existed)
        {
            File.WriteAllText(SettingsFilePath, content!);
        }
        else if (File.Exists(SettingsFilePath))
        {
            File.Delete(SettingsFilePath);
        }
    }

    [Fact]
    public async Task UpdateCoreOptionsAsync_SavesJsonWithUpdatedValues()
    {
        var (existed, content) = BackupSettingsFile();
        try
        {
            var store = CreateStore();

            await store.UpdateCoreOptionsAsync(o => o with
            {
                DecimalPlaces = 3,
                MaxDegreeOfParallelism = 6,
                CaptureChildren = true
            }, TestContext.Current.CancellationToken);

            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            var core = doc.RootElement.GetProperty(nameof(Core));
            Assert.Equal(3, core.GetProperty(nameof(CoreOptions.DecimalPlaces)).GetInt32());
            Assert.Equal(6, core.GetProperty(nameof(CoreOptions.MaxDegreeOfParallelism)).GetInt32());
            Assert.True(core.GetProperty(nameof(CoreOptions.CaptureChildren)).GetBoolean());
            Assert.Equal(nameof(StringComparer.OrdinalIgnoreCase), core.GetProperty(nameof(CoreOptions.PathComparer)).GetString());
        }
        finally
        {
            RestoreSettingsFile(existed, content);
        }
    }

    [Fact]
    public async Task UpdateUIOptionsAsync_SavesJsonWithUpdatedValues()
    {
        var (existed, content) = BackupSettingsFile();
        try
        {
            var store = CreateStore();

            await store.UpdateUIOptionsAsync(o => o with
            {
                Theme = ThemeMode.Dark,
                ThrottleIntervalMilliseconds = 500,
                ToastDurationSeconds = 7,
                Level = LogLevel.Warning
            }, TestContext.Current.CancellationToken);

            using var doc = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            var ui = doc.RootElement.GetProperty(nameof(UI));
            Assert.Equal(nameof(ThemeMode.Dark), ui.GetProperty(nameof(UIOptions.Theme)).GetString());
            Assert.Equal(500, ui.GetProperty(nameof(UIOptions.ThrottleIntervalMilliseconds)).GetInt32());
            Assert.Equal(7, ui.GetProperty(nameof(UIOptions.ToastDurationSeconds)).GetDouble());
            Assert.Equal(nameof(LogLevel.Warning), ui.GetProperty(nameof(UIOptions.Level)).GetString());
        }
        finally
        {
            RestoreSettingsFile(existed, content);
        }
    }

    private sealed class RecordingLogger : ILogger<SettingsStore>
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
