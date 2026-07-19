using Microsoft.Extensions.Logging;

namespace OpenSorSe.Core.Configuration;

/// <summary>
/// Represents the validated settings available to the running application.
/// </summary>
public sealed class ApplicationSettings
{
    /// <summary>
    /// Gets or initializes application-wide feature-presentation settings.
    /// </summary>
    public FeatureSettings Features { get; init; } = new();

    /// <summary>
    /// Gets or initializes the logging settings.
    /// </summary>
    public LoggingSettings Logging { get; init; } = new();

    /// <summary>
    /// Gets or initializes the optional local AI-provider settings.
    /// </summary>
    public AiSettings Ai { get; init; } = new();

    /// <summary>
    /// Gets or initializes settings for the opt-in local results catalog.
    /// </summary>
    public CatalogSettings Catalog { get; init; } = new();

    /// <summary>
    /// Validates settings before they are made available to the application.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">
    /// Thrown when a required settings group is missing.
    /// </exception>
    public void Validate()
    {
        if (Features is null)
        {
            throw new ConfigurationValidationException("Feature settings are required.");
        }

        Features.Validate();

        if (Logging is null)
        {
            throw new ConfigurationValidationException("Logging settings are required.");
        }

        Logging.Validate();

        if (Ai is null)
        {
            throw new ConfigurationValidationException("AI settings are required.");
        }

        Ai.Validate();

        if (Catalog is null)
        {
            throw new ConfigurationValidationException("Catalog settings are required.");
        }

        Catalog.Validate();
    }
}

/// <summary>
/// Defines application-wide interface-complexity choices independently from AI enablement.
/// </summary>
public sealed class FeatureSettings
{
    /// <summary>Gets or initializes whether specialist and troubleshooting features are shown.</summary>
    public bool ShowAdvancedFeatures { get; init; }

    /// <summary>Validates feature settings reserved for compatible expansion.</summary>
    public void Validate()
    {
    }
}

/// <summary>
/// Identifies an independently enabled AI capability.
/// </summary>
public enum AiCapability
{
    /// <summary>Review-only suggestions for one known file name.</summary>
    FileRenameSuggestions,
    /// <summary>Review-only logical folder hierarchies for known file metadata.</summary>
    FolderStructureSuggestions,
}

/// <summary>
/// Defines user control over the bounded, application-owned local results catalog.
/// </summary>
public sealed class CatalogSettings
{
    /// <summary>
    /// Gets or initializes whether completed display-safe scan snapshots may be stored in OpenSorSe application data.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Validates catalog settings reserved for future compatible expansion.
    /// </summary>
    public void Validate()
    {
    }
}

/// <summary>
/// Defines the optional, user-controlled local Ollama integration settings.
/// </summary>
public sealed class AiSettings
{
    /// <summary>Gets the maximum supported model identifier length.</summary>
    public const int MaximumModelIdentifierLength = 256;

    /// <summary>
    /// Gets or initializes whether AI suggestion requests are permitted.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>Gets or initializes whether review-only file-rename suggestions are enabled.</summary>
    public bool FileRenameSuggestionsEnabled { get; init; }

    /// <summary>Gets or initializes whether review-only folder-structure suggestions are enabled.</summary>
    public bool FolderStructureSuggestionsEnabled { get; init; }

    /// <summary>
    /// Gets or initializes the Ollama-compatible endpoint. The default is the local Ollama endpoint.
    /// </summary>
    public string Endpoint { get; init; } = "http://127.0.0.1:11434";

    /// <summary>
    /// Gets or initializes the locally discovered model selected for suggestion requests.
    /// </summary>
    public string? SelectedModel { get; init; }

    /// <summary>
    /// Gets or initializes the bounded duration permitted for one AI request.
    /// </summary>
    public int RequestTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Gets or initializes whether locally recorded approved patterns may be supplied as concise request context.
    /// </summary>
    public bool PreferenceAdaptationEnabled { get; init; } = true;

    /// <summary>
    /// Gets whether the master switch and the specified capability switch are both enabled.
    /// </summary>
    /// <param name="capability">The capability to evaluate.</param>
    /// <returns><see langword="true"/> only when the capability may be considered for use.</returns>
    public bool IsCapabilityEnabled(AiCapability capability) => Enabled && capability switch
    {
        AiCapability.FileRenameSuggestions => FileRenameSuggestionsEnabled,
        AiCapability.FolderStructureSuggestions => FolderStructureSuggestionsEnabled,
        _ => false,
    };

    /// <summary>
    /// Validates the supported local AI configuration values.
    /// </summary>
    /// <exception cref="ConfigurationValidationException">Thrown when the settings are unsafe or unsupported.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint) ||
            !Uri.TryCreate(Endpoint.Trim(), UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https") ||
            string.IsNullOrWhiteSpace(endpoint.Host) ||
            RequestTimeoutSeconds is < 1 or > 120 ||
            SelectedModel is { } model &&
            (model.Length > MaximumModelIdentifierLength || model.Any(char.IsControl)))
        {
            throw new ConfigurationValidationException("AI settings are invalid.");
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
             !Path.IsPathRooted(LogDirectoryPath)))
        {
            throw new ConfigurationValidationException("Logging settings are invalid.");
        }
    }
}
