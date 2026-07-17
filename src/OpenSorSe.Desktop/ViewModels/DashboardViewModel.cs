using CommunityToolkit.Mvvm.Input;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents a read-only application overview and navigation-only quick actions.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private readonly Action<NavigationDestination> _navigate;
    private DashboardStatistics _statistics = DashboardStatistics.Empty;
    private bool _hasCompletedScan;
    private string _statusText = "No scan has been completed in this application session.";

    /// <summary>
    /// Initializes a dashboard that delegates quick actions to the application shell.
    /// </summary>
    /// <param name="navigate">The shell navigation action.</param>
    public DashboardViewModel(Action<NavigationDestination> navigate)
    {
        _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        ScanFolderCommand = new RelayCommand(() => _navigate(NavigationDestination.Scan));
        ViewResultsCommand = new RelayCommand(() => _navigate(NavigationDestination.Results), () => HasCompletedScan);
        OpenSettingsCommand = new RelayCommand(() => _navigate(NavigationDestination.Settings));
    }

    /// <summary>
    /// Gets the current dashboard totals.
    /// </summary>
    public DashboardStatistics Statistics
    {
        get => _statistics;
        private set => SetProperty(ref _statistics, value);
    }

    /// <summary>
    /// Gets whether a completed scan is available to review in the current application session.
    /// </summary>
    public bool HasCompletedScan
    {
        get => _hasCompletedScan;
        private set
        {
            if (SetProperty(ref _hasCompletedScan, value))
            {
                OnPropertyChanged(nameof(IsAwaitingFirstScan));
                ViewResultsCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether the dashboard should explain that no completed scan is available yet.
    /// </summary>
    public bool IsAwaitingFirstScan => !HasCompletedScan;

    /// <summary>
    /// Gets the current user-safe dashboard status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the quick action that opens the scan destination.
    /// </summary>
    public IRelayCommand ScanFolderCommand { get; }

    /// <summary>
    /// Gets the quick action that opens the results destination.
    /// </summary>
    public IRelayCommand ViewResultsCommand { get; }

    /// <summary>
    /// Gets the quick action that opens the settings destination.
    /// </summary>
    public IRelayCommand OpenSettingsCommand { get; }

    /// <summary>
    /// Replaces the dashboard totals with the latest completed, in-memory scan summary.
    /// </summary>
    /// <param name="summary">The summary produced for the completed read-only results review.</param>
    public void UpdateFromCompletedScan(ResultsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        Statistics = new DashboardStatistics(
            summary.FilesScanned,
            summary.FoldersDiscovered,
            summary.ExactDuplicates,
            summary.Warnings);
        HasCompletedScan = true;
        StatusText = $"Latest scan completed: {summary.FilesScanned} file(s) and {summary.FoldersDiscovered} folder(s) discovered.";
    }
}
