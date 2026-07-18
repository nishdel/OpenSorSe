using OpenSorSe.Executor.Models;
using OpenSorSe.Rules.Models;

namespace OpenSorSe.Executor;

/// <summary>Executes accepted planned operations sequentially and safely.</summary>
public interface IActionExecutor
{
    /// <summary>Executes the supplied operations.</summary>
    /// <param name="operations">The accepted operations to execute.</param><param name="progress">An optional progress observer.</param><param name="cancellationToken">A token used to stop future work.</param><returns>The completed or cancelled execution result.</returns>
    Task<ActionExecutionResult> ExecuteAsync(IReadOnlyCollection<PlannedOperation> operations, IProgress<ActionExecutionProgress>? progress = null, CancellationToken cancellationToken = default);
}
