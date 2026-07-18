namespace OpenSorSe.Executor.Models;

/// <summary>Describes one user-safe execution issue.</summary>
/// <param name="OperationId">The affected operation identifier.</param>
/// <param name="Kind">The issue category.</param>
/// <param name="Message">A user-safe message without raw exception details.</param>
public sealed record ActionExecutionIssue(string OperationId, ActionExecutionIssueKind Kind, string Message);
