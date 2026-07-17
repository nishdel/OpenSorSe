using CommunityToolkit.Mvvm.Input;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents privacy-safe process-lifetime logging aggregates without retaining or reading log payloads.
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase
{
    private readonly ILoggingService _loggingService;
    private LoggingStatistics _statistics = LoggingStatistics.Empty;
    private string _statusText = "No diagnostic events have been recorded in this application session.";

    /// <summary>
    /// Initializes aggregate logging-health presentation.
    /// </summary>
    /// <param name="loggingService">The centralized logging service that provides aggregate counters.</param>
    public LogViewerViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        RefreshCommand = new RelayCommand(Refresh);
        Refresh();
    }

    /// <summary>
    /// Gets process-lifetime logging counters without log text or exception details.
    /// </summary>
    public LoggingStatistics Statistics
    {
        get => _statistics;
        private set
        {
            if (SetProperty(ref _statistics, value))
            {
                OnPropertyChanged(nameof(RecordedEventCount));
                OnPropertyChanged(nameof(HasRecordedEvents));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(LoggingStatus));
            }
        }
    }

    /// <summary>
    /// Gets the total number of diagnostic events recorded during the current application session.
    /// </summary>
    public long RecordedEventCount => Statistics.TraceEntries + Statistics.DebugEntries + Statistics.InformationEntries +
        Statistics.WarningEntries + Statistics.ErrorEntries + Statistics.CriticalEntries;

    /// <summary>
    /// Gets whether OpenSorSe recorded any diagnostic events in the current application session.
    /// </summary>
    public bool HasRecordedEvents => RecordedEventCount > 0;

    /// <summary>
    /// Gets whether the page should show its no-events explanation.
    /// </summary>
    public bool IsEmpty => !HasRecordedEvents;

    /// <summary>
    /// Gets a plain-language description of the known diagnostic logging health.
    /// </summary>
    public string LoggingStatus => Statistics.FileWriteFailures == 0
        ? "Healthy: no diagnostic log write failures have been recorded."
        : "Attention needed: OpenSorSe could not write one or more diagnostic log entries.";

    /// <summary>
    /// Gets the explanation shown when the current application session has no diagnostic events.
    /// </summary>
    public string EmptyStateMessage => "No diagnostic events have been recorded in this application session.";

    /// <summary>
    /// Gets the user-safe aggregate-view status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the command that reads the latest aggregate logging counters.
    /// </summary>
    public IRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Refreshes the displayed aggregate counters without reading log files.
    /// </summary>
    public void Refresh()
    {
        Statistics = _loggingService.GetStatistics();
        StatusText = HasRecordedEvents ? "Diagnostics updated." : EmptyStateMessage;
    }
}
