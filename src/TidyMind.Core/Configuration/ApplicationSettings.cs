using Microsoft.Extensions.Logging;

namespace TidyMind.Core.Configuration;

/// <summary>
/// Represents the validated settings available to the running application.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Gets or initializes the logging settings.
    /// </summary>
    public LoggingSettings Logging { get; init; } = new();

    /// <summary>
    /// Validates settings before they are made available to the application.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">
    /// Thrown when a required settings group is missing.
    /// </exception>
    public void Validate()
    {
        if (Logging is null)
        {
            throw new ConfigurationValidationException("Logging settings are required.");
        }
    }
}

/// <summary>
/// Defines logging-related application settings.
/// </summary>
public sealed class LoggingSettings
{
    /// <summary>
    /// Gets or initializes the lowest severity that is written to configured log outputs.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;
}
