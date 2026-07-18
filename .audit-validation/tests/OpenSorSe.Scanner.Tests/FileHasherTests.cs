using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner.Tests;

/// <summary>Verifies SHA-256 file hashing behavior.</summary>
public sealed class FileHasherTests
{
    /// <summary>Verifies the normalized SHA-256 hash of a known input.</summary>
    [Fact]
    public async Task HashAsync_HashesKnownContent()
    {
        using var directory = new TemporaryDirectory();
        var path = directory.CreateFile("input.txt", "abc");

        var result = await CreateHasher().HashAsync(new[] { new FileEntry(path) });

        var hash = Assert.Single(result.Files).Hash;
        Assert.NotNull(hash);
        Assert.Equal("SHA-256", hash.Algorithm);
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", hash.Value);
        Assert.Equal(1L, result.Statistics.FilesHashed);
        Assert.Equal(3L, result.Statistics.BytesHashed);
    }

    /// <summary>Verifies order, duplicate entries, metadata, and original entries are preserved.</summary>
    [Fact]
    public async Task HashAsync_PreservesOrderDuplicatesAndMetadata()
    {
        using var directory = new TemporaryDirectory();
        var first = directory.CreateFile("first.txt", "one");
        var second = directory.CreateFile("second.txt", "two");
        var metadata = new FileMetadata("first.txt", ".txt", 3, null, null, null, FileAttributes.Normal);
        var entries = new[] { new FileEntry(second), new FileEntry(first, metadata), new FileEntry(second) };

        var result = await CreateHasher().HashAsync(entries);

        Assert.Equal(entries.Select(entry => entry.FullPath), result.Files.Select(entry => entry.FullPath));
        Assert.Equal(3L, result.Statistics.FilesProcessed);
        Assert.Equal(3L, result.Statistics.FilesHashed);
        Assert.Same(metadata, result.Files[1].Metadata);
        Assert.Null(entries[1].Hash);
    }

    /// <summary>Verifies a failed entry clears stale hashes and does not stop later entries.</summary>
    [Fact]
    public async Task HashAsync_ContinuesAfterUnavailableFileAndClearsStaleHash()
    {
        using var directory = new TemporaryDirectory();
        var missing = Path.Combine(directory.Path, "missing.txt");
        var available = directory.CreateFile("available.txt", "available");
        var stale = new FileHash("SHA-256", new string('a', 64));

        var result = await CreateHasher().HashAsync(new[] { new FileEntry(missing, Hash: stale), new FileEntry(available) });

        Assert.Null(result.Files[0].Hash);
        Assert.NotNull(result.Files[1].Hash);
        Assert.Contains(result.Issues, issue => issue.Kind == FileHashIssueKind.FileUnavailable);
    }

    /// <summary>Verifies directories are skipped as non-regular entries.</summary>
    [Fact]
    public async Task HashAsync_SkipsDirectory()
    {
        using var directory = new TemporaryDirectory();

        var result = await CreateHasher().HashAsync(new[] { new FileEntry(directory.Path) });

        Assert.Null(result.Files[0].Hash);
        Assert.Contains(result.Issues, issue => issue.Kind == FileHashIssueKind.NonRegularFileSkipped);
    }

    /// <summary>Verifies pre-requested cancellation throws without returning a result.</summary>
    [Fact]
    public async Task HashAsync_ThrowsWhenCancellationIsAlreadyRequested()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateHasher().HashAsync(Array.Empty<FileEntry>(), cancellationSource.Token));
    }

    private static FileHasher CreateHasher() => new(new TestLoggingService(), new TestErrorHandler());

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OpenSorSe.Hash.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string contents)
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, contents);
            return path;
        }

        public void Dispose() => Directory.Delete(Path, true);
    }

    private sealed class TestLoggingService : ILoggingService
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() { }
        public void Initialize(LogLevel minimumLevel) { }
    }

    private sealed class TestErrorHandler : IErrorHandler
    {
        public event EventHandler<ApplicationError>? ErrorReported;
        public void Report(ApplicationError applicationError) => ErrorReported?.Invoke(this, applicationError);
    }
}
