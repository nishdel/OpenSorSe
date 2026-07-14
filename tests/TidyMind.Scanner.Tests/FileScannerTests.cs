using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TidyMind.Core.Errors;
using TidyMind.Core.Logging;
using TidyMind.Scanner;
using TidyMind.Scanner.Models;

namespace TidyMind.Scanner.Tests;

/// <summary>
/// Verifies read-only filesystem traversal behavior.
/// </summary>
public sealed class FileScannerTests
{
    /// <summary>
    /// Verifies that nested files and directories are discovered recursively.
    /// </summary>
    [Fact]
    public async Task ScanAsync_RecursivelyDiscoversFilesAndDirectories()
    {
        using var directory = new TemporaryDirectory();
        var nestedDirectory = directory.CreateDirectory("first\\second");
        var rootFile = directory.CreateFile("root.txt");
        var nestedFile = Path.Combine(nestedDirectory, "nested.txt");
        File.WriteAllText(nestedFile, "content is not read by the scanner");

        var result = await CreateScanner().ScanAsync(CreateRequest(directory.Path));

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Equal(2L, result.Statistics.FilesDiscovered);
        Assert.Equal(3L, result.Statistics.DirectoriesDiscovered);
        Assert.Empty(result.Issues);
        Assert.True(
            new HashSet<string>(PathComparer) { rootFile, nestedFile }.SetEquals(
                result.Files.Select(file => file.FullPath)));
        Assert.True(
            new HashSet<string>(PathComparer)
            {
                directory.Path,
                Path.Combine(directory.Path, "first"),
                nestedDirectory,
            }.SetEquals(result.Directories.Select(entry => entry.FullPath)));
    }

    /// <summary>
    /// Verifies that an empty root is returned as a discovered directory.
    /// </summary>
    [Fact]
    public async Task ScanAsync_IncludesAnEmptyRootDirectory()
    {
        using var directory = new TemporaryDirectory();

        var result = await CreateScanner().ScanAsync(CreateRequest(directory.Path));

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Empty(result.Files);
        Assert.Equal(new[] { directory.Path }, result.Directories.Select(entry => entry.FullPath));
        Assert.Equal(1L, result.Statistics.DirectoriesDiscovered);
    }

    /// <summary>
    /// Verifies that equivalent and overlapping roots produce unique entries.
    /// </summary>
    [Fact]
    public async Task ScanAsync_NormalizesAndDeduplicatesIdenticalAndOverlappingRoots()
    {
        using var directory = new TemporaryDirectory();
        var childDirectory = directory.CreateDirectory("child");
        var childFile = directory.CreateFile("child\\document.txt");
        var duplicateRoot = directory.Path + Path.DirectorySeparatorChar;

        var result = await CreateScanner().ScanAsync(CreateRequest(directory.Path, duplicateRoot, childDirectory));

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Equal(1L, result.Statistics.FilesDiscovered);
        Assert.Equal(2L, result.Statistics.DirectoriesDiscovered);
        Assert.Equal(new[] { childFile }, result.Files.Select(entry => entry.FullPath));
        Assert.True(
            new HashSet<string>(PathComparer) { directory.Path, childDirectory }.SetEquals(
                result.Directories.Select(entry => entry.FullPath)));
    }

    /// <summary>
    /// Verifies that an unavailable root does not prevent other roots from scanning.
    /// </summary>
    [Fact]
    public async Task ScanAsync_RecordsUnavailableRootAndContinuesWithOtherRoots()
    {
        using var directory = new TemporaryDirectory();
        var availableFile = directory.CreateFile("available.txt");
        var missingRoot = Path.Combine(directory.Path, "missing");

        var result = await CreateScanner().ScanAsync(CreateRequest(missingRoot, directory.Path));

        Assert.Equal(ScanStatus.Completed, result.Status);
        Assert.Equal(new[] { availableFile }, result.Files.Select(entry => entry.FullPath));
        Assert.Contains(result.Issues, issue =>
            PathComparer.Equals(issue.Path, missingRoot) &&
            issue.Kind == ScanIssueKind.RootDirectoryUnavailable);
    }

