using OpenSorSe.Rules.Models;

namespace OpenSorSe.Rules;

/// <summary>
/// Converts rule decisions into immutable operations without executing filesystem changes.
/// </summary>
public interface IActionPlanner
{
    /// <summary>
    /// Plans supported actions from the supplied decisions in input order.
    /// </summary>
    /// <param name="decisions">The rule decisions to plan.</param>
    /// <param name="cancellationToken">A token used to cancel planning.</param>
    /// <returns>The operations, statistics, and recoverable issues.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="decisions"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the collection contains a null decision.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<ActionPlanResult> PlanAsync(IReadOnlyCollection<RuleDecision> decisions, CancellationToken cancellationToken = default);
}
