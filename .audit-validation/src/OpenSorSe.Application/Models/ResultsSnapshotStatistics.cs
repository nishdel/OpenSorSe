namespace OpenSorSe.Application.Models;

/// <summary>
/// Contains aggregate values derived from one immutable results snapshot.
/// </summary>
public sealed record ResultsSnapshotStatistics(
    long FilesDiscovered,
    long DirectoriesDiscovered,
    long ExactDuplicateGroupCount,
    long ExactDuplicateFileCount,
    long PlannedOperationCount,
    long WarningCount,
    long ErrorCount);
