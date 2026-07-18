namespace OpenSorSe.Executor.Models;

/// <summary>Describes the outcome state of an attempted operation.</summary>
public enum ActionExecutionStatus
{
    /// <summary>The operation completed successfully.</summary>
    Succeeded,
    /// <summary>The operation was attempted but failed.</summary>
    Failed,
    /// <summary>The operation is unsupported and was not attempted.</summary>
    Skipped,
}
