namespace TidyMind.Scanner.Models;

/// <summary>
/// Identifies a recoverable classification issue.
/// </summary>
public enum FileClassificationIssueKind
{
    /// <summary>The file entry did not contain metadata required for classification.</summary>
    MetadataUnavailable,
}
