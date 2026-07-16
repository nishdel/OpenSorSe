using TidyMind.Rules.Models;

namespace TidyMind.Executor.Models;

/// <summary>Contains the outcome of one planned operation.</summary>
/// <param name="OperationId">The operation identifier.</param><param name="Kind">The operation kind.</param><param name="SourcePath">The source path.</param><param name="DestinationPath">The destination path, if any.</param><param name="Status">The outcome status.</param><param name="UndoRecordId">The created undo record identifier on success.</param><param name="Issue">The issue on failure or skip.</param>
public sealed record ActionExecutionOutcome(string OperationId, PlannedOperationKind Kind, string SourcePath, string? DestinationPath, ActionExecutionStatus Status, string? UndoRecordId, ActionExecutionIssue? Issue);
