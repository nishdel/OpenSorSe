using Microsoft.Extensions.DependencyInjection;
using TidyMind.Core.Configuration;
using TidyMind.Core.Errors;
using TidyMind.Core.Events;
using TidyMind.Core.Lifecycle;
using TidyMind.Core.Logging;
using TidyMind.Core.State;
using TidyMind.Core.Tasks;

namespace TidyMind.Core.DependencyInjection;

/// <summary>
/// Registers the shared Core infrastructure with the .NET dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the documented shared Core foundation as singleton services.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="options">The composition settings for Core infrastructure.</param>
    /// <returns>The same collection to support fluent composition.</returns>
    public static IServiceCollection AddTidyMindCore(
        this IServiceCollection services,
        TidyMindCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.ConfigurationFilePath);

        if (services.Any(descriptor => descriptor.ServiceType == typeof(IApplicationHost)))
        {
            throw new InvalidOperationException("TidyMind Core services have already been registered.");
        }

        services.AddSingleton(options);
        services.AddSingleton<IConfigurationService>(serviceProvider =>
            new JsonConfigurationService(
                serviceProvider.GetRequiredService<TidyMindCoreOptions>().ConfigurationFilePath));
        services.AddSingleton<ILoggingService, LoggingService>();
        services.AddSingleton<IApplicationState, ApplicationState>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddSingleton<ITaskManager, TaskManager>();
        services.AddSingleton<IApplicationHost, ApplicationHost>();
        services.AddSingleton<IServiceRegistry, ServiceRegistry>();
        return services;
    }
}
