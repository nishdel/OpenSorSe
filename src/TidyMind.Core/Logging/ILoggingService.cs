using Microsoft.Extensions.Logging;

namespace TidyMind.Core.Logging;

/// <summary>
/// Configures centralized logging and creates categorized loggers for application modules.
/// </summary>
public interface ILoggingService : IDisposable
{
    /// <summary>
    /// Initializes logging with the supplied minimum severity.
    /// </summary>
    /// <param name="minimumLevel">The lowest severity to write to log outputs.</param>
    void Initialize(LogLevel minimumLevel);

    /// <summary>
    /// Creates a logger for the supplied category.
    /// </summary>
    /// <param name="categoryName">The subsystem or type name used to categorize entries.</param>
    /// <returns>A categorized logger.</returns>
    ILogger CreateLogger(string categoryName);
}
