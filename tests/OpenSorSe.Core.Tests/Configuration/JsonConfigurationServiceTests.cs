using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Core.Tests.Configuration;

/// <summary>
/// Tests JSON-backed application configuration behavior.
/// </summary>
public sealed class JsonConfigurationServiceTests
{
    /// <summary>
    /// Verifies that environment values take precedence over persisted user settings.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_UsesEnvironmentValueOverUserSetting()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(settingsFilePath, "{\"Logging\":{\"MinimumLevel\":\"Warning\"}}");

        try
        {
            var service = new JsonConfigurationService(
                settingsFilePath,
                variableName => variableName == "OPENSORSE_LOGGING__MINIMUMLEVEL" ? "Debug" : null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Debug, service.Current.Logging.MinimumLevel);
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>
    /// Verifies that missing user configuration uses safe defaults.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_UsesInformationLoggingByDefault()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        var service = new JsonConfigurationService(settingsFilePath, _ => null);

        await service.InitializeAsync(CancellationToken.None);

        Assert.Equal(LogLevel.Information, service.Current.Logging.MinimumLevel);
        Assert.False(service.Current.Features.ShowAdvancedFeatures);
        Assert.False(service.Current.Ai.Enabled);
        Assert.False(service.Current.Ai.FileRenameSuggestionsEnabled);
        Assert.False(service.Current.Ai.FolderStructureSuggestionsEnabled);
    }

    /// <summary>Verifies pre-v0.9.1 JSON keeps established values while new opt-ins receive safe defaults.</summary>
    [Fact]
    public async Task InitializeAsync_PreV091Settings_DefaultsNewSwitchesOffWithoutResettingProviderValues()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(settingsFilePath, """{"Ai":{"Enabled":true,"Endpoint":"http://127.0.0.1:11434","SelectedModel":"existing-model","RequestTimeoutSeconds":45}}""");

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.True(service.Current.Ai.Enabled);
            Assert.Equal("existing-model", service.Current.Ai.SelectedModel);
            Assert.Equal(45, service.Current.Ai.RequestTimeoutSeconds);
            Assert.False(service.Current.Ai.FileRenameSuggestionsEnabled);
            Assert.False(service.Current.Ai.FolderStructureSuggestionsEnabled);
            Assert.False(service.Current.Features.ShowAdvancedFeatures);
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>
    /// Verifies logging file settings remain intact when the minimum level is overridden from the environment.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_PreservesLoggingOutputSettingsDuringEnvironmentOverride()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(settingsFilePath, "{\"Logging\":{\"MinimumLevel\":\"Warning\",\"FileLoggingEnabled\":false,\"LogDirectoryPath\":\"C:\\\\Logs\",\"RetainedFileCount\":3}}");

