using TidyMind.Application.Models;

namespace TidyMind.Application;

/// <summary>Coordinates the sequential non-destructive scanner-to-conflict-resolution pipeline.</summary>
public interface IProcessingOrchestrator
{
    /// <summary>Runs supported pipeline stages in documented order.</summary>
    /// <param name="request">The explicit scan and rule inputs.</param>
    /// <param name="progress">An optional observer of stage and scanner progress.</param>
    /// <param name="cancellationToken">A token that prevents later stages from starting.</param>
    /// <returns>The reached stage outputs and final status.</returns>
    Task<ProcessingResult> ProcessAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
}
