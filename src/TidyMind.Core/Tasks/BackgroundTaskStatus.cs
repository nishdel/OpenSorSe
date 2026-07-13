namespace TidyMind.Core.Tasks;

/// <summary>
/// Describes the lifecycle status of a managed background operation.
/// </summary>
public enum BackgroundTaskStatus
{
    /// <summary>The operation is registered but has not started.</summary>
    Pending,

    /// <summary>The operation is currently executing.</summary>
    Running,

    /// <summary>The operation completed successfully.</summary>
    Completed,

    /// <summary>The operation stopped after cancellation was requested.</summary>
    Cancelled,

    /// <summary>The operation ended because of an unhandled failure.</summary>
    Failed,
}
