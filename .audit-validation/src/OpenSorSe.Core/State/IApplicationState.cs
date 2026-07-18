namespace OpenSorSe.Core.State;

/// <summary>
/// Provides the authoritative, in-memory state of the application lifecycle.
/// </summary>
public interface IApplicationState
{
    /// <summary>
    /// Gets the current application lifecycle state.
    /// </summary>
    ApplicationLifecycleState LifecycleState { get; }

    /// <summary>
    /// Occurs after a valid lifecycle transition.
    /// </summary>
    event EventHandler<ApplicationLifecycleStateChangedEventArgs>? LifecycleStateChanged;

    /// <summary>
    /// Attempts a valid transition to a new lifecycle state.
    /// </summary>
    /// <param name="newState">The requested lifecycle state.</param>
    /// <returns><see langword="true"/> when the transition succeeds; otherwise, <see langword="false"/>.</returns>
    bool TryTransitionTo(ApplicationLifecycleState newState);
}
