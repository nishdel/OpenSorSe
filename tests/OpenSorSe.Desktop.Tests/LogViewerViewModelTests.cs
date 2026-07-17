using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Logging;
using OpenSorSe.Desktop.ViewModels;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies privacy-safe aggregate logging presentation.
/// </summary>
public sealed class LogViewerViewModelTests
{
    /// <summary>
    /// Verifies refresh projects process-lifetime counters without querying log-entry payloads.
    /// </summary>
    [Fact]
    public void Refresh_ProjectsAggregateCountersAndOptionalLevelFilter()
    {
        var logging = new TestLoggingService(new LoggingStatistics(1, 2, 3, 4, 5, 6, 7));
        var viewModel = new LogViewerViewModel(logging);

        viewModel.SelectedLevel = LogLevel.Warning;

        Assert.Equal(4L, viewModel.FilteredEntryCount);
        Assert.Equal(7L, viewModel.Statistics.FileWriteFailures);
        Assert.Equal("Logging statistics refreshed.", viewModel.StatusText);
        Assert.Equal(1, logging.StatisticsRequestCount);
    }

    /// <summary>
    /// Verifies clearing changes only the displayed snapshot and never asks the logging service to mutate storage.
    /// </summary>
    [Fact]
    public void ClearDisplay_ClearsOnlyPresentationState()
    {
        var logging = new TestLoggingService(new LoggingStatistics(0, 0, 1, 0, 0, 0, 0));
        var viewModel = new LogViewerViewModel(logging);

        viewModel.ClearDisplayCommand.Execute(null);

        Assert.Equal(0L, viewModel.FilteredEntryCount);
        Assert.Equal("Displayed statistics cleared. Stored logs were not changed.", viewModel.StatusText);
        Assert.Equal(1, logging.StatisticsRequestCount);
    }

    private sealed class TestLoggingService(LoggingStatistics statistics) : ILoggingService
    {
        public int StatisticsRequestCount { get; private set; }

        public void Initialize(LogLevel minimumLevel)
        {
        }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public LoggingStatistics GetStatistics()
        {
            StatisticsRequestCount++;
            return statistics;
        }

        public void Dispose()
        {
        }
    }
}
