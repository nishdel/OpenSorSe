namespace TidyMind.Rules.Models;

/// <summary>
/// Identifies a recoverable intra-plan conflict-resolution issue.
/// </summary>
public enum ConflictResolutionIssueKind
{
    /// <summary>The operation does not meet the required lexical contract.</summary>
    InvalidOperation,
    /// <summary>The operation identifier was already used by an earlier input operation.</summary>
    DuplicateOperationId,
    /// <summary>The operation duplicates an earlier accepted operation signature.</summary>
    DuplicateOperation,
    /// <summary>The operation targets a destination already owned by an accepted operation.</summary>
    DestinationConflict,
    /// <summary>The operation has an unsafe source relationship with an accepted operation.</summary>
    SourceConflict,
}
