using OpenSorSe.Application;
using OpenSorSe.Application.AI;
using OpenSorSe.Application.Catalog;
using OpenSorSe.Application.CatalogComparison;
using OpenSorSe.Application.CatalogSearch;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Represents the presentation state for the application's initial shell window.
/// </summary>
public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private static readonly IReadOnlyList<NavigationDestination> NavigationDestinations =
        Enum.GetValues<NavigationDestination>();
    private static readonly IReadOnlyList<NavigationItem> PrimaryNavigationItems = Array.AsReadOnly<NavigationItem>(
    [
        new(NavigationDestination.Dashboard, "Dashboard"),
        new(NavigationDestination.Scan, "Scan folders"),
        new(NavigationDestination.Results, "Results"),
        new(NavigationDestination.Catalog, "Saved catalog"),
        new(NavigationDestination.CatalogSearch, "Catalog search"),
        new(NavigationDestination.CatalogComparison, "Compare snapshots"),
        new(NavigationDestination.Rules, "Rules"),
        new(NavigationDestination.Settings, "Settings"),
        new(NavigationDestination.Diagnostics, "Diagnostics"),
        new(NavigationDestination.History, "Operation history"),
        new(NavigationDestination.About, "About"),
    ]);
    private readonly IApplicationController? _applicationController;
    private readonly IConfigurationService _configurationService;
    private readonly IResultsSnapshotProjector _resultsSnapshotProjector;
    private readonly IResultsCatalogStore? _catalogStore;
    private NavigationDestination _selectedDestination = NavigationDestination.Dashboard;
    private NavigationItem _selectedNavigationItem = PrimaryNavigationItems[0];
    private CancellationTokenSource? _processingCancellation;
    private bool _isProcessing;
    private string _statusText = "Ready";
    private string? _currentCatalogEntryId;

    /// <summary>
    /// Initializes the shell with its dashboard presentation model.
    /// </summary>
    public MainViewModel()
        : this(new PreviewConfigurationService(), new LoggingService(), null, new ResultsSnapshotProjector(), null, null, null, new CatalogComparisonService(), true)
    {
    }

    /// <summary>
    /// Initializes the shell with its dashboard, page state, and configuration-backed settings editor.
    /// </summary>
    /// <param name="configurationService">The initialized configuration service used by the settings page.</param>
    /// <param name="loggingService">The centralized logging service used by the aggregate log viewer.</param>
    public MainViewModel(IConfigurationService configurationService, ILoggingService loggingService)
        : this(configurationService, loggingService, null, new ResultsSnapshotProjector(), null, null, null, new CatalogComparisonService(), true)
    {
    }

    /// <summary>
    /// Initializes the shell with the application controller that runs the non-destructive processing pipeline.
    /// </summary>
    /// <param name="configurationService">The initialized configuration service used by the settings page.</param>
    /// <param name="loggingService">The centralized logging service used by the aggregate log viewer.</param>
    /// <param name="applicationController">The UI-agnostic controller for read-only processing requests.</param>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController)
        : this(configurationService, loggingService, applicationController ?? throw new ArgumentNullException(nameof(applicationController)), new ResultsSnapshotProjector(), null, null, null, new CatalogComparisonService(), true)
    {
    }

    /// <summary>
    /// Initializes the shell with its non-destructive processing controller and immutable results projector.
    /// </summary>
    /// <param name="configurationService">The initialized configuration service used by the settings page.</param>
    /// <param name="loggingService">The centralized logging service used by the aggregate log viewer.</param>
    /// <param name="applicationController">The UI-agnostic controller for read-only processing requests.</param>
    /// <param name="resultsSnapshotProjector">The application-layer projector for completed processing output.</param>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector)
        : this(
            configurationService,
            loggingService,
            applicationController ?? throw new ArgumentNullException(nameof(applicationController)),
            resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector)),
            null,
            null,
            null,
            new CatalogComparisonService(),
            true)
    {
    }

    /// <summary>
    /// Initializes the shell with all read-only processing and optional AI suggestion services.
    /// </summary>
    /// <param name="configurationService">The initialized configuration service used by Settings and optional AI workflows.</param>
    /// <param name="loggingService">The centralized logging service used by Diagnostics.</param>
    /// <param name="applicationController">The UI-agnostic controller for read-only processing requests.</param>
    /// <param name="resultsSnapshotProjector">The application-layer projector for completed processing output.</param>
    /// <param name="aiSuggestionService">The application-owned optional suggestion service.</param>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector,
        IAiSuggestionService aiSuggestionService)
        : this(
            configurationService,
            loggingService,
            applicationController ?? throw new ArgumentNullException(nameof(applicationController)),
            resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector)),
            aiSuggestionService ?? throw new ArgumentNullException(nameof(aiSuggestionService)),
            null,
            null,
            new CatalogComparisonService(),
            true)
    {
    }

    /// <summary>
    /// Initializes the shell with optional AI and application-owned catalog services.
    /// </summary>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector,
        IAiSuggestionService aiSuggestionService,
        IResultsCatalogStore resultsCatalogStore)
        : this(
            configurationService,
            loggingService,
            applicationController ?? throw new ArgumentNullException(nameof(applicationController)),
            resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector)),
            aiSuggestionService ?? throw new ArgumentNullException(nameof(aiSuggestionService)),
            resultsCatalogStore ?? throw new ArgumentNullException(nameof(resultsCatalogStore)),
            null,
            new CatalogComparisonService(),
            true)
    {
    }

    /// <summary>
    /// Initializes the production shell with catalog snapshots and bounded named catalog-query persistence.
    /// </summary>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector,
        IAiSuggestionService aiSuggestionService,
        IResultsCatalogStore resultsCatalogStore,
        ISavedCatalogSearchStore savedCatalogSearchStore)
        : this(
            configurationService,
            loggingService,
            applicationController ?? throw new ArgumentNullException(nameof(applicationController)),
            resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector)),
            aiSuggestionService ?? throw new ArgumentNullException(nameof(aiSuggestionService)),
            resultsCatalogStore ?? throw new ArgumentNullException(nameof(resultsCatalogStore)),
            savedCatalogSearchStore ?? throw new ArgumentNullException(nameof(savedCatalogSearchStore)),
            new CatalogComparisonService(),
            true)
    {
    }

    /// <summary>
    /// Initializes the production shell with catalog persistence, named queries, and historical comparison.
    /// </summary>
    public MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector,
        IAiSuggestionService aiSuggestionService,
        IResultsCatalogStore resultsCatalogStore,
        ISavedCatalogSearchStore savedCatalogSearchStore,
        ICatalogComparisonService catalogComparisonService)
        : this(
            configurationService,
            loggingService,
            applicationController ?? throw new ArgumentNullException(nameof(applicationController)),
            resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector)),
            aiSuggestionService ?? throw new ArgumentNullException(nameof(aiSuggestionService)),
            resultsCatalogStore ?? throw new ArgumentNullException(nameof(resultsCatalogStore)),
            savedCatalogSearchStore ?? throw new ArgumentNullException(nameof(savedCatalogSearchStore)),
            catalogComparisonService ?? throw new ArgumentNullException(nameof(catalogComparisonService)),
            true)
    {
    }

    private MainViewModel(
        IConfigurationService configurationService,
        ILoggingService loggingService,
        IApplicationController? applicationController,
        IResultsSnapshotProjector resultsSnapshotProjector,
        IAiSuggestionService? aiSuggestionService,
        IResultsCatalogStore? catalogStore,
        ISavedCatalogSearchStore? savedSearchStore,
        ICatalogComparisonService comparisonService,
        bool _)
    {
        ArgumentNullException.ThrowIfNull(configurationService);
        ArgumentNullException.ThrowIfNull(loggingService);
        _configurationService = configurationService;
        _applicationController = applicationController;
        _resultsSnapshotProjector = resultsSnapshotProjector ?? throw new ArgumentNullException(nameof(resultsSnapshotProjector));
        _catalogStore = catalogStore;
        Dashboard = new DashboardViewModel(Navigate);
        FolderSelection = new FolderSelectionViewModel();
        ScanProgress = new ScanProgressViewModel();
        Results = new ResultsViewModel(configurationService, aiSuggestionService);
        Catalog = new CatalogViewModel(configurationService, catalogStore);
        CatalogSearch = new CatalogSearchViewModel(configurationService, catalogStore, savedSearchStore);
        CatalogComparison = new CatalogComparisonViewModel(configurationService, catalogStore, comparisonService);
        RuleEditor = new RuleEditorViewModel();
        Settings = new SettingsViewModel(configurationService, aiSuggestionService);
        LogViewer = new LogViewerViewModel(loggingService);
        UndoHistory = new UndoHistoryViewModel();
        About = new AboutViewModel();
        Notifications = new NotificationCenterViewModel();
        FolderSelection.ScanRequested += OnScanRequested;
        ScanProgress.CancelRequested += OnScanCancellationRequested;
        Results.PersistedTagsChanged += OnPersistedTagsChanged;
        Catalog.EntryOpened += OnCatalogEntryOpened;
        Catalog.CatalogChanged += OnCatalogChanged;
        CatalogSearch.EntryOpened += OnCatalogEntryOpened;
        CatalogComparison.EntryOpened += OnCatalogEntryOpened;
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
    /// Gets the live presentation model for the active read-only processing operation.
    /// </summary>
    public ScanProgressViewModel ScanProgress { get; }

    /// <summary>
    /// Gets the immutable-result review state hosted by the shell.
    /// </summary>
    public ResultsViewModel Results { get; }

    /// <summary>
    /// Gets the opt-in, application-owned saved results catalog state.
    /// </summary>
    public CatalogViewModel Catalog { get; }

    /// <summary>
    /// Gets deterministic metadata search state for opt-in, application-owned saved snapshots.
    /// </summary>
    public CatalogSearchViewModel CatalogSearch { get; }

    /// <summary>
    /// Gets deterministic comparison state for two application-owned historical snapshots.
    /// </summary>
    public CatalogComparisonViewModel CatalogComparison { get; }

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

    /// <summary>Gets user-facing primary navigation items in their stable shell order.</summary>
    public IReadOnlyList<NavigationItem> NavigationItems => PrimaryNavigationItems;

    /// <summary>Gets or sets the user-facing navigation item selected by the shell.</summary>
    public NavigationItem SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Navigate(value.Destination);
        }
    }

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
                _selectedNavigationItem = PrimaryNavigationItems.Single(item => item.Destination == value);
                OnPropertyChanged(nameof(SelectedNavigationItem));
                OnPropertyChanged(nameof(CurrentPageTitle));
                OnPropertyChanged(nameof(IsDashboardSelected));
                OnPropertyChanged(nameof(IsScanSelected));
                OnPropertyChanged(nameof(IsResultsSelected));
                OnPropertyChanged(nameof(IsCatalogSelected));
                OnPropertyChanged(nameof(IsCatalogSearchSelected));
                OnPropertyChanged(nameof(IsCatalogComparisonSelected));
                OnPropertyChanged(nameof(IsRulesSelected));
                OnPropertyChanged(nameof(IsSettingsSelected));
                OnPropertyChanged(nameof(IsDiagnosticsSelected));
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
        NavigationDestination.Catalog => "Saved catalog",
        NavigationDestination.CatalogSearch => "Catalog search",
        NavigationDestination.CatalogComparison => "Compare snapshots",
        NavigationDestination.Rules => "Rules",
        NavigationDestination.Settings => "Settings",
        NavigationDestination.Diagnostics => "Diagnostics",
        NavigationDestination.History => "Operation history",
        NavigationDestination.About => "About OpenSorSe",
        _ => throw new InvalidOperationException("The navigation destination is unsupported."),
    };

    /// <summary>
    /// Gets the current user-safe application status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets whether a processing request is currently active.
    /// </summary>
    public bool IsProcessing
    {
        get => _isProcessing;
        private set
        {
            if (SetProperty(ref _isProcessing, value))
            {
                OnPropertyChanged(nameof(IsFolderSelectionVisible));
            }
        }
    }

    /// <summary>
    /// Gets whether the folder-selection controls should be presented instead of live progress.
    /// </summary>
    public bool IsFolderSelectionVisible => !IsProcessing;

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
    /// Gets whether the opt-in saved-results catalog page is currently selected.
    /// </summary>
    public bool IsCatalogSelected => SelectedDestination == NavigationDestination.Catalog;

    /// <summary>
    /// Gets whether deterministic catalog-wide metadata search is currently selected.
    /// </summary>
    public bool IsCatalogSearchSelected => SelectedDestination == NavigationDestination.CatalogSearch;

    /// <summary>
    /// Gets whether historical saved-snapshot comparison is currently selected.
    /// </summary>
    public bool IsCatalogComparisonSelected => SelectedDestination == NavigationDestination.CatalogComparison;

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
    public bool IsDiagnosticsSelected => SelectedDestination == NavigationDestination.Diagnostics;

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
    public bool IsFeaturePageSelected => !IsDashboardSelected && !IsScanSelected && !IsResultsSelected && !IsCatalogSelected && !IsCatalogSearchSelected && !IsCatalogComparisonSelected && !IsRulesSelected && !IsSettingsSelected && !IsDiagnosticsSelected && !IsHistorySelected && !IsAboutSelected;

    /// <summary>
    /// Selects a documented application-shell destination.
    /// </summary>
    /// <param name="destination">The destination to display.</param>
    public void Navigate(NavigationDestination destination)
    {
        SelectedDestination = destination;
    }

    /// <summary>
    /// Selects a shell destination and completes any destination-owned refresh before returning.
    /// </summary>
    /// <param name="destination">The destination to display.</param>
    /// <returns>A task that completes after the destination is ready for presentation.</returns>
    public async Task NavigateAsync(NavigationDestination destination)
    {
        Navigate(destination);
        if (destination == NavigationDestination.Catalog)
        {
            await Catalog.RefreshAsync();
        }
        else if (destination == NavigationDestination.CatalogSearch)
        {
            await CatalogSearch.RefreshSavedSearchesAsync();
        }
        else if (destination == NavigationDestination.CatalogComparison)
        {
            await CatalogComparison.RefreshEntriesAsync();
        }
    }

    /// <summary>
    /// Initializes optional catalog presentation after the configuration service has loaded.
    /// </summary>
    /// <returns>A task that completes once the local catalog has been queried or a safe unavailable state is displayed.</returns>
    public Task InitializeCatalogAsync() => Task.WhenAll(
        Catalog.RefreshAsync(),
        CatalogSearch.RefreshSavedSearchesAsync(),
        CatalogComparison.RefreshEntriesAsync());

    /// <summary>
    /// Releases notification-expiration resources and requests cancellation of active processing.
    /// </summary>
    public void Dispose()
    {
        FolderSelection.ScanRequested -= OnScanRequested;
        ScanProgress.CancelRequested -= OnScanCancellationRequested;
        Results.PersistedTagsChanged -= OnPersistedTagsChanged;
        Catalog.EntryOpened -= OnCatalogEntryOpened;
        Catalog.CatalogChanged -= OnCatalogChanged;
        CatalogSearch.EntryOpened -= OnCatalogEntryOpened;
        CatalogComparison.EntryOpened -= OnCatalogEntryOpened;
        _processingCancellation?.Cancel();
        Results.Dispose();
        Catalog.Dispose();
        CatalogSearch.Dispose();
        CatalogComparison.Dispose();
        Settings.Dispose();
        Notifications.Dispose();
    }

    private async void OnScanRequested(object? sender, ScanRequest request)
    {
        await StartProcessingAsync(request);
    }

    /// <summary>
    /// Starts one read-only processing request through the configured application controller.
    /// </summary>
    /// <param name="request">The validated desktop scan request.</param>
    /// <returns>A task that completes when the request reaches a terminal presentation state.</returns>
    public async Task StartProcessingAsync(ScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (IsProcessing)
        {
            return;
        }

        if (_applicationController is null)
        {
            StatusText = "Scanning is unavailable because the application controller has not been configured.";
            Notifications.Publish(new NotificationRequest(NotificationSeverity.Error, StatusText));
            return;
        }

        using var cancellation = new CancellationTokenSource();
        _processingCancellation = cancellation;
        IsProcessing = true;
        ScanProgress.Start();
        StatusText = "Scanning selected folders...";

        try
        {
            var progress = new Progress<ProcessingProgress>(ApplyProgress);
            var processingRequest = new ProcessingRequest(
                new OpenSorSe.Scanner.Models.ScanRequest(request.FolderPaths, ScanOptions.Default),
                RuleEditor.Rules.ToArray());
            var result = await _applicationController.StartProcessingAsync(
                processingRequest,
                progress,
                cancellation.Token);

            if (result.Session.Status == ProcessingSessionStatus.Completed &&
                result.Processing is { Status: ProcessingStatus.Completed } processing)
            {
                var snapshot = await Task.Run(() => _resultsSnapshotProjector.Project(result));
                await Results.LoadSnapshotAsync(snapshot);
                _currentCatalogEntryId = null;
                if (_catalogStore is not null && _configurationService.Current.Catalog.Enabled)
                {
                    await PersistCompletedSnapshotAsync(snapshot, request.FolderPaths.ToArray());
                }

                Dashboard.UpdateFromCompletedScan(Results.Summary);
                ScanProgress.Complete(ScanStatus.Completed);
                StatusText = $"Scan completed: {processing.Scan.Statistics.FilesDiscovered} file(s) and {processing.Scan.Statistics.DirectoriesDiscovered} folder(s) discovered.";
                Notifications.Publish(new NotificationRequest(NotificationSeverity.Success, StatusText));
                Navigate(NavigationDestination.Results);
            }
            else if (result.Session.Status == ProcessingSessionStatus.Cancelled)
            {
                ScanProgress.Complete(ScanStatus.Cancelled);
                StatusText = "Scan cancelled. Any partial discovery results were not processed further.";
                Notifications.Publish(new NotificationRequest(NotificationSeverity.Information, StatusText));
            }
            else
            {
                StatusText = result.Session.FailureMessage ?? "The processing session could not be completed.";
                Notifications.Publish(new NotificationRequest(NotificationSeverity.Error, StatusText));
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            ScanProgress.Complete(ScanStatus.Cancelled);
            StatusText = "Scan cancelled.";
            Notifications.Publish(new NotificationRequest(NotificationSeverity.Information, StatusText));
        }
        catch (Exception)
        {
            StatusText = "The scan could not be started or completed.";
            Notifications.Publish(new NotificationRequest(NotificationSeverity.Error, StatusText));
        }
        finally
        {
            if (ReferenceEquals(_processingCancellation, cancellation))
            {
                _processingCancellation = null;
            }

            IsProcessing = false;
        }
    }

    private void ApplyProgress(ProcessingProgress progress)
    {
        ScanProgress.SetStageText(progress.Stage switch
        {
            ProcessingProgressStage.Scanning => "Scanning files...",
            ProcessingProgressStage.ReadingMetadata => "Reading file metadata...",
            ProcessingProgressStage.Hashing => "Hashing files...",
            ProcessingProgressStage.Classifying => "Classifying files...",
            ProcessingProgressStage.DetectingDuplicates => "Detecting duplicates...",
            ProcessingProgressStage.EvaluatingRules => "Evaluating rules...",
            ProcessingProgressStage.PlanningActions => "Planning actions...",
            ProcessingProgressStage.ResolvingConflicts => "Resolving conflicts...",
            ProcessingProgressStage.Completed => "Preparing results...",
            ProcessingProgressStage.Cancelled => "Cancelling scan...",
            _ => throw new ArgumentOutOfRangeException(nameof(progress)),
        });

        if (progress.ScanProgress is not null)
        {
            ScanProgress.ApplyProgress(progress.ScanProgress);
        }
    }

    private void OnScanCancellationRequested(object? sender, EventArgs eventArgs)
    {
        _processingCancellation?.Cancel();
        StatusText = "Cancelling scan...";
    }

    private async Task PersistCompletedSnapshotAsync(ResultsSnapshot snapshot, IReadOnlyList<string> sourceRoots)
    {
        if (_catalogStore is null)
        {
            return;
        }

        var entry = new CatalogEntry(
            $"catalog:{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow,
            snapshot,
            Results.GetPersistableTags())
        {
            SourceRoots = Array.AsReadOnly(sourceRoots.ToArray()),
        };
        try
        {
            await _catalogStore.SaveAsync(entry, CancellationToken.None);
            if (ReferenceEquals(Results.Snapshot, snapshot))
            {
                _currentCatalogEntryId = entry.Id;
                await Catalog.RefreshAsync();
                await RefreshCatalogDependentsAsync();
                Notifications.Publish(new NotificationRequest(NotificationSeverity.Information, "This completed scan was saved to the local catalog."));
                if (!entry.AcceptedTags.SequenceEqual(Results.GetPersistableTags()))
                {
                    await PersistAcceptedTagsAsync();
                }
            }
        }
        catch (CatalogCapacityExceededException)
        {
            Notifications.Publish(new NotificationRequest(
                NotificationSeverity.Warning,
                $"This completed scan was not saved to the local catalog because its files or selected source roots exceed the documented catalog limits ({CatalogLimits.MaximumFilesPerEntry:N0} files and {CatalogLimits.MaximumSourceRootCount} roots)."));
        }
        catch (Exception)
        {
            Notifications.Publish(new NotificationRequest(
                NotificationSeverity.Warning,
                "This completed scan could not be saved to the local catalog. Results remain available in this session."));
        }
    }

    private async void OnCatalogEntryOpened(object? sender, CatalogEntry entry)
    {
        try
        {
            await Results.LoadSnapshotAsync(entry.Snapshot, entry.AcceptedTags);
            Results.MarkSnapshotAsSavedCatalogEntry();
            _currentCatalogEntryId = entry.Id;
            Dashboard.UpdateFromCompletedScan(Results.Summary);
            StatusText = "Saved catalog snapshot opened. It has not been refreshed from the filesystem.";
            Navigate(NavigationDestination.Results);
        }
        catch (Exception)
        {
            Notifications.Publish(new NotificationRequest(NotificationSeverity.Error, "The saved catalog snapshot could not be displayed."));
        }
    }

    private async void OnPersistedTagsChanged(object? sender, EventArgs eventArgs)
    {
        if (_currentCatalogEntryId is not null && _catalogStore is not null && _configurationService.Current.Catalog.Enabled)
        {
            await PersistAcceptedTagsAsync();
        }
    }

    private async void OnCatalogChanged(object? sender, EventArgs eventArgs)
    {
        await RefreshCatalogDependentsAsync();
    }

    private async Task PersistAcceptedTagsAsync()
    {
        if (_catalogStore is null || _currentCatalogEntryId is null || Results.Snapshot is null)
        {
            return;
        }

        try
        {
            var existing = await _catalogStore.LoadAsync(_currentCatalogEntryId, CancellationToken.None);
            if (existing is null ||
                !string.Equals(Results.Snapshot.SessionId, existing.Snapshot.SessionId, StringComparison.Ordinal) ||
                Results.Snapshot.ProjectedAtUtc != existing.Snapshot.ProjectedAtUtc)
            {
                return;
            }

            await _catalogStore.SaveAsync(existing with { AcceptedTags = Results.GetPersistableTags() }, CancellationToken.None);
            await Catalog.RefreshAsync();
            await RefreshCatalogDependentsAsync();
        }
        catch (Exception)
        {
            Notifications.Publish(new NotificationRequest(
                NotificationSeverity.Warning,
                "Accepted tags could not be saved to the local catalog. They remain available for this session."));
        }
    }

    private async Task RefreshCatalogDependentsAsync()
    {
        CatalogSearch.InvalidateResults();
        CatalogComparison.InvalidateCatalog();
        await CatalogComparison.RefreshEntriesAsync();
    }

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
