namespace TidyMind.Rules.Models;

/// <summary>
/// Describes an unevaluated, non-executed action proposed by a file rule.
/// </summary>
/// <param name="Kind">The proposed action kind.</param>
/// <param name="DestinationPath">The unexpanded destination path for move or copy actions.</param>
/// <param name="NameTemplate">The unexpanded name template for rename actions.</param>
public sealed record RuleAction(RuleActionKind Kind, string? DestinationPath = null, string? NameTemplate = null);
