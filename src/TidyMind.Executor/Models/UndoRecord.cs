namespace TidyMind.Executor.Models;

/// <summary>Records information needed for a future undo operation.</summary>
/// <param name="UndoRecordId">The deterministic undo record identifier.</param>
/// <param name="OperationId">The completed operation identifier.</param>
/// <param name="Kind">The reverse operation kind.</param>
/// <param name="OriginalPath">The pre-execution source path.</param>
/// <param name="ResultPath">The post-execution result path.</param>
/// <param name="ExecutedAtUtc">The UTC completion time.</param>
public sealed record UndoRecord(string UndoRecordId, string OperationId, UndoOperationKind Kind, string OriginalPath, string ResultPath, DateTimeOffset ExecutedAtUtc);
