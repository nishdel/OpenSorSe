namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Summarizes the entries and recoverable issues encountered during a scan.
/// </summary>
public sealed record ScanStatistics(
    long FilesDiscovered,
    long DirectoriesDiscovered,
    long IssuesEncountered);
