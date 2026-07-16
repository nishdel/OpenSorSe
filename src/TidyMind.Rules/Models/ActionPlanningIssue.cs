namespace TidyMind.Rules.Models;

/// <summary>
/// Describes one recoverable planning issue for an input decision.
/// </summary>
/// <param name="DecisionIndex">The zero-based input decision position.</param>
/// <param name="FilePath">The supplied file path, if available.</param>
/// <param name="Kind">The issue category.</param>
/// <param name="Message">A user-readable issue description.</param>
public sealed record ActionPlanningIssue(int DecisionIndex, string FilePath, ActionPlanningIssueKind Kind, string Message);
