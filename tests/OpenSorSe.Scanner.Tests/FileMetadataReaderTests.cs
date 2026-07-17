using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner.Tests;

/// <summary>
/// Verifies filesystem metadata enrichment behavior.
/// </summary>
public sealed class FileMetadataReaderTests
{
    /// <summary>
    /// Verifies accessible files are enriched without changing their contents.
    /// </summary>
    [Fact]
    public async Task ReadAsync_EnrichesAccessibleFileWithoutModifyingIt()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.CreateFile("REPORT.TXT", "unchanged content");
        var beforeContent = File.ReadAllText(path);
        var beforeEntries = Directory.GetFileSystemEntries(directory.Path);

        var result = await CreateReader().ReadAsync(new[] { new FileEntry(path) });

        var metadata = Assert.Single(result.Files).Metadata;
        Assert.NotNull(metadata);
        Assert.Equal("REPORT.TXT", metadata.FileName);
        Assert.Equal(".txt", metadata.Extension);
        Assert.Equal((long)beforeContent.Length, metadata.SizeInBytes!.Value);
        Assert.NotNull(metadata.CreationTimeUtc);
        Assert.Equal(TimeSpan.Zero, metadata.CreationTimeUtc.Value.Offset);
        Assert.Equal(beforeContent, File.ReadAllText(path));
        Assert.True(
            new HashSet<string>(beforeEntries, StringComparer.OrdinalIgnoreCase).SetEquals(
                Directory.GetFileSystemEntries(directory.Path)));
    }

    /// <summary>
    /// Verifies input ordering and duplicate entries are preserved.
    /// </summary>
    [Fact]
    public async Task ReadAsync_PreservesInputOrderAndDuplicates()
    {
        using var directory = new TemporaryDirectory();
        var first = directory.CreateFile("first.txt", "one");
        var second = directory.CreateFile("second", "two");
        var entries = new[] { new FileEntry(second), new FileEntry(first), new FileEntry(second) };

        var result = await CreateReader().ReadAsync(entries);

        Assert.Equal(entries.Select(entry => entry.FullPath), result.Files.Select(entry => entry.FullPath));
        Assert.Equal(3L, result.Statistics.FilesProcessed);
        Assert.Equal(3L, result.Statistics.FilesEnriched);
        Assert.Equal(string.Empty, result.Files[0].Metadata!.Extension);
        Assert.Equal(result.Files[0].Metadata, result.Files[2].Metadata);
    }

    /// <summary>
    /// Verifies original immutable entries remain unchanged after enrichment.
    /// </summary>
    [Fact]
    public async Task ReadAsync_DoesNotMutateOriginalEntries()
    {
        using var directory = new TemporaryDirectory();
        var entry = new FileEntry(directory.CreateFile("original.txt", "data"));

        var result = await CreateReader().ReadAsync(new[] { entry });

        Assert.Null(entry.Metadata);
        Assert.NotNull(result.Files[0].Metadata);
        Assert.NotSame(entry, result.Files[0]);
    }

    /// <summary>
    /// Verifies an unavailable file does not prevent later files from being enriched.
    /// </summary>
    [Fact]
    public async Task ReadAsync_RecordsUnavailableFileAndContinues()
    {
        using var directory = new TemporaryDirectory();
        var missing = Path.Combine(directory.Path, "missing.txt");
        var available = directory.CreateFile("available.txt", "available");

        var result = await CreateReader().ReadAsync(new[] { new FileEntry(missing), new FileEntry(available) });

        Assert.Equal(2, result.Files.Count);
        Assert.Null(result.Files[0].Metadata);
        Assert.NotNull(result.Files[1].Metadata);
        Assert.Contains(result.Issues, issue => issue.FilePath == missing && issue.Kind == FileMetadataIssueKind.FileUnavailable);
    }

    /// <summary>
    /// Verifies cancellation is observed before metadata work starts.
    /// </summary>
    [Fact]
    public async Task ReadAsync_ThrowsWhenCancellationIsAlreadyRequested()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateReader().ReadAsync(Array.Empty<FileEntry>(), cancellationSource.Token));
    }

    private static FileMetadataReader CreateReader() => new(new TestLoggingService(), new TestErrorHandler());

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OpenSorSe.Metadata.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
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
