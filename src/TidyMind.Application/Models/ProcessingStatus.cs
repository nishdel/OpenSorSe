namespace TidyMind.Application.Models;

/// <summary>Describes the terminal state of one sequential processing pipeline run.</summary>
public enum ProcessingStatus
{
    /// <summary>All v0.1 pipeline stages completed.</summary>
    Completed,
    /// <summary>Cancellation stopped later pipeline stages.</summary>
    Cancelled,
}
