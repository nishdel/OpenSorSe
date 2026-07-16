namespace TidyMind.Executor.Models;

/// <summary>Identifies a user-safe per-operation execution failure.</summary>
public enum ActionExecutionIssueKind
{
    /// <summary>The operation is structurally invalid.</summary>
    InvalidOperation,
    /// <summary>The operation kind is unsupported in v0.1.</summary>
    UnsupportedOperation,
    /// <summary>The source is unavailable.</summary>
    SourceUnavailable,
    /// <summary>The source is not a regular file.</summary>
    SourceTypeUnsupported,
    /// <summary>The destination parent directory is unavailable.</summary>
    DestinationDirectoryUnavailable,
    /// <summary>The destination already exists.</summary>
    DestinationAlreadyExists,
    /// <summary>Filesystem access was denied.</summary>
    AccessDenied,
    /// <summary>The source differs from available planned metadata.</summary>
    SourceChanged,
    /// <summary>An input/output operation failed.</summary>
    IoFailure,
    /// <summary>Cancellation interrupted an active copy operation.</summary>
    CancelledDuringOperation,
}
