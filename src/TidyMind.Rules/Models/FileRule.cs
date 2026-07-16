namespace TidyMind.Rules.Models;

/// <summary>
/// Defines an ordered, priority-based rule evaluated against a file entry.
/// </summary>
/// <param name="Id">The case-insensitively unique rule identifier.</param>
/// <param name="Name">The human-readable rule name.</param>
/// <param name="Priority">The precedence value used after matching.</param>
/// <param name="Conditions">The non-empty AND-combined conditions.</param>
/// <param name="Action">The proposed, non-executed action.</param>
/// <param name="IsEnabled">Whether the rule participates in evaluation.</param>
public sealed record FileRule(string Id, string Name, int Priority, IReadOnlyList<RuleCondition> Conditions, RuleAction Action, bool IsEnabled = true);