        try
        {
            var service = new JsonConfigurationService(
                settingsFilePath,
                variableName => variableName == "OPENSORSE_LOGGING__MINIMUMLEVEL" ? "Error" : null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Error, service.Current.Logging.MinimumLevel);
            Assert.False(service.Current.Logging.FileLoggingEnabled);
            Assert.Equal("C:\\Logs", service.Current.Logging.LogDirectoryPath);
            Assert.Equal(3, service.Current.Logging.RetainedFileCount);
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>
    /// Verifies malformed owned JSON is preserved while safe defaults and a user-visible recovery warning are activated.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_MalformedJson_UsesDefaultsAndPreservesInvalidFile()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        const string invalidJson = "{invalid";
        await File.WriteAllTextAsync(settingsFilePath, invalidJson);

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Information, service.Current.Logging.MinimumLevel);
            Assert.NotNull(service.InitializationWarning);
            Assert.Equal(invalidJson, await File.ReadAllTextAsync(settingsFilePath));
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>
    /// Verifies an oversized application-owned settings file activates safe defaults and remains untouched.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_OversizedSettings_UsesDefaultsAndPreservesFile()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        await using (var stream = new FileStream(settingsFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            stream.SetLength(ConfigurationLimits.MaximumSettingsFileBytes + 1);
        }

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Information, service.Current.Logging.MinimumLevel);
            Assert.NotNull(service.InitializationWarning);
            Assert.Equal(ConfigurationLimits.MaximumSettingsFileBytes + 1, new FileInfo(settingsFilePath).Length);
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>Verifies syntactically valid but unsafe settings also recover without rewriting the owned file.</summary>
    [Fact]
    public async Task InitializeAsync_InvalidSettingsValues_UseDefaultsAndPreserveFile()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        const string invalidSettings = "{\"Logging\":{\"RetainedFileCount\":0}}";
        await File.WriteAllTextAsync(settingsFilePath, invalidSettings);

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await service.InitializeAsync(CancellationToken.None);

            Assert.Equal(7, service.Current.Logging.RetainedFileCount);
            Assert.NotNull(service.InitializationWarning);
            Assert.Equal(invalidSettings, await File.ReadAllTextAsync(settingsFilePath));
        }
        finally
        {
            File.Delete(settingsFilePath);
        }
    }

    /// <summary>
    /// Verifies saving and loading round-trips validated settings through the configured application path only.
    /// </summary>
    [Fact]
    public async Task SaveAsync_RoundTripsValidatedSettings()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}");
        var settingsFilePath = Path.Combine(directoryPath, "settings.json");

        try
        {
            var writer = new JsonConfigurationService(settingsFilePath, _ => null);
            await writer.InitializeAsync(CancellationToken.None);
            await writer.SaveAsync(CancellationToken.None);
            var reader = new JsonConfigurationService(settingsFilePath, _ => null);

            await reader.InitializeAsync(CancellationToken.None);

            Assert.Equal(LogLevel.Information, reader.Current.Logging.MinimumLevel);
            Assert.True(reader.Current.Logging.FileLoggingEnabled);
            Assert.Equal(7, reader.Current.Logging.RetainedFileCount);
            Assert.Empty(Directory.GetFiles(directoryPath, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies replacement settings are validated, persisted, and exposed only after successful serialization.
    /// </summary>
    [Fact]
    public async Task SaveAsync_ReplacementSettings_PersistsAndUpdatesCurrent()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}");
        var settingsFilePath = Path.Combine(directoryPath, "settings.json");
        var settings = new ApplicationSettings
        {
            Features = new FeatureSettings
            {
                ShowAdvancedFeatures = true,
            },
            Logging = new LoggingSettings
            {
                MinimumLevel = LogLevel.Warning,
                FileLoggingEnabled = false,
                RetainedFileCount = 3,
            },
            Ai = new AiSettings
            {
                Enabled = true,
                FileRenameSuggestionsEnabled = true,
                FolderStructureSuggestionsEnabled = false,
                Endpoint = "http://127.0.0.1:11434",
                SelectedModel = "llama3:latest",
                RequestTimeoutSeconds = 45,
                PreferenceAdaptationEnabled = false,
            },
            Catalog = new CatalogSettings
            {
                Enabled = true,
            },
        };

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await service.SaveAsync(settings, CancellationToken.None);

            Assert.Same(settings, service.Current);
            var reader = new JsonConfigurationService(settingsFilePath, _ => null);
            await reader.InitializeAsync(CancellationToken.None);
            Assert.Equal(LogLevel.Warning, reader.Current.Logging.MinimumLevel);
            Assert.False(reader.Current.Logging.FileLoggingEnabled);
            Assert.Equal(3, reader.Current.Logging.RetainedFileCount);
            Assert.True(reader.Current.Ai.Enabled);
            Assert.True(reader.Current.Features.ShowAdvancedFeatures);
            Assert.True(reader.Current.Ai.FileRenameSuggestionsEnabled);
            Assert.False(reader.Current.Ai.FolderStructureSuggestionsEnabled);
            Assert.Equal("llama3:latest", reader.Current.Ai.SelectedModel);
            Assert.Equal(45, reader.Current.Ai.RequestTimeoutSeconds);
            Assert.False(reader.Current.Ai.PreferenceAdaptationEnabled);
            Assert.True(reader.Current.Catalog.Enabled);
        }
        finally
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies cancellation occurs before configuration file access or parent-directory creation.
    /// </summary>
    [Fact]
    public async Task InitializeAndSaveAsync_PreCancelled_LeaveFilesystemUntouched()
    {
        var directoryPath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}");
        var settingsFilePath = Path.Combine(directoryPath, "settings.json");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = new JsonConfigurationService(settingsFilePath, _ => null);

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.InitializeAsync(cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.SaveAsync(cancellation.Token));
        Assert.False(Directory.Exists(directoryPath));
    }

    /// <summary>
    /// Verifies relative configuration paths are rejected before runtime configuration can begin.
    /// </summary>
    [Fact]
    public void Constructor_RelativePath_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new JsonConfigurationService("settings.json", _ => null));
    }

    /// <summary>Verifies provider-controlled model identifiers cannot persist control characters.</summary>
    [Fact]
    public void ApplicationSettings_ControlCharacterModel_IsRejected()
    {
        var settings = new ApplicationSettings
        {
            Ai = new AiSettings { Enabled = true, SelectedModel = "bad\nmodel" },
        };

        Assert.Throws<ConfigurationValidationException>(settings.Validate);
    }
}
