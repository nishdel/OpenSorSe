namespace TidyMind.Scanner.Models;

/// <summary>
/// Contains counts produced by a duplicate-detection operation.
/// </summary>
/// <param name="FilesProcessed">The number of input entries examined.</param>
/// <param name="FilesUnique">The number of entries assigned <see cref="DuplicateStatus.Unique"/>.</param>
/// <param name="FilesDuplicate">The number of entries assigned <see cref="DuplicateStatus.Duplicate"/>.</param>
/// <param name="FilesUnknown">The number of entries assigned <see cref="DuplicateStatus.Unknown"/>.</param>
/// <param name="DuplicateGroups">The number of returned duplicate groups.</param>
/// <param name="IssuesEncountered">The number of returned recoverable issues.</param>
public sealed record DuplicateDetectionStatistics(long FilesProcessed, long FilesUnique, long FilesDuplicate, long FilesUnknown, long DuplicateGroups, long IssuesEncountered);
