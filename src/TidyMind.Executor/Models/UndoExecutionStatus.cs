namespace TidyMind.Executor.Models;

/// <summary>Describes an undo attempt outcome.</summary>
public enum UndoExecutionStatus
{
    /// <summary>The undo succeeded.</summary>
    Succeeded,

    /// <summary>The undo failed.</summary>
    Failed,
}
