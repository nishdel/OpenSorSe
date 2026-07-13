namespace TidyMind.Core.State;

/// <summary>
/// Stores and validates application lifecycle state transitions.
/// </summary>
public sealed class ApplicationState : IApplicationState
{
    private readonly object _syncRoot = new();
    private ApplicationLifecycleState _lifecycleState = ApplicationLifecycleState.Starting;

    /// <inheritdoc />
    public ApplicationLifecycleState LifecycleState
    {
        get
        {
            lock (_syncRoot)
            {
                return _lifecycleState;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ApplicationLifecycleStateChangedEventArgs>? LifecycleStateChanged;

    /// <inheritdoc />
    public bool TryTransitionTo(ApplicationLifecycleState newState)
    {
        ApplicationLifecycleState previousState;
        lock (_syncRoot)
        {
            previousState = _lifecycleState;
            if (!IsValidTransition(previousState, newState))
            {
                return false;
            }

            _lifecycleState = newState;
        }

        LifecycleStateChanged?.Invoke(
            this,
            new ApplicationLifecycleStateChangedEventArgs(previousState, newState));
        return true;
    }

    private static bool IsValidTransition(
        ApplicationLifecycleState currentState,
        ApplicationLifecycleState newState)
    {
        return (currentState, newState) switch
        {
            (ApplicationLifecycleState.Starting, ApplicationLifecycleState.Initializing) => true,
            (ApplicationLifecycleState.Starting, ApplicationLifecycleState.Stopped) => true,
            (ApplicationLifecycleState.Initializing, ApplicationLifecycleState.Running) => true,
            (ApplicationLifecycleState.Initializing, ApplicationLifecycleState.Stopped) => true,
            (ApplicationLifecycleState.Running, ApplicationLifecycleState.ShuttingDown) => true,
            (ApplicationLifecycleState.ShuttingDown, ApplicationLifecycleState.Stopped) => true,
            _ => false,
        };
    }
}
