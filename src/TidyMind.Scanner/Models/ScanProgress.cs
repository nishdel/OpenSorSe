namespace TidyMind.Scanner.Models;

/// <summary>
/// Provides a point-in-time view of work completed by an active scan.
/// </summary>
public sealed record ScanProgress(
    string? CurrentPath,
    ScanStatistics Statistics,
    TimeSpan Elapsed);
