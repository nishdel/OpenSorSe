using TidyMind.Application.Models;

namespace TidyMind.Application;

/// <summary>Routes explicit UI processing requests to application services without referencing UI types.</summary>
public interface IApplicationController
{
    /// <summary>Runs one user-approved processing request through session tracking.</summary>
    /// <param name="request">The validated processing request.</param>
    /// <param name="progress">An optional progress observer owned by the presentation layer.</param>
    /// <param name="cancellationToken">A token that cancels the processing request.</param>
    /// <returns>The terminal tracked session result.</returns>
    Task<ProcessingSessionResult> StartProcessingAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
}
