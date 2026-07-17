using CommunityToolkit.Mvvm.Input;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Presents a read-only application overview and navigation-only quick actions.
/// </summary>
public sealed class DashboardViewModel : ViewModelBase
{
    private readonly Action<NavigationDestination> _navigate;

    /// <summary>
    /// Initializes a dashboard that delegates quick actions to the application shell.
    /// </summary>
    /// <param name="navigate">The shell navigation action.</param>
    public DashboardViewModel(Action<NavigationDestination> navigate)
    {
        _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        ScanFolderCommand = new RelayCommand(() => _navigate(NavigationDestination.Scan));
        ViewResultsCommand = new RelayCommand(() => _navigate(NavigationDestination.Results));
        OpenSettingsCommand = new RelayCommand(() => _navigate(NavigationDestination.Settings));
    }

    /// <summary>
    /// Gets the current dashboard totals.
    /// </summary>
    public DashboardStatistics Statistics => DashboardStatistics.Empty;

    /// <summary>
    /// Gets the status shown before later session and scan components publish activity.
    /// </summary>
    public string StatusText => "Ready";

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
}
