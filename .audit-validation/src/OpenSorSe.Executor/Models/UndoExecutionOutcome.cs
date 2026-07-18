namespace OpenSorSe.Executor.Models;

/// <summary>Contains one undo outcome.</summary>
/// <param name="UndoRecordId">The record identifier.</param><param name="OperationId">The original operation identifier.</param><param name="Kind">The undo kind.</param><param name="OriginalPath">The original path.</param><param name="ResultPath">The result path.</param><param name="Status">The outcome status.</param><param name="Issue">The failure issue.</param>
public sealed record UndoExecutionOutcome(string UndoRecordId, string OperationId, UndoOperationKind Kind, string OriginalPath, string ResultPath, UndoExecutionStatus Status, UndoExecutionIssue? Issue);
