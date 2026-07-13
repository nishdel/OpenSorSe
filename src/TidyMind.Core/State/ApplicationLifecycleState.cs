namespace TidyMind.Core.State;

/// <summary>
/// Describes the lifecycle stage of the running application.
/// </summary>
public enum ApplicationLifecycleState
{
    /// <summary>The executable has started but infrastructure is not ready.</summary>
    Starting,

    /// <summary>Core infrastructure is being initialized.</summary>
    Initializing,

    /// <summary>The application is ready to accept normal work.</summary>
    Running,

    /// <summary>The application is releasing resources and stopping work.</summary>
    ShuttingDown,

    /// <summary>The application has completed shutdown.</summary>
    Stopped,
}