    /// <summary>
    /// Verifies that scanning reports structured progress information.
    /// </summary>
    [Fact]
    public async Task ScanAsync_ReportsStructuredProgress()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateFile("one.txt");
        var reports = new List<ScanProgress>();
        var progress = new CallbackProgress(scanProgress => reports.Add(scanProgress));

        var result = await CreateScanner().ScanAsync(
            CreateRequest(directory.Path, TimeSpan.Zero),
            progress);

        Assert.NotEmpty(reports);
        Assert.Contains(reports, report => report.CurrentPath is not null);
        var finalReport = reports[^1];
        Assert.Equal(result.Statistics, finalReport.Statistics);
        Assert.Null(finalReport.CurrentPath);
    }

    /// <summary>
    /// Verifies that cancellation returns a partial scan result deterministically.
    /// </summary>
    [Fact]
    public async Task ScanAsync_ReturnsPartialResultWhenCancellationIsRequestedDuringProgressReporting()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateFile("one.txt");
        directory.CreateFile("two.txt");
        using var cancellationSource = new CancellationTokenSource();
        var progress = new CallbackProgress(scanProgress =>
        {
            if (scanProgress.CurrentPath is not null)
            {
                cancellationSource.Cancel();
            }
        });

        var result = await CreateScanner().ScanAsync(
            CreateRequest(directory.Path, TimeSpan.Zero),
            progress,
            cancellationSource.Token);

        Assert.Equal(ScanStatus.Cancelled, result.Status);
        Assert.InRange(result.Statistics.FilesDiscovered, 0L, 2L);
        Assert.Equal(result.Statistics.FilesDiscovered, (long)result.Files.Count);
    }

    /// <summary>
    /// Verifies that a scan request requires at least one root directory.
    /// </summary>
    [Fact]
    public async Task ScanAsync_RejectsAnEmptyRootCollection()
    {
        var request = new ScanRequest(Array.Empty<string>(), ScanOptions.Default);

        await Assert.ThrowsAsync<ArgumentException>(() => CreateScanner().ScanAsync(request));
    }

    /// <summary>
    /// Verifies that a negative progress-report interval is rejected.
    /// </summary>
    [Fact]
    public async Task ScanAsync_RejectsANegativeProgressInterval()
    {
        using var directory = new TemporaryDirectory();
        var request = new ScanRequest(
            new[] { directory.Path },
            new ScanOptions(TimeSpan.FromMilliseconds(-1)));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => CreateScanner().ScanAsync(request));
    }

    private static StringComparer PathComparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private static FileScanner CreateScanner() => new(new TestLoggingService(), new TestErrorHandler());

    private static ScanRequest CreateRequest(string firstRoot, params string[] additionalRoots) =>
        CreateRequest(new[] { firstRoot }.Concat(additionalRoots), ScanOptions.Default.ProgressReportInterval);

    private static ScanRequest CreateRequest(string root, TimeSpan progressReportInterval) =>
        CreateRequest(new[] { root }, progressReportInterval);

    private static ScanRequest CreateRequest(IEnumerable<string> roots, TimeSpan progressReportInterval) =>
        new(roots.ToArray(), new ScanOptions(progressReportInterval));

    private sealed class CallbackProgress : IProgress<ScanProgress>
    {
        private readonly Action<ScanProgress> _callback;

        public CallbackProgress(Action<ScanProgress> callback)
        {
            _callback = callback;
        }

        public void Report(ScanProgress value)
        {
            _callback(value);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TidyMind.Scanner.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateDirectory(string relativePath)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string CreateFile(string relativePath)
        {
            var path = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }

    private sealed class TestLoggingService : ILoggingService
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose()
        {
        }

        public void Initialize(LogLevel minimumLevel)
        {
        }
    }

    private sealed class TestErrorHandler : IErrorHandler
    {
        public event EventHandler<ApplicationError>? ErrorReported;

        public void Report(ApplicationError applicationError)
        {
            ErrorReported?.Invoke(this, applicationError);
        }
    }
}
