using OpenSorSe.Application.Models;

namespace OpenSorSe.Application;

/// <summary>Creates and tracks non-persistent processing sessions around the orchestrator.</summary>
public interface IProcessingSessionManager
{
    /// <summary>Gets current sessions in creation order.</summary>
    IReadOnlyList<ProcessingSession> Sessions { get; }
    /// <summary>Occurs after a session snapshot changes.</summary>
    event EventHandler<ProcessingSession>? SessionChanged;
    /// <summary>Creates and runs one session for the supplied processing request.</summary>
    /// <param name="request">The explicit processing request.</param>
    /// <param name="progress">Optional pipeline progress observer.</param>
    /// <param name="cancellationToken">A token that cancels the processing request.</param>
    /// <returns>The terminal session and reached processing result.</returns>
    Task<ProcessingSessionResult> RunAsync(ProcessingRequest request, IProgress<ProcessingProgress>? progress = null, CancellationToken cancellationToken = default);
    /// <summary>Closes one terminal in-memory session.</summary>
    /// <param name="sessionId">The unique session identifier.</param>
    /// <returns><see langword="true"/> when a terminal session was closed.</returns>
    bool TryClose(string sessionId);
}
