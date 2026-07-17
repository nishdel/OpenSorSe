using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TidyMind.Core.DependencyInjection;
using TidyMind.Core.Logging;

namespace TidyMind.Core.Tests.Logging;

/// <summary>
/// Verifies local, privacy-preserving centralized logging behavior.
/// </summary>
public sealed class LoggingServiceTests
{
    /// <summary>
    /// Verifies enabled categories write UTC daily text entries and accurate process-lifetime counts.
    /// </summary>
    [Fact]
    public void Initialize_WritesCategorizedEntriesAndStatistics()
    {
        using var directory = new TemporaryDirectory();
        using var service = new LoggingService();
        service.Initialize(new LoggingOptions(LogLevel.Information, true, directory.Path));
        var logger = service.CreateLogger("CoreTests");

        logger.LogInformation("information message");
        logger.LogWarning("warning message");
        logger.LogError(new InvalidOperationException("sensitive exception"), "error message");

        var filePath = Assert.Single(Directory.GetFiles(directory.Path, "tidymind-*.log"));
        var output = File.ReadAllText(filePath);
        Assert.Contains("[Information] [CoreTests] information message", output, StringComparison.Ordinal);
        Assert.Contains("[Warning] [CoreTests] warning message", output, StringComparison.Ordinal);
        Assert.Contains("[Error] [CoreTests] error message", output, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive exception", output, StringComparison.Ordinal);
        Assert.Equal(new LoggingStatistics(0, 0, 1, 1, 1, 0, 0), service.GetStatistics());
    }

    /// <summary>
    /// Verifies the configured minimum level filters both local output and statistics.
    /// </summary>
    [Fact]
    public void Initialize_FiltersEntriesBelowMinimumLevel()
    {
        using var directory = new TemporaryDirectory();
        using var service = new LoggingService();
        service.Initialize(new LoggingOptions(LogLevel.Warning, true, directory.Path));
        var logger = service.CreateLogger("CoreTests");

        logger.LogInformation("filtered");
        logger.LogWarning("retained");

        var output = File.ReadAllText(Assert.Single(Directory.GetFiles(directory.Path, "tidymind-*.log")));
        Assert.DoesNotContain("filtered", output, StringComparison.Ordinal);
        Assert.Contains("retained", output, StringComparison.Ordinal);
        Assert.Equal(new LoggingStatistics(0, 0, 0, 1, 0, 0, 0), service.GetStatistics());
    }

    /// <summary>
    /// Verifies disabling the file sink produces no application-log directory while remaining safe to log through Debug.
    /// </summary>
    [Fact]
    public void Initialize_WithFileSinkDisabled_DoesNotCreateLogDirectory()
    {
        using var directory = new TemporaryDirectory();
        var logDirectoryPath = Path.Combine(directory.Path, "logs");
        using var service = new LoggingService();
        service.Initialize(new LoggingOptions(LogLevel.Information, false, logDirectoryPath));

        service.CreateLogger("CoreTests").LogInformation("debug fallback remains available");

        Assert.False(Directory.Exists(logDirectoryPath));
        Assert.Equal(1L, service.GetStatistics().InformationEntries);
    }

    /// <summary>
    /// Verifies only matching daily log files beyond the configured retention count are removed.
    /// </summary>
    [Fact]
    public void Initialize_AppliesBoundedDailyRetention()
    {
        using var directory = new TemporaryDirectory();
        for (var day = 1; day <= 9; day++)
        {
            File.WriteAllText(Path.Combine(directory.Path, $"tidymind-2024-01-{day:00}.log"), "old");
        }

        var unrelatedPath = Path.Combine(directory.Path, "unrelated.log");
        File.WriteAllText(unrelatedPath, "retain");
        using var service = new LoggingService();
        service.Initialize(new LoggingOptions(LogLevel.Information, true, directory.Path, 7));

        Assert.Equal(7, Directory.GetFiles(directory.Path, "tidymind-*.log").Length);
        Assert.True(File.Exists(unrelatedPath));
    }

    /// <summary>
    /// Verifies an unavailable local file sink cannot interrupt an application log call.
    /// </summary>
    [Fact]
    public void Initialize_WhenFileSinkFails_ContinuesAndCountsFailure()
    {
        using var directory = new TemporaryDirectory();
        var blockingPath = directory.CreateFile("not-a-directory", "block");
        using var service = new LoggingService();
        service.Initialize(new LoggingOptions(LogLevel.Information, true, blockingPath));

        service.CreateLogger("CoreTests").LogError("still safe");

        Assert.Equal(1L, service.GetStatistics().ErrorEntries);
        Assert.True(service.GetStatistics().FileWriteFailures >= 1);
    }

    /// <summary>
    /// Verifies invalid output options are rejected and disposal prevents later use.
    /// </summary>
    [Fact]
    public void Initialize_RejectsInvalidOptionsAndDisposedServiceCannotBeUsed()
    {
        using var service = new LoggingService();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Initialize(new LoggingOptions(LogLevel.Information, RetainedFileCount: 0)));
        Assert.Throws<ArgumentException>(() => service.Initialize(new LoggingOptions(LogLevel.Information, true, "relative")));
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.CreateLogger("CoreTests"));
        Assert.Throws<ObjectDisposedException>(() => service.Initialize(LogLevel.Information));
    }

    /// <summary>
    /// Verifies shared Core registration resolves the centralized logging contract without UI infrastructure.
    /// </summary>
    [Fact]
    public void DependencyInjection_ResolvesLoggingService()
    {
        using var directory = new TemporaryDirectory();
        var services = new ServiceCollection();
        services.AddTidyMindCore(new TidyMindCoreOptions { ConfigurationFilePath = Path.Combine(directory.Path, "settings.json") });
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.IsType<LoggingService>(provider.GetRequiredService<ILoggingService>());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"TidyMind.Logging.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string contents)
        {
            var filePath = System.IO.Path.Combine(Path, name);
            File.WriteAllText(filePath, contents);
            return filePath;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
