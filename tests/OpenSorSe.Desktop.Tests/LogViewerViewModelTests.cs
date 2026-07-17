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
    /// Verifies refresh projects user-facing process-lifetime diagnostics without querying log-entry payloads.
    /// </summary>
    [Fact]
    public void Refresh_ProjectsAggregateCountersAndLoggingHealth()
    {
        var logging = new TestLoggingService(new LoggingStatistics(1, 2, 3, 4, 5, 6, 7));
        var viewModel = new LogViewerViewModel(logging);

        Assert.Equal(21L, viewModel.RecordedEventCount);
        Assert.Equal(7L, viewModel.Statistics.FileWriteFailures);
        Assert.Equal("Attention needed: OpenSorSe could not write one or more diagnostic log entries.", viewModel.LoggingStatus);
        Assert.Equal("Diagnostics updated.", viewModel.StatusText);
        Assert.Equal(1, logging.StatisticsRequestCount);
    }

    /// <summary>
    /// Verifies the empty diagnostics state explains that no events exist in the current application session.
    /// </summary>
    [Fact]
    public void Constructor_NoRecordedEvents_UsesPlainLanguageEmptyState()
    {
        var logging = new TestLoggingService(LoggingStatistics.Empty);
        var viewModel = new LogViewerViewModel(logging);

        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.HasRecordedEvents);
        Assert.Equal("No diagnostic events have been recorded in this application session.", viewModel.EmptyStateMessage);
        Assert.Equal("Healthy: no diagnostic log write failures have been recorded.", viewModel.LoggingStatus);
        Assert.Equal(viewModel.EmptyStateMessage, viewModel.StatusText);
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
