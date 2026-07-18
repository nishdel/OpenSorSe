namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Contains enriched file entries and recoverable metadata processing issues.
/// </summary>
public sealed record FileMetadataResult(
    IReadOnlyList<FileEntry> Files,
    FileMetadataStatistics Statistics,
    IReadOnlyList<FileMetadataIssue> Issues);
