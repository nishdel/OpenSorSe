namespace OpenSorSe.Executor.Models;
/// <summary>Identifies undo progress stages.</summary>
public enum UndoExecutionProgressStage
{
    /// <summary>Undo is starting.</summary>
    Starting,

    /// <summary>A record is executing.</summary>
    Executing,

    /// <summary>Undo completed.</summary>
    Completed,

    /// <summary>Cancellation stopped undo.</summary>
    Cancelled,
}
