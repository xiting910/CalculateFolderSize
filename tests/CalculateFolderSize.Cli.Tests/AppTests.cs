using CalculateFolderSize.Core.Interfaces;
using CalculateFolderSize.Core.Models;
using Moq;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Linq.Expressions;
using System.Text;

namespace CalculateFolderSize.Cli.Tests;

public sealed class AppTests
{
    [Fact]
    public async Task RunAsync_ExitCommand_TerminatesLoop()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(GetFromFolder).Verifiable(Times.Never);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("exit");
        await app.RunAsync();

        console.Verify(c => c.Clear(It.IsAny<bool>()), Times.AtLeastOnce());
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_ExitCommand_IsCaseInsensitive()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(GetFromFolder).Verifiable(Times.Never);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("EXIT");
        await app.RunAsync();

        console.Verify(c => c.Clear(It.IsAny<bool>()), Times.AtLeastOnce());
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_EmptyInput_DoesNotCalculateAndPromptsAgain()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(GetFromFolder).Verifiable(Times.Never);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("", "exit");
        await app.RunAsync();

        console.Verify(c => c.Clear(It.IsAny<bool>()), Times.AtLeast(2));
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_PathInput_CalculatesFolderSize()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.GetFromFolder("C:\\Temp", null, It.IsAny<CancellationToken>()))
            .Returns(CreateResult("C:\\Temp"))
            .Verifiable(Times.Once);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("C:\\Temp", "exit");
        await app.RunAsync();

        console.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.AtLeastOnce());
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_NonexistentPath_ShowsWarningWithoutException()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.GetFromFolder("C:\\Missing", null, It.IsAny<CancellationToken>()))
            .Returns((FolderSize?)null)
            .Verifiable(Times.Once);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("C:\\Missing", "exit");
        await app.RunAsync();

        console.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.AtLeastOnce());
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_ClearCacheCommand_ClearsCalculatorCache()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.TryClearCache()).Returns(true).Verifiable(Times.Once);
        calculator.Setup(GetFromFolder).Verifiable(Times.Never);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("clearcache", "exit");
        await app.RunAsync();

        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_ClearCacheCommand_WhenClearRefused_ShowsWarning()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.TryClearCache()).Returns(false).Verifiable(Times.Once);
        calculator.Setup(GetFromFolder).Verifiable(Times.Never);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("clearcache", "exit");
        await app.RunAsync();

        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_ResultWithErrors_AsksAndPrintsErrors()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.GetFromFolder("C:\\Temp", null, It.IsAny<CancellationToken>()))
            .Returns(CreateResult("C:\\Temp", errorCount: 1))
            .Verifiable(Times.Once);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("C:\\Temp", "y", "exit");
        await app.RunAsync();

        console.Verify(c => c.Write(It.IsAny<IRenderable>()), Times.AtLeastOnce());
        calculator.Verify();
    }

    [Fact]
    public async Task RunAsync_MultipleQuotedPaths_CalculatesAll()
    {
        var (console, input) = CreateConsole();
        var calculator = new Mock<IFolderSizeCalculator>();
        calculator.Setup(c => c.GetFromFolder("C:\\A", null, It.IsAny<CancellationToken>()))
            .Returns(CreateResult("C:\\A"))
            .Verifiable(Times.Once);
        calculator.Setup(c => c.GetFromFolder("C:\\B", null, It.IsAny<CancellationToken>()))
            .Returns(CreateResult("C:\\B"))
            .Verifiable(Times.Once);
        var app = CreateApp(console.Object, calculator.Object);

        input.Queue("\"C:\\A\" \"C:\\B\"", "exit");
        await app.RunAsync();

        calculator.Verify();
    }

    private static FolderSize CreateResult(string path, int errorCount = 0)
    {
        var errors = new Dictionary<string, Exception>();
        for (var i = 0; i < errorCount; i++)
        {
            errors[$"{path}\\bad{i}.txt"] = new UnauthorizedAccessException($"Access denied {i}");
        }

        return new(path, TotalBytes: 2048, FolderCount: 2, FileCount: 3, errors);
    }

    private static App CreateApp(IAnsiConsole console, IFolderSizeCalculator calculator)
    {
        var formatter = new Mock<IFileSizeFormatter>();
        formatter.Setup(f => f.Format(It.IsAny<long>())).Returns("2KB").Verifiable(Times.AtLeastOnce());

        return new(
            CreateOptions(),
            console,
            formatter.Object,
            calculator,
            new UserInputProcessor(new PathNormalizer(CreateOptions()))
        );
    }

    private static CliOptions CreateOptions()
    {
        return new(12, '\\', '/', "exit", "clearcache");
    }

    private static Expression<Func<IFolderSizeCalculator, FolderSize?>> GetFromFolder =>
        c => c.GetFromFolder(It.IsAny<string>(), It.IsAny<IProgress<ProgressReport>?>(), It.IsAny<CancellationToken>());

    private static (Mock<IAnsiConsole> Console, FakeConsoleInput Input) CreateConsole()
    {
        var console = new Mock<IAnsiConsole>();
        var input = new FakeConsoleInput();
        var profile = new Profile(
            new Mock<IAnsiConsoleOutput>().Object,
            new Capabilities { Interactive = true },
            Encoding.UTF8);

        _ = console.SetupGet(c => c.Profile).Returns(profile);
        _ = console.SetupGet(c => c.Input).Returns(input);
        _ = console.SetupGet(c => c.ExclusivityMode).Returns(new RunExclusivityMode());
        _ = console.SetupGet(c => c.Cursor).Returns(new Mock<IAnsiConsoleCursor>().Object);

        return (console, input);
    }

    private sealed class FakeConsoleInput : IAnsiConsoleInput
    {
        private readonly Queue<ConsoleKeyInfo> _keys = new();

        public void Queue(params string[] inputs)
        {
            foreach (var input in inputs)
            {
                foreach (var ch in input) { _keys.Enqueue(ToKey(ch)); }
                _keys.Enqueue(ToKey('\r'));
                if (input.Length > 0) { _keys.Enqueue(ToKey('\r')); }
            }

            _keys.Enqueue(ToKey('\r'));
        }

        public bool IsKeyAvailable()
        {
            return true;
        }

        public ConsoleKeyInfo? ReadKey(bool intercept)
        {
            return _keys.Count == 0
                ? throw new InvalidOperationException("No more input keys queued. The test input sequence is exhausted.")
                : _keys.Dequeue();
        }

        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadKey(intercept));
        }

        private static ConsoleKeyInfo ToKey(char ch)
        {
            var key = ch == '\r' ? ConsoleKey.Enter : (ConsoleKey)char.ToUpperInvariant(ch);
            return new(ch, key, false, false, false);
        }
    }

    private sealed class RunExclusivityMode : IExclusivityMode
    {
        public T Run<T>(Func<T> func)
        {
            return func();
        }

        public Task<T> RunAsync<T>(Func<Task<T>> func)
        {
            return func();
        }
    }
}
