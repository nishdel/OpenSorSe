namespace TidyMind.Rules.Models;

/// <summary>
/// Contains counts produced by a rule-evaluation operation.
/// </summary>
/// <param name="FilesProcessed">The number of input files evaluated.</param>
/// <param name="FilesWithMatches">The number of files matching one or more enabled rules.</param>
/// <param name="FilesWithoutMatches">The number of files matching no enabled rule.</param>
/// <param name="RulesEvaluated">The total enabled rule evaluations across files.</param>
/// <param name="RuleMatches">The total successful enabled rule matches across files.</param>
public sealed record RuleEvaluationStatistics(long FilesProcessed, long FilesWithMatches, long FilesWithoutMatches, long RulesEvaluated, long RuleMatches);
