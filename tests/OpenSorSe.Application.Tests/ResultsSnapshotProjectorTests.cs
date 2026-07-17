using OpenSorSe.Application;
using OpenSorSe.Application.Models;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Tests;

/// <summary>
/// Verifies immutable, read-only projection of completed processing output.
/// </summary>
public sealed class ResultsSnapshotProjectorTests
{
    /// <summary>
    /// Verifies projection preserves source order, hides raw hash values, maps operations, and calculates conservative duplicate space.
    /// </summary>
    [Fact]
    public void Project_CompletedResult_ProducesDeterministicImmutableSnapshot()
    {
        var first = CreateFile("C:\\One\\same.txt", 4, DuplicateStatus.Duplicate, "sha256:abcdef");
        var second = CreateFile("C:\\Two\\same.txt", 4, DuplicateStatus.Duplicate, "sha256:abcdef");
        var processing = CreateProcessing([first, second], includeDuplicates: true, includeConflicts: true);
        var session = new ProcessingSessionResult(
            new ProcessingSession("session:test", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ProcessingSessionStatus.Completed, null),
            processing);
        var projector = new ResultsSnapshotProjector(new FixedTimeProvider(DateTimeOffset.UnixEpoch.AddDays(1)));

        var snapshot = projector.Project(session);

        Assert.Equal(DateTimeOffset.UnixEpoch.AddDays(1), snapshot.ProjectedAtUtc);
        Assert.Equal(["same.txt", "same.txt"], snapshot.Files.Select(file => file.DisplayFileName));
        Assert.Equal(["file:00000000", "file:00000001"], snapshot.Files.Select(file => file.Id));
        Assert.All(snapshot.Files, file => Assert.DoesNotContain("abcdef", file.Id, StringComparison.Ordinal));
        var group = Assert.Single(snapshot.DuplicateGroups);
        Assert.Equal(1, group.Ordinal);
        Assert.Equal(4, group.CommonFileSizeInBytes);
        Assert.Equal(4, group.PotentialReclaimableBytes);
        Assert.Equal(["file:00000000", "file:00000001"], group.MemberFileIds);
        Assert.True(snapshot.Files[0].HasPlannedOperation);
        Assert.Single(snapshot.PlannedOperations);
        Assert.Equal("Rule", snapshot.PlannedOperations[0].RuleDisplayName);
        Assert.Equal(2, snapshot.Statistics.ExactDuplicateFileCount);
        Assert.Equal("C:\\One\\same.txt", first.FullPath);
        Assert.Throws<NotSupportedException>(() => ((IList<ResultFile>)snapshot.Files).Add(snapshot.Files[0]));
    }

    /// <summary>
    /// Verifies completed results without detector or conflict output remain reviewable with explicit limitations.
    /// </summary>
    [Fact]
    public void Project_CompletedResultWithUnavailableOptionalOutputs_RecordsSafeWarnings()
    {
        var processing = CreateProcessing([CreateFile("C:\\Only\\entry.txt", null, DuplicateStatus.Unknown, null)], includeDuplicates: false, includeConflicts: false);
        var session = new ProcessingSessionResult(
            new ProcessingSession("session:test", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ProcessingSessionStatus.Completed, null),
            processing);

        var snapshot = new ResultsSnapshotProjector().Project(session);

        Assert.False(snapshot.IsDuplicateDataAvailable);
        Assert.Empty(snapshot.DuplicateGroups);
        Assert.Empty(snapshot.PlannedOperations);
        Assert.Contains(snapshot.Issues, issue => issue.Message.Contains("duplicate review was unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.Issues, issue => issue.Message.Contains("planned-operation review was unavailable", StringComparison.OrdinalIgnoreCase));
        Assert.Single(snapshot.Files);
    }

    /// <summary>
    /// Verifies incomplete terminal states cannot produce a partial review snapshot.
    /// </summary>
    [Fact]
    public void Project_NonCompletedSession_RejectsInput()
    {
        var session = new ProcessingSessionResult(
            new ProcessingSession("session:cancelled", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, ProcessingSessionStatus.Cancelled, null),
            null);

        Assert.Throws<ArgumentException>(() => new ResultsSnapshotProjector().Project(session));
    }

    private static ProcessingResult CreateProcessing(IReadOnlyList<FileEntry> files, bool includeDuplicates, bool includeConflicts)
    {
        var scan = new ScanResult(
            files,
            [new DirectoryEntry("C:\\")],
            new ScanStatistics(files.Count, 1, files.Count),
            [new ScanIssue("C:\\Skipped", ScanIssueKind.DirectoryUnavailable, "A test scan warning.")],
            ScanStatus.Completed,
            TimeSpan.Zero);
        var duplicates = includeDuplicates
            ? new DuplicateDetectionResult(
                files,
                [new DuplicateGroup("sha256:abcdef", "SHA256", "abcdef", files)],
                new DuplicateDetectionStatistics(files.Count, files.Count, 1, 2, 0, 0),
                [])
            : null;
        var conflicts = includeConflicts
            ? new ConflictResolutionResult(
                [new PlannedOperation("plan:0", PlannedOperationKind.Move, files[0], files[0].FullPath, "C:\\Destination\\same.txt", "rule", "Rule", 1)],
                new ConflictResolutionStatistics(1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
                [])
            : null;
        return new ProcessingResult(ProcessingStatus.Completed, scan, null, null, null, duplicates, null, null, conflicts);
    }

    private static FileEntry CreateFile(string path, long? size, DuplicateStatus status, string? groupId) =>
        new(
            path,
            new FileMetadata(Path.GetFileName(path), Path.GetExtension(path), size, null, DateTimeOffset.UnixEpoch, null, FileAttributes.Normal),
            Duplicate: new DuplicateClassification(status, groupId));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow() => now;
    }
}
