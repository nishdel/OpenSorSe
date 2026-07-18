namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Summarizes work completed by file metadata processing.
/// </summary>
public sealed record FileMetadataStatistics(long FilesProcessed, long FilesEnriched, long IssuesEncountered);
