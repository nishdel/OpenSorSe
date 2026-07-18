namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Describes a recoverable problem encountered while enriching one file.
/// </summary>
public sealed record FileMetadataIssue(string FilePath, FileMetadataIssueKind Kind, string Message);
