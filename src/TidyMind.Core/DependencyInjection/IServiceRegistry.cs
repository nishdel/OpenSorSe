namespace TidyMind.Core.DependencyInjection;

/// <summary>
/// Provides centralized discovery of shared services registered during application composition.
/// </summary>
public interface IServiceRegistry
{
    /// <summary>
    /// Gets a required registered service.
    /// </summary>
    /// <typeparam name="TService">The registered service type.</typeparam>
    /// <returns>The registered service instance.</returns>
    TService GetRequiredService<TService>() where TService : notnull;

    /// <summary>
    /// Determines whether a service has been registered.
    /// </summary>
    /// <typeparam name="TService">The service type to look up.</typeparam>
    /// <returns><see langword="true"/> when the service is available; otherwise, <see langword="false"/>.</returns>
    bool IsRegistered<TService>() where TService : notnull;
}
