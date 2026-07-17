using Microsoft.Extensions.Logging;

namespace OpenSorSe.Core.Configuration;

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

        Logging.Validate();
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

    /// <summary>
    /// Gets or initializes whether local daily text logging is enabled.
    /// </summary>
    public bool FileLoggingEnabled { get; init; } = true;

    /// <summary>
    /// Gets or initializes the optional absolute directory for local daily log files.
    /// </summary>
    public string? LogDirectoryPath { get; init; }

    /// <summary>
    /// Gets or initializes the number of daily log files retained locally.
    /// </summary>
    public int RetainedFileCount { get; init; } = 7;

    /// <summary>
    /// Validates logging-specific settings.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">Thrown when a logging setting is invalid.</exception>
    public void Validate()
    {
        if (!Enum.IsDefined(MinimumLevel) || RetainedFileCount < 1 ||
            LogDirectoryPath is not null &&
            (string.IsNullOrWhiteSpace(LogDirectoryPath) ||
             FileLoggingEnabled && !Path.IsPathRooted(LogDirectoryPath)))
        {
            throw new ConfigurationValidationException("Logging settings are invalid.");
        }
    }
}
