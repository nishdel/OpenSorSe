namespace OpenSorSe.Core.Events;

/// <summary>
/// Delivers application events to independent subscribers.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Registers an asynchronous handler for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The immutable event type to observe.</typeparam>
    /// <param name="handler">The handler to invoke for each published event.</param>
    /// <returns>A subscription that removes the handler when disposed.</returns>
    IDisposable Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
        where TEvent : IApplicationEvent;

    /// <summary>
    /// Publishes an event to all current subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The event type to publish.</typeparam>
    /// <param name="applicationEvent">The event that has occurred.</param>
    /// <param name="cancellationToken">A token that cancels delivery before the next handler.</param>
    /// <returns>A task that completes when all handlers have been offered the event.</returns>
    Task PublishAsync<TEvent>(TEvent applicationEvent, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent;
}
