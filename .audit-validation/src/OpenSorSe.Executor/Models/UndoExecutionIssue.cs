namespace OpenSorSe.Executor.Models;

/// <summary>Describes one undo issue.</summary>
/// <param name="UndoRecordId">The record identifier.</param><param name="Kind">The issue kind.</param><param name="Message">A user-safe message.</param>
public sealed record UndoExecutionIssue(string UndoRecordId, UndoExecutionIssueKind Kind, string Message);
