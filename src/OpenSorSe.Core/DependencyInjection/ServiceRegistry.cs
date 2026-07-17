namespace OpenSorSe.Core.DependencyInjection;

/// <summary>
/// Adapts the .NET dependency injection provider to the Core service registry contract.
/// </summary>
public sealed class ServiceRegistry : IServiceRegistry
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a registry over the composed application service provider.
    /// </summary>
    /// <param name="serviceProvider">The application service provider.</param>
    public ServiceRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public TService GetRequiredService<TService>() where TService : notnull
    {
        return _serviceProvider.GetService(typeof(TService)) is TService service
            ? service
            : throw new InvalidOperationException($"{typeof(TService).Name} is not registered.");
    }

    /// <inheritdoc />
    public bool IsRegistered<TService>() where TService : notnull
    {
        return _serviceProvider.GetService(typeof(TService)) is not null;
    }
}
