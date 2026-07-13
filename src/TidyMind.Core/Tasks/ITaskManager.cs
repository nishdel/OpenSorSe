namespace TidyMind.Core.Tasks;

/// <summary>
/// Coordinates non-blocking background operations, progress reporting, and cancellation.
/// </summary>
public interface ITaskManager
{
    /// <summary>
    /// Gets snapshots of operations that are currently pending or running.
    /// </summary>
    IReadOnlyCollection<BackgroundTaskSnapshot> ActiveTasks { get; }

    /// <summary>
    /// Occurs when a managed task changes status or reports progress.
    /// </summary>
    event Action<BackgroundTaskSnapshot>? TaskChanged;

    /// <summary>
    /// Runs an operation on the thread pool and tracks its lifecycle.
    /// </summary>
    /// <param name="name">A descriptive task name.</param>
    /// <param name="operation">The work to execute.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task containing the final operation snapshot.</returns>
    Task<BackgroundTaskSnapshot> RunAsync(
        string name,
        Func<CancellationToken, IProgress<double>, Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests cancellation for an active operation.
    /// </summary>
    /// <param name="taskId">The task to cancel.</param>
    /// <returns><see langword="true"/> when a task was active and cancellation was requested.</returns>
    bool TryCancel(Guid taskId);
}
