using OpenSorSe.Core.Events;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Core.Tests.Events;

/// <summary>
/// Tests in-memory event delivery behavior.
/// </summary>
public sealed class EventBusTests
{
    /// <summary>
    /// Verifies that a failing subscriber does not prevent subsequent subscribers from receiving an event.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ContinuesAfterSubscriberFailure()
    {
        using var loggingService = new LoggingService();
        var eventBus = new EventBus(loggingService);
        var handled = false;
        eventBus.Subscribe<TestEvent>((_, _) => throw new InvalidOperationException("Expected test failure."));
        eventBus.Subscribe<TestEvent>((_, _) =>
        {
            handled = true;
            return Task.CompletedTask;
        });

        await eventBus.PublishAsync(new TestEvent());

        Assert.True(handled);
    }

    private sealed class TestEvent : IApplicationEvent
    {
    }
}
