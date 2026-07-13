namespace TidyMind.Core.State;

/// <summary>
/// Supplies the previous and current values of an application lifecycle transition.
/// </summary>
public sealed class ApplicationLifecycleStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes lifecycle transition event data.
    /// </summary>
    /// <param name="previousState">The state before the transition.</param>
    /// <param name="currentState">The state after the transition.</param>
    public ApplicationLifecycleStateChangedEventArgs(
        ApplicationLifecycleState previousState,
        ApplicationLifecycleState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    /// <summary>Gets the state before the transition.</summary>
    public ApplicationLifecycleState PreviousState { get; }

    /// <summary>Gets the state after the transition.</summary>
    public ApplicationLifecycleState CurrentState { get; }
}
