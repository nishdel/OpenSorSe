using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TidyMind.Core.Configuration;

/// <summary>
/// Loads JSON user settings over safe defaults and applies environment overrides.
/// </summary>
public sealed class JsonConfigurationService : IConfigurationService
{
    private const string LoggingLevelEnvironmentVariable = "TIDYMIND_LOGGING__MINIMUMLEVEL";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly Func<string, string?> _environmentVariableReader;
    private readonly string _settingsFilePath;

    /// <summary>
    /// Initializes a configuration service for a user settings file.
    /// </summary>
    /// <param name="settingsFilePath">The absolute path of the user settings file.</param>
    /// <param name="environmentVariableReader">An optional environment variable reader for testing.</param>
    public JsonConfigurationService(
        string settingsFilePath,
        Func<string, string?>? environmentVariableReader = null)
    {
        if (string.IsNullOrWhiteSpace(settingsFilePath))
        {
            throw new ArgumentException("A settings file path is required.", nameof(settingsFilePath));
        }

        _settingsFilePath = settingsFilePath;
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
    }

    /// <inheritdoc />
    public ApplicationSettings Current { get; private set; } = new();

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = new ApplicationSettings();

        if (File.Exists(_settingsFilePath))
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            settings = await JsonSerializer.DeserializeAsync<ApplicationSettings>(
                stream,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false) ?? new ApplicationSettings();
        }

        Current = ApplyEnvironmentOverrides(settings);
        Current.Validate();
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken cancellationToken)
    {
        var directoryPath = Path.GetDirectoryName(_settingsFilePath);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            throw new ConfigurationValidationException("The settings file path must include a directory.");
        }

        Directory.CreateDirectory(directoryPath);
        var temporaryFilePath = $"{_settingsFilePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = File.Create(temporaryFilePath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    Current,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporaryFilePath, _settingsFilePath, true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private ApplicationSettings ApplyEnvironmentOverrides(ApplicationSettings settings)
    {
        var configuredLevel = _environmentVariableReader(LoggingLevelEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configuredLevel))
        {
            return settings;
        }

        if (!Enum.TryParse<LogLevel>(configuredLevel, true, out var minimumLevel))
        {
            throw new ConfigurationValidationException(
                $"{LoggingLevelEnvironmentVariable} must be a valid logging level.");
        }

        return new ApplicationSettings
        {
            Logging = new LoggingSettings { MinimumLevel = minimumLevel },
        };
    }
}
