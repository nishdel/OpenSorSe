namespace TidyMind.Executor.Models;

/// <summary>Identifies a progress-reporting stage.</summary>
public enum ActionExecutionProgressStage
{
    /// <summary>Execution is starting.</summary>
    Starting,
    /// <summary>An operation is about to execute.</summary>
    Executing,
    /// <summary>Execution completed normally.</summary>
    Completed,
    /// <summary>Cancellation stopped further execution.</summary>
    Cancelled,
}
