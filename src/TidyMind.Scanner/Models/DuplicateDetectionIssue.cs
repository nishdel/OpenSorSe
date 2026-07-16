namespace TidyMind.Scanner.Models;

/// <summary>
/// Describes one recoverable duplicate-detection issue for a file entry.
/// </summary>
/// <param name="FilePath">The entry path exactly as supplied.</param>
/// <param name="Kind">The issue category.</param>
/// <param name="Message">A user-readable description of the issue.</param>
public sealed record DuplicateDetectionIssue(string FilePath, DuplicateDetectionIssueKind Kind, string Message);
