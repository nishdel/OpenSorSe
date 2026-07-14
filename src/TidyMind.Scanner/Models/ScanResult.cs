namespace TidyMind.Scanner.Models;

/// <summary>
/// Contains all entries and recoverable issues produced by one scan operation.
/// </summary>
public sealed record ScanResult(
    IReadOnlyList<FileEntry> Files,
    IReadOnlyList<DirectoryEntry> Directories,
    ScanStatistics Statistics,
    IReadOnlyList<ScanIssue> Issues,
    ScanStatus Status,
    TimeSpan Elapsed);
