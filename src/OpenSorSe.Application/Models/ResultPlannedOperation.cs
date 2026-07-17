using OpenSorSe.Rules.Models;

namespace OpenSorSe.Application.Models;

/// <summary>
/// Represents one accepted planned operation for display only.
/// </summary>
public sealed record ResultPlannedOperation(
    string OperationId,
    PlannedOperationKind Kind,
    string? SourceFileId,
    string? DestinationPath,
    string? RuleDisplayName);
