using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Executor.Models;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Executor.Tests;

/// <summary>Verifies safe sequential filesystem action execution.</summary>
public sealed class ActionExecutorTests
{
    /// <summary>Verifies empty input and pre-cancellation results.</summary>
    [Fact]
    public async Task ExecuteAsync_EmptyAndPreCancelled_AreDeterministic()
    {
        var empty = await CreateExecutor().ExecuteAsync(Array.Empty<PlannedOperation>());
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await CreateExecutor().ExecuteAsync(Array.Empty<PlannedOperation>(), cancellationToken: cancellation.Token);
        Assert.Equal(0L, empty.Statistics.OperationsReceived); Assert.False(empty.WasCancelled);
        Assert.True(cancelled.WasCancelled); Assert.Empty(cancelled.Outcomes);
    }

    /// <summary>Verifies collection validation happens before filesystem mutation.</summary>
    [Fact]
    public async Task ExecuteAsync_InvalidCollection_IsRejected()
    {
        IReadOnlyCollection<PlannedOperation> nullOperations = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateExecutor().ExecuteAsync(nullOperations));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateExecutor().ExecuteAsync(new List<PlannedOperation> { null! }));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateExecutor().ExecuteAsync([Operation("same", PlannedOperationKind.Delete, "C:\\a"), Operation("same", PlannedOperationKind.Delete, "C:\\b")]));
    }

    /// <summary>Verifies move, copy, rename, undo records, ordering, and copied bytes.</summary>
    [Fact]
    public async Task ExecuteAsync_SupportedOperations_SucceedWithUndoRecords()
    {
        using var directory = new TemporaryDirectory();
        var move = directory.File("move.txt", "move"); var copy = directory.File("copy.txt", "copy"); var rename = directory.File("rename.txt", "rename");
        var result = await CreateExecutor().ExecuteAsync([
            Operation("plan:0", PlannedOperationKind.Move, move, Path.Combine(directory.Path, "moved.txt")),
            Operation("plan:1", PlannedOperationKind.Copy, copy, Path.Combine(directory.Path, "copied.txt")),
            Operation("plan:2", PlannedOperationKind.Rename, rename, Path.Combine(directory.Path, "renamed.txt"))]);
        Assert.All(result.Outcomes, outcome => Assert.Equal(ActionExecutionStatus.Succeeded, outcome.Status));
        Assert.Equal(["undo:0", "undo:1", "undo:2"], result.UndoRecords.Select(record => record.UndoRecordId));
        Assert.Equal(4L, result.Statistics.BytesCopied);
    }

    /// <summary>Verifies delete is skipped and existing destinations are never overwritten.</summary>
    [Fact]
    public async Task ExecuteAsync_DeleteAndExistingDestination_AreSafe()
    {
        using var directory = new TemporaryDirectory();
        var source = directory.File("source.txt", "source"); var existing = directory.File("existing.txt", "existing");
        var result = await CreateExecutor().ExecuteAsync([Operation("delete", PlannedOperationKind.Delete, source), Operation("move", PlannedOperationKind.Move, source, existing)]);
        Assert.Equal(ActionExecutionStatus.Skipped, result.Outcomes[0].Status);
        Assert.Equal(ActionExecutionIssueKind.DestinationAlreadyExists, result.Outcomes[1].Issue!.Kind);
        Assert.Equal("existing", File.ReadAllText(existing)); Assert.True(File.Exists(source));
    }

    /// <summary>Verifies recoverable source and metadata failures do not stop a later valid operation.</summary>
    [Fact]
    public async Task ExecuteAsync_RecoverableFailure_Continues()
    {
        using var directory = new TemporaryDirectory();
        var valid = directory.File("valid.txt", "valid"); var missing = Path.Combine(directory.Path, "missing.txt");
        var result = await CreateExecutor().ExecuteAsync([Operation("missing", PlannedOperationKind.Move, missing, Path.Combine(directory.Path, "missing-target.txt")), Operation("valid", PlannedOperationKind.Copy, valid, Path.Combine(directory.Path, "valid-copy.txt"))]);
        Assert.Equal(ActionExecutionIssueKind.SourceUnavailable, result.Outcomes[0].Issue!.Kind);
        Assert.Equal(ActionExecutionStatus.Succeeded, result.Outcomes[1].Status);
    }

    /// <summary>Verifies source and destination safety validation.</summary>
    [Fact]
    public async Task ExecuteAsync_RejectsUnsafePathsAndSources()
    {
        using var directory = new TemporaryDirectory();
        var source = directory.File("source.txt", "data");
        var missingParent = Path.Combine(directory.Path, "missing", "target.txt");
        var directorySource = new PlannedOperation("directory", PlannedOperationKind.Move, new FileEntry(directory.Path), directory.Path, Path.Combine(directory.Path, "target.txt"), "rule", "Rule", 1);
        var mismatched = new PlannedOperation("mismatch", PlannedOperationKind.Move, new FileEntry(source), Path.Combine(directory.Path, "other.txt"), Path.Combine(directory.Path, "target2.txt"), "rule", "Rule", 1);
        var result = await CreateExecutor().ExecuteAsync([Operation("parent", PlannedOperationKind.Move, source, missingParent), directorySource, mismatched]);
        Assert.Equal([ActionExecutionIssueKind.DestinationDirectoryUnavailable, ActionExecutionIssueKind.SourceTypeUnsupported, ActionExecutionIssueKind.InvalidOperation], result.Outcomes.Select(outcome => outcome.Issue!.Kind));
        Assert.False(Directory.Exists(Path.Combine(directory.Path, "missing")));
    }

    /// <summary>Verifies metadata changes reject execution while missing metadata permits it.</summary>
    [Fact]
    public async Task ExecuteAsync_RevalidatesMetadataWhenAvailable()
    {
        using var directory = new TemporaryDirectory();
        var changed = directory.File("changed.txt", "data"); var free = directory.File("free.txt", "free");
        var metadata = new FileMetadata("changed.txt", ".txt", 999, DateTimeOffset.UtcNow, null, null, FileAttributes.Normal);
        var changedOperation = new PlannedOperation("changed", PlannedOperationKind.Move, new FileEntry(changed, metadata), changed, Path.Combine(directory.Path, "changed-target.txt"), "rule", "Rule", 1);
        var result = await CreateExecutor().ExecuteAsync([changedOperation, Operation("free", PlannedOperationKind.Move, free, Path.Combine(directory.Path, "free-target.txt"))]);
        Assert.Equal(ActionExecutionIssueKind.SourceChanged, result.Outcomes[0].Issue!.Kind);
        Assert.Equal(ActionExecutionStatus.Succeeded, result.Outcomes[1].Status);
    }

    /// <summary>Verifies rename refuses cross-directory and existing-destination operations.</summary>
    [Fact]
    public async Task ExecuteAsync_RenameNeverOverwritesOrCrossesDirectories()
    {
        using var directory = new TemporaryDirectory();
        var source = directory.File("source.txt", "source"); var existing = directory.File("existing.txt", "existing");
        var other = Directory.CreateDirectory(Path.Combine(directory.Path, "other")).FullName;
        var result = await CreateExecutor().ExecuteAsync([Operation("existing", PlannedOperationKind.Rename, source, existing), Operation("cross", PlannedOperationKind.Rename, source, Path.Combine(other, "source.txt"))]);
        Assert.Equal([ActionExecutionIssueKind.DestinationAlreadyExists, ActionExecutionIssueKind.InvalidOperation], result.Outcomes.Select(outcome => outcome.Issue!.Kind));
        Assert.Equal("existing", File.ReadAllText(existing));
    }

    /// <summary>Verifies progress stages, counters, and operation identity for successful execution.</summary>
    [Fact]
    public async Task ExecuteAsync_ReportsDeterministicProgress()
    {
        using var directory = new TemporaryDirectory(); var source = directory.File("source.txt", "data");
        var progress = new ProgressCollector();
        var result = await CreateExecutor().ExecuteAsync([Operation("move", PlannedOperationKind.Move, source, Path.Combine(directory.Path, "target.txt"))], progress);
        Assert.Equal(ActionExecutionProgressStage.Starting, progress.Items[0].Stage);
        Assert.Contains(progress.Items, item => item.Stage == ActionExecutionProgressStage.Executing && item.CurrentOperationId == "move" && item.CurrentOperationKind == PlannedOperationKind.Move);
        Assert.Equal(ActionExecutionProgressStage.Completed, progress.Items[^1].Stage);
        Assert.Equal(result.Statistics.OperationsSucceeded, progress.Items[^1].OperationsSucceeded);
    }

    /// <summary>Verifies cancellation between operations preserves completed work and reports cancellation.</summary>
    [Fact]
    public async Task ExecuteAsync_CancellationBetweenOperations_PreservesCompletedWork()
    {
        using var directory = new TemporaryDirectory(); var first = directory.File("first.txt", "one"); var second = directory.File("second.txt", "two");
        using var cancellation = new CancellationTokenSource();
        var progress = new CancellingProgress(cancellation);
        var result = await CreateExecutor().ExecuteAsync([Operation("first", PlannedOperationKind.Move, first, Path.Combine(directory.Path, "first-target.txt")), Operation("second", PlannedOperationKind.Move, second, Path.Combine(directory.Path, "second-target.txt"))], progress, cancellation.Token);
        Assert.True(result.WasCancelled); Assert.Single(result.Outcomes); Assert.Single(result.UndoRecords); Assert.True(File.Exists(second));
    }

    /// <summary>Verifies successful undo records retain required deterministic fields and input order.</summary>
    [Fact]
    public async Task ExecuteAsync_UndoRecords_AreCompleteAndOrdered()
    {
        using var directory = new TemporaryDirectory(); var move = directory.File("move.txt", "m"); var copy = directory.File("copy.txt", "cc");
        var before = DateTimeOffset.UtcNow;
        var result = await CreateExecutor().ExecuteAsync([Operation("move", PlannedOperationKind.Move, move, Path.Combine(directory.Path, "moved.txt")), Operation("copy", PlannedOperationKind.Copy, copy, Path.Combine(directory.Path, "copied.txt"))]);
        var after = DateTimeOffset.UtcNow;
        Assert.Equal(["undo:0", "undo:1"], result.UndoRecords.Select(record => record.UndoRecordId));
        Assert.Equal(UndoOperationKind.Move, result.UndoRecords[0].Kind); Assert.Equal("move", result.UndoRecords[0].OperationId);
        Assert.Equal(UndoOperationKind.Copy, result.UndoRecords[1].Kind); Assert.Equal("copy", result.UndoRecords[1].OperationId);
        Assert.All(result.UndoRecords, record => { Assert.Equal(TimeSpan.Zero, record.ExecutedAtUtc.Offset); Assert.InRange(record.ExecutedAtUtc, before, after); });
    }

    /// <summary>Verifies active copy cancellation returns a structured outcome and removes its owned partial destination.</summary>
    [Fact]
    public async Task ExecuteAsync_CancelledCopy_CleansOwnedDestination()
    {
        using var directory = new TemporaryDirectory(); var source = directory.File("source.bin", new string('x', 128 * 1024)); var destination = Path.Combine(directory.Path, "copy.bin");
        using var cancellation = new CancellationTokenSource(); var progress = new ProgressCollector();
        var executor = new ActionExecutor(new TestLoggingService(), new TestErrorHandler(), phase => { if (phase == CopyLoopPhase.BlockWritten) cancellation.Cancel(); });
        var result = await executor.ExecuteAsync([Operation("copy", PlannedOperationKind.Copy, source, destination)], progress, cancellation.Token);
        var outcome = Assert.Single(result.Outcomes);
        Assert.Equal(ActionExecutionStatus.Failed, outcome.Status); Assert.Equal(ActionExecutionIssueKind.CancelledDuringOperation, outcome.Issue!.Kind);
        Assert.True(result.WasCancelled); Assert.Empty(result.UndoRecords); Assert.Equal(0L, result.Statistics.BytesCopied); Assert.False(File.Exists(destination)); Assert.True(File.Exists(source)); Assert.Equal(ActionExecutionProgressStage.Cancelled, progress.Items[^1].Stage);
    }

    /// <summary>Verifies a symbolic-link source is rejected when the environment permits link creation.</summary>
    [Fact]
    public async Task ExecuteAsync_ReparsePointSource_IsRejectedWhenSupported()
    {
        using var directory = new TemporaryDirectory(); var target = directory.File("target.txt", "target"); var link = Path.Combine(directory.Path, "link.txt");
        try { File.CreateSymbolicLink(link, target); } catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or PlatformNotSupportedException) { return; }
        var destination = Path.Combine(directory.Path, "destination.txt");
        var result = await CreateExecutor().ExecuteAsync([Operation("link", PlannedOperationKind.Move, link, destination)]);
        Assert.Equal(ActionExecutionIssueKind.SourceTypeUnsupported, Assert.Single(result.Outcomes).Issue!.Kind); Assert.True(File.Exists(target)); Assert.False(File.Exists(destination));
    }

    private static ActionExecutor CreateExecutor() => new(new TestLoggingService(), new TestErrorHandler());
    private static PlannedOperation Operation(string id, PlannedOperationKind kind, string source, string? destination = null) => new(id, kind, new FileEntry(source), source, destination, "rule", "Rule", 1);
    private sealed class TemporaryDirectory : IDisposable { public TemporaryDirectory() { Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"OpenSorSe.Executor.{Guid.NewGuid():N}"); Directory.CreateDirectory(Path); } public string Path { get; } public string File(string name, string contents) { var path = System.IO.Path.Combine(Path, name); System.IO.File.WriteAllText(path, contents); return path; } public void Dispose() => Directory.Delete(Path, true); }
    private sealed class TestLoggingService : ILoggingService { public ILogger CreateLogger(string categoryName) => NullLogger.Instance; public void Dispose() { } public void Initialize(LogLevel minimumLevel) { } }
    private sealed class TestErrorHandler : IErrorHandler { public event EventHandler<ApplicationError>? ErrorReported; public void Report(ApplicationError applicationError) => ErrorReported?.Invoke(this, applicationError); }
    private sealed class ProgressCollector : IProgress<ActionExecutionProgress> { public List<ActionExecutionProgress> Items { get; } = []; public void Report(ActionExecutionProgress value) => Items.Add(value); }
    private sealed class CancellingProgress(CancellationTokenSource source) : IProgress<ActionExecutionProgress> { public void Report(ActionExecutionProgress value) { if (value.Stage == ActionExecutionProgressStage.Completed && value.OperationsSucceeded == 1) source.Cancel(); } }
}
