namespace OpenSorSe.Rules.Models;

/// <summary>
/// Contains planned operations, planning statistics, and recoverable planning issues.
/// </summary>
/// <param name="Operations">Successful operations in input-decision order.</param>
/// <param name="Statistics">Aggregate planning counts.</param>
/// <param name="Issues">Recoverable issues in input-decision order.</param>
public sealed record ActionPlanResult(IReadOnlyList<PlannedOperation> Operations, ActionPlanningStatistics Statistics, IReadOnlyList<ActionPlanningIssue> Issues);
