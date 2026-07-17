namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Contains the enriched entries, duplicate groups, statistics, and recoverable issues from duplicate detection.
/// </summary>
/// <param name="Files">Entries in exactly the supplied input order.</param>
/// <param name="Groups">Duplicate groups ordered by first hash appearance.</param>
/// <param name="Statistics">Aggregate detection statistics.</param>
/// <param name="Issues">Recoverable hash-validation issues.</param>
public sealed record DuplicateDetectionResult(IReadOnlyList<FileEntry> Files, IReadOnlyList<DuplicateGroup> Groups, DuplicateDetectionStatistics Statistics, IReadOnlyList<DuplicateDetectionIssue> Issues);
