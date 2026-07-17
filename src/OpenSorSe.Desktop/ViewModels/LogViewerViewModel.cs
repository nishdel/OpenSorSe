using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents privacy-safe process-lifetime logging aggregates without retaining or reading log payloads.
/// </summary>
public sealed class LogViewerViewModel : ViewModelBase
{
    private readonly ILoggingService _loggingService;
    private LogLevel? _selectedLevel;
    private LoggingStatistics _statistics = LoggingStatistics.Empty;
    private string _statusText = "No log-entry payloads are retained in v0.1.";

    /// <summary>
    /// Initializes aggregate logging-health presentation.
    /// </summary>
    /// <param name="loggingService">The centralized logging service that provides aggregate counters.</param>
    public LogViewerViewModel(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
        RefreshCommand = new RelayCommand(Refresh);
        ClearDisplayCommand = new RelayCommand(ClearDisplay);
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
                OnPropertyChanged(nameof(FilteredEntryCount));
            }
        }
    }

    /// <summary>
    /// Gets or sets an optional aggregate severity filter.
    /// </summary>
    public LogLevel? SelectedLevel
    {
        get => _selectedLevel;
        set
        {
            if (SetProperty(ref _selectedLevel, value))
            {
                OnPropertyChanged(nameof(FilteredEntryCount));
            }
        }
    }

    /// <summary>
    /// Gets the documented levels that may filter the aggregate view.
    /// </summary>
    public IReadOnlyList<LogLevel> AvailableLevels { get; } = Enum.GetValues<LogLevel>();

    /// <summary>
    /// Gets the aggregate entry count for the optional selected severity.
    /// </summary>
    public long FilteredEntryCount => SelectedLevel switch
    {
        null => Statistics.TraceEntries + Statistics.DebugEntries + Statistics.InformationEntries + Statistics.WarningEntries + Statistics.ErrorEntries + Statistics.CriticalEntries,
        LogLevel.Trace => Statistics.TraceEntries,
        LogLevel.Debug => Statistics.DebugEntries,
        LogLevel.Information => Statistics.InformationEntries,
        LogLevel.Warning => Statistics.WarningEntries,
        LogLevel.Error => Statistics.ErrorEntries,
        LogLevel.Critical => Statistics.CriticalEntries,
        _ => 0,
    };

    /// <summary>
    /// Gets the user-safe aggregate-view status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the command that refreshes aggregate logging counters.
    /// </summary>
    public IRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command that clears only the displayed aggregate snapshot.
    /// </summary>
    public IRelayCommand ClearDisplayCommand { get; }

    /// <summary>
    /// Refreshes the displayed aggregate counters without reading log files.
    /// </summary>
    public void Refresh()
    {
        Statistics = _loggingService.GetStatistics();
        StatusText = "Logging statistics refreshed.";
    }

    private void ClearDisplay()
    {
        Statistics = LoggingStatistics.Empty;
        StatusText = "Displayed statistics cleared. Stored logs were not changed.";
    }
}
