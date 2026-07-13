namespace TidyMind.Core.Lifecycle;

/// <summary>
/// Coordinates Core infrastructure startup and graceful shutdown.
/// </summary>
public interface IApplicationHost
{
    /// <summary>
    /// Initializes shared Core services in the documented lifecycle order.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels startup before completion.</param>
    /// <returns>A task that completes when the application enters the running state.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Shuts down shared Core services in a controlled order.
    /// </summary>
    /// <param name="cancellationToken">A token that cancels shutdown before completion.</param>
    /// <returns>A task that completes when the application reaches the stopped state.</returns>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
