namespace TidyMind.Application.Models;

/// <summary>Describes the in-memory lifecycle state of one processing session.</summary>
public enum ProcessingSessionStatus
{
    /// <summary>The session is currently running its processing request.</summary>
    Running,

    /// <summary>The processing pipeline completed.</summary>
    Completed,

    /// <summary>Cancellation stopped the processing pipeline.</summary>
    Cancelled,

    /// <summary>An unexpected processing failure ended the session.</summary>
    Failed,

    /// <summary>A terminal session was explicitly closed from in-memory tracking.</summary>
    Closed,
}
