namespace OpenSorSe.Scanner.Models;

/// <summary>Describes a recoverable issue encountered while hashing one file.</summary>
public sealed record FileHashIssue(string FilePath, FileHashIssueKind Kind, string Message);
