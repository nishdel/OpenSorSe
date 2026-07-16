namespace TidyMind.Executor.Models;

/// <summary>Contains execution outcomes, undo records, statistics, and cancellation state.</summary>
/// <param name="Outcomes">Outcomes in attempted input order.</param><param name="UndoRecords">Undo records in successful input order.</param><param name="Statistics">Aggregate execution statistics.</param><param name="WasCancelled">Whether cancellation stopped further execution.</param>
public sealed record ActionExecutionResult(IReadOnlyList<ActionExecutionOutcome> Outcomes, IReadOnlyList<UndoRecord> UndoRecords, ActionExecutionStatistics Statistics, bool WasCancelled);
