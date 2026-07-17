namespace OpenSorSe.Rules.Models;

/// <summary>
/// Contains the decisions and aggregate statistics from deterministic rule evaluation.
/// </summary>
/// <param name="Decisions">One decision for every input entry in input order.</param>
/// <param name="Statistics">Aggregate evaluation counts.</param>
public sealed record RuleEvaluationResult(IReadOnlyList<RuleDecision> Decisions, RuleEvaluationStatistics Statistics);
