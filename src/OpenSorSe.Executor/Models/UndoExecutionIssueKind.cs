namespace OpenSorSe.Executor.Models;

/// <summary>Identifies a user-safe undo failure.</summary>
public enum UndoExecutionIssueKind
{
    /// <summary>The record is invalid.</summary>
    InvalidRecord,

    /// <summary>The undo kind is unsupported.</summary>
    UnsupportedUndoKind,

    /// <summary>The result file is unavailable.</summary>
    ResultUnavailable,

    /// <summary>The result is not a regular file.</summary>
    ResultTypeUnsupported,

    /// <summary>The original parent is unavailable.</summary>
    OriginalParentUnavailable,

    /// <summary>The original path is occupied.</summary>
    OriginalPathOccupied,

    /// <summary>The record paths are incompatible.</summary>
    PathMismatch,

    /// <summary>Access was denied.</summary>
    AccessDenied,

    /// <summary>An I/O operation failed.</summary>
    IoFailure,
}
