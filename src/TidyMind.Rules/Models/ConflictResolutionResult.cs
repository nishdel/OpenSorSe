namespace TidyMind.Rules.Models;

/// <summary>
/// Contains accepted operations, statistics, and recoverable intra-plan issues.
/// </summary>
/// <param name="Operations">Accepted original operations in input order.</param>
/// <param name="Statistics">Aggregate conflict-resolution counts.</param>
/// <param name="Issues">Recoverable issues in input order.</param>
public sealed record ConflictResolutionResult(IReadOnlyList<PlannedOperation> Operations, ConflictResolutionStatistics Statistics, IReadOnlyList<ConflictResolutionIssue> Issues);
