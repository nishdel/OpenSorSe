using TidyMind.Core.Configuration;
using TidyMind.Core.Logging;

namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Represents the presentation state for the application's initial shell window.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly IReadOnlyList<NavigationDestination> NavigationDestinations =
        Enum.GetValues<NavigationDestination>();
    private NavigationDestination _selectedDestination = NavigationDestination.Dashboard;

    /// <summary>
    /// Initializes the shell with its dashboard presentation model.
    /// </summary>
    public MainViewModel()
        : this(new PreviewConfigurationService(), new LoggingService())
    {
    }

    /// <summary>
    /// Initializes the shell with its dashboard, page state, and configuration-backed settings editor.
    /// </summary>
    /// <param name="configurationService">The initialized configuration service used by the settings page.</param>
    /// <param name="loggingService">The centralized logging service used by the aggregate log viewer.</param>
    public MainViewModel(IConfigurationService configurationService, ILoggingService loggingService)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(loggingService);
        Dashboard = new DashboardViewModel(Navigate);
        FolderSelection = new FolderSelectionViewModel();
        Results = new ResultsViewModel();
        RuleEditor = new RuleEditorViewModel();
        Settings = new SettingsViewModel(configurationService);
        LogViewer = new LogViewerViewModel(loggingService);
        UndoHistory = new UndoHistoryViewModel();
        About = new AboutViewModel();
        Notifications = new NotificationCenterViewModel();
    }

    /// <summary>
    /// Gets the dashboard state hosted by the shell.
    /// </summary>
    public DashboardViewModel Dashboard { get; }

    /// <summary>
    /// Gets the scan-root selection state hosted by the shell.
    /// </summary>
    public FolderSelectionViewModel FolderSelection { get; }

    /// <summary>
    /// Gets the immutable-result review state hosted by the shell.
    /// </summary>
    public ResultsViewModel Results { get; }

    /// <summary>
    /// Gets the in-memory rule-editing state hosted by the shell.
    /// </summary>
    public RuleEditorViewModel RuleEditor { get; }

    /// <summary>
    /// Gets the configuration-backed settings editing state hosted by the shell.
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// Gets the aggregate logging-health state hosted by the shell.
    /// </summary>
    public LogViewerViewModel LogViewer { get; }

    /// <summary>
    /// Gets the explicit undo-session review state hosted by the shell.
    /// </summary>
    public UndoHistoryViewModel UndoHistory { get; }

    /// <summary>
    /// Gets the static application-information state hosted by the shell.
    /// </summary>
    public AboutViewModel About { get; }

    /// <summary>
    /// Gets the non-blocking in-memory notification queue hosted by the shell.
    /// </summary>
    public NotificationCenterViewModel Notifications { get; }

    /// <summary>
    /// Gets the destinations offered by the primary application shell.
    /// </summary>
    public IReadOnlyList<NavigationDestination> Destinations => NavigationDestinations;

    /// <summary>
    /// Gets or sets the destination currently selected in the application shell.
    /// </summary>
    public NavigationDestination SelectedDestination
    {
        get => _selectedDestination;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "The navigation destination is unsupported.");
            }

            if (SetProperty(ref _selectedDestination, value))
            {
                OnPropertyChanged(nameof(CurrentPageTitle));
                OnPropertyChanged(nameof(IsDashboardSelected));
                OnPropertyChanged(nameof(IsScanSelected));
                OnPropertyChanged(nameof(IsResultsSelected));
                OnPropertyChanged(nameof(IsRulesSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
                OnPropertyChanged(nameof(IsLogsSelected));
                OnPropertyChanged(nameof(IsHistorySelected));
                OnPropertyChanged(nameof(IsAboutSelected));
                OnPropertyChanged(nameof(IsFeaturePageSelected));
            }
        }
    }

    /// <summary>
    /// Gets the title displayed in the content host for the current destination.
    /// </summary>
    public string CurrentPageTitle => SelectedDestination switch
    {
        NavigationDestination.Dashboard => "Dashboard",
        NavigationDestination.Scan => "Scan",
        NavigationDestination.Results => "Results",
        NavigationDestination.Rules => "Rules",
        NavigationDestination.Settings => "Settings",
        NavigationDestination.Logs => "Logs",
        NavigationDestination.History => "Undo History",
        NavigationDestination.About => "About TidyMind",
        _ => throw new InvalidOperationException("The navigation destination is unsupported."),
    };

    /// <summary>
    /// Gets the stable shell status text before later feature pages provide richer status.
    /// </summary>
    public string StatusText => "Ready";

    /// <summary>
    /// Gets whether the dashboard is currently selected.
    /// </summary>
    public bool IsDashboardSelected => SelectedDestination == NavigationDestination.Dashboard;

    /// <summary>
    /// Gets whether the scan-root selection page is currently selected.
    /// </summary>
    public bool IsScanSelected => SelectedDestination == NavigationDestination.Scan;

    /// <summary>
    /// Gets whether the results-review page is currently selected.
    /// </summary>
    public bool IsResultsSelected => SelectedDestination == NavigationDestination.Results;

    /// <summary>
    /// Gets whether the rule-editor page is currently selected.
    /// </summary>
    public bool IsRulesSelected => SelectedDestination == NavigationDestination.Rules;

    /// <summary>
    /// Gets whether the settings page is currently selected.
    /// </summary>
    public bool IsSettingsSelected => SelectedDestination == NavigationDestination.Settings;

    /// <summary>
    /// Gets whether the aggregate logging-health page is currently selected.
    /// </summary>
    public bool IsLogsSelected => SelectedDestination == NavigationDestination.Logs;

    /// <summary>
    /// Gets whether the explicit undo-session history page is currently selected.
    /// </summary>
    public bool IsHistorySelected => SelectedDestination == NavigationDestination.History;

    /// <summary>
    /// Gets whether the application-information page is currently selected.
    /// </summary>
    public bool IsAboutSelected => SelectedDestination == NavigationDestination.About;

    /// <summary>
    /// Gets whether a later feature-page destination is currently selected.
    /// </summary>
    public bool IsFeaturePageSelected => !IsDashboardSelected && !IsScanSelected && !IsResultsSelected && !IsRulesSelected && !IsSettingsSelected && !IsLogsSelected && !IsHistorySelected && !IsAboutSelected;

    /// <summary>
    /// Selects a documented application-shell destination.
    /// </summary>
    /// <param name="destination">The destination to display.</param>
    public void Navigate(NavigationDestination destination) => SelectedDestination = destination;

    /// <summary>
    /// Releases notification-expiration resources owned by the shell.
    /// </summary>
    public void Dispose() => Notifications.Dispose();

    private sealed class PreviewConfigurationService : IConfigurationService
    {
        public ApplicationSettings Current { get; private set; } = new();

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken)
        {
            Current = settings;
            return Task.CompletedTask;
        }
    }
}
