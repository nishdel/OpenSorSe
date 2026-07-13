using Microsoft.Extensions.Logging;
using TidyMind.Core.Configuration;

namespace TidyMind.Core.Tests.Configuration;

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
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"tidymind-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(settingsFilePath, "{\"Logging\":{\"MinimumLevel\":\"Warning\"}}");

        try
        {
            var service = new JsonConfigurationService(
                settingsFilePath,
                variableName => variableName == "TIDYMIND_LOGGING__MINIMUMLEVEL" ? "Debug" : null);

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
        var settingsFilePath = Path.Combine(Path.GetTempPath(), $"tidymind-{Guid.NewGuid():N}.json");
        var service = new JsonConfigurationService(settingsFilePath, _ => null);

        await service.InitializeAsync(CancellationToken.None);

        Assert.Equal(LogLevel.Information, service.Current.Logging.MinimumLevel);
    }
}
