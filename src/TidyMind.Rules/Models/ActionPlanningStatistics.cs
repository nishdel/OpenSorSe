namespace TidyMind.Rules.Models;

/// <summary>
/// Contains counts produced while planning proposed actions.
/// </summary>
/// <param name="DecisionsProcessed">The number of input decisions examined.</param>
/// <param name="OperationsPlanned">The number of returned operations.</param>
/// <param name="DecisionsWithNoAction">The number of no-action decisions.</param>
/// <param name="DecisionsFailed">The number of decisions producing an issue.</param>
/// <param name="MoveOperations">The number of planned moves.</param>
/// <param name="CopyOperations">The number of planned copies.</param>
/// <param name="RenameOperations">The number of planned renames.</param>
/// <param name="DeleteOperations">The number of planned deletes.</param>
/// <param name="IssuesEncountered">The number of returned issues.</param>
public sealed record ActionPlanningStatistics(long DecisionsProcessed, long OperationsPlanned, long DecisionsWithNoAction, long DecisionsFailed, long MoveOperations, long CopyOperations, long RenameOperations, long DeleteOperations, long IssuesEncountered);
