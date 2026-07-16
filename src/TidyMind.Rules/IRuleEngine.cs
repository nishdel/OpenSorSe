using TidyMind.Rules.Models;
using TidyMind.Scanner.Models;

namespace TidyMind.Rules;

/// <summary>
/// Evaluates configured rules against enriched file entries without executing actions.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evaluates the supplied files against the supplied rules in deterministic input order.
    /// </summary>
    /// <param name="files">The enriched entries to evaluate.</param>
    /// <param name="rules">The ordered rules to validate and evaluate.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The proposed decisions and evaluation statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an input collection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when inputs or rules are invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<RuleEvaluationResult> EvaluateAsync(
        IReadOnlyCollection<FileEntry> files,
        IReadOnlyList<FileRule> rules,
        CancellationToken cancellationToken = default);
}
