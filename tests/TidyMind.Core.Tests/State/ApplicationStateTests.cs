using TidyMind.Core.State;

namespace TidyMind.Core.Tests.State;

/// <summary>
/// Tests application lifecycle state transitions.
/// </summary>
public sealed class ApplicationStateTests
{
    /// <summary>
    /// Verifies that lifecycle transitions follow the documented startup sequence.
    /// </summary>
    [Fact]
    public void TryTransitionTo_AcceptsDocumentedStartupSequence()
    {
        var state = new ApplicationState();

        Assert.True(state.TryTransitionTo(ApplicationLifecycleState.Initializing));
        Assert.True(state.TryTransitionTo(ApplicationLifecycleState.Running));
        Assert.Equal(ApplicationLifecycleState.Running, state.LifecycleState);
    }

    /// <summary>
    /// Verifies that invalid lifecycle transitions leave state unchanged.
    /// </summary>
    [Fact]
    public void TryTransitionTo_RejectsInvalidTransition()
    {
        var state = new ApplicationState();

        Assert.False(state.TryTransitionTo(ApplicationLifecycleState.Running));
        Assert.Equal(ApplicationLifecycleState.Starting, state.LifecycleState);
    }
}
