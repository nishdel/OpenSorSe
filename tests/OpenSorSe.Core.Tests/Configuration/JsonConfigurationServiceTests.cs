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
    /// Verifies malformed JSON is mapped to the configuration error contract without replacing current defaults.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_MalformedJson_ThrowsConfigurationValidationException()
    {
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"opensorse-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(settingsFilePath, "{invalid");

        try
        {
            var service = new JsonConfigurationService(settingsFilePath, _ => null);

            await Assert.ThrowsAsync<ConfigurationValidationException>(() => service.InitializeAsync(CancellationToken.None));
            Assert.Equal(LogLevel.Information, service.Current.Logging.MinimumLevel);
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
            Logging = new LoggingSettings
            {
                MinimumLevel = LogLevel.Warning,
                FileLoggingEnabled = false,
                RetainedFileCount = 3,
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
}
