using TidyMind.Rules.Models;

namespace TidyMind.Executor.Models;

/// <summary>Reports aggregate progress without exposing a fabricated percentage.</summary>
/// <param name="Stage">The current stage.</param><param name="OperationsReceived">Input operation count.</param><param name="OperationsAttempted">Attempted operation count.</param><param name="OperationsSucceeded">Successful operation count.</param><param name="OperationsFailed">Failed operation count.</param><param name="OperationsSkipped">Skipped operation count.</param><param name="CurrentOperationId">The active operation identifier, if any.</param><param name="CurrentOperationKind">The active operation kind, if any.</param>
public sealed record ActionExecutionProgress(ActionExecutionProgressStage Stage, long OperationsReceived, long OperationsAttempted, long OperationsSucceeded, long OperationsFailed, long OperationsSkipped, string? CurrentOperationId, PlannedOperationKind? CurrentOperationKind);
