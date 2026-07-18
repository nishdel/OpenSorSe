namespace OpenSorSe.Rules.Models;

/// <summary>
/// Describes one recoverable issue for an input planned operation.
/// </summary>
/// <param name="OperationIndex">The zero-based input operation position.</param>
/// <param name="OperationId">The supplied operation identifier, if available.</param>
/// <param name="Kind">The issue category.</param>
/// <param name="Message">A user-readable description without raw exception details.</param>
/// <param name="ConflictingOperationId">The earlier accepted or input operation causing the issue, if applicable.</param>
public sealed record ConflictResolutionIssue(int OperationIndex, string OperationId, ConflictResolutionIssueKind Kind, string Message, string? ConflictingOperationId = null);
