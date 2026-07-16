using TidyMind.Scanner.Models;

namespace TidyMind.Rules.Models;

/// <summary>
/// Contains the winning proposed action and all matching rule identifiers for an input file entry.
/// </summary>
/// <param name="File">The original immutable input entry.</param>
/// <param name="Action">The selected action or <see cref="RuleActionKind.NoAction"/>.</param>
/// <param name="SelectedRuleId">The selected rule identifier, if any.</param>
/// <param name="SelectedRuleName">The selected rule name, if any.</param>
/// <param name="SelectedRulePriority">The selected rule priority, if any.</param>
/// <param name="MatchingRuleIds">All enabled matching rule identifiers in supplied order.</param>
public sealed record RuleDecision(FileEntry File, RuleAction Action, string? SelectedRuleId, string? SelectedRuleName, int? SelectedRulePriority, IReadOnlyList<string> MatchingRuleIds);
