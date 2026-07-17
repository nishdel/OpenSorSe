using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Application;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Desktop.ViewModels;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;
using DesktopScanRequest = OpenSorSe.Desktop.ViewModels.ScanRequest;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies deterministic state transitions in the desktop application shell.
/// </summary>
public sealed class MainViewModelTests
{
    /// <summary>
    /// Verifies the shell starts on Dashboard and exposes destinations in documented enum order.
    /// </summary>
    [Fact]
    public void Constructor_InitializesDashboardNavigation()
    {
        var viewModel = new MainViewModel();

        Assert.Equal(NavigationDestination.Dashboard, viewModel.SelectedDestination);
        Assert.Equal("Dashboard", viewModel.CurrentPageTitle);
        Assert.Equal("Ready", viewModel.StatusText);
        Assert.Equal(Enum.GetValues<NavigationDestination>(), viewModel.Destinations);
    }

    /// <summary>
    /// Verifies navigation updates the selected destination and presentation title without business work.
    /// </summary>
    [Fact]
    public void Navigate_UpdatesPresentationState()
    {
        var viewModel = new MainViewModel();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => changedProperties.Add(arguments.PropertyName);

        viewModel.Navigate(NavigationDestination.Settings);

        Assert.Equal(NavigationDestination.Settings, viewModel.SelectedDestination);
        Assert.Equal("Settings", viewModel.CurrentPageTitle);
        Assert.True(viewModel.IsSettingsSelected);
        Assert.False(viewModel.IsDashboardSelected);
        Assert.False(viewModel.IsScanSelected);
        Assert.False(viewModel.IsResultsSelected);
        Assert.False(viewModel.IsRulesSelected);
        Assert.False(viewModel.IsDiagnosticsSelected);
        Assert.False(viewModel.IsHistorySelected);
        Assert.False(viewModel.IsAboutSelected);
        Assert.Contains(nameof(MainViewModel.SelectedDestination), changedProperties);
        Assert.Contains(nameof(MainViewModel.CurrentPageTitle), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsDashboardSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsScanSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsResultsSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsRulesSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsSettingsSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsDiagnosticsSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsHistorySelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsAboutSelected), changedProperties);
    }

    /// <summary>
    /// Verifies the Scan destination exposes only the scan-root selection state.
    /// </summary>
    [Fact]
    public void Navigate_ToScan_ExposesFolderSelectionState()
    {
        var viewModel = new MainViewModel();

        viewModel.Navigate(NavigationDestination.Scan);

        Assert.True(viewModel.IsScanSelected);
        Assert.False(viewModel.IsFeaturePageSelected);
        Assert.NotNull(viewModel.FolderSelection);
    }

    /// <summary>
    /// Verifies unsupported navigation input is rejected before presentation state changes.
    /// </summary>
    [Fact]
    public void Navigate_UnsupportedDestination_ThrowsWithoutChangingState()
    {
        var viewModel = new MainViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.Navigate((NavigationDestination)999));

        Assert.Equal(NavigationDestination.Dashboard, viewModel.SelectedDestination);
    }

    /// <summary>
    /// Verifies dashboard quick actions request shell navigation without starting business operations.
    /// </summary>
    [Fact]
    public void DashboardQuickActions_KeepResultsUnavailableUntilAScanCompletes()
    {
        var viewModel = new MainViewModel();
        var initialStatistics = viewModel.Dashboard.Statistics;

        viewModel.Dashboard.ScanFolderCommand.Execute(null);
        Assert.Equal(NavigationDestination.Scan, viewModel.SelectedDestination);
        Assert.False(viewModel.Dashboard.ViewResultsCommand.CanExecute(null));
        viewModel.Dashboard.UpdateFromCompletedScan(new ResultsSummary(2, 1, 0, 1, 0));
        viewModel.Dashboard.ViewResultsCommand.Execute(null);
        Assert.Equal(NavigationDestination.Results, viewModel.SelectedDestination);
        viewModel.Dashboard.OpenSettingsCommand.Execute(null);

        Assert.Equal(NavigationDestination.Settings, viewModel.SelectedDestination);
        Assert.NotEqual(initialStatistics, viewModel.Dashboard.Statistics);
        Assert.Equal(new DashboardStatistics(2, 1, 1, 0), viewModel.Dashboard.Statistics);
        Assert.True(viewModel.Dashboard.HasCompletedScan);
    }

    /// <summary>
    /// Verifies the shell converts a selected-folder request into a controller request and presents read-only results.
    /// </summary>
    [Fact]
    public async Task StartProcessingAsync_CompletedRequest_PresentsDiscoveredFilesAndDirectories()
    {
        var controller = new RecordingController();
        using var viewModel = new MainViewModel(new TestConfigurationService(), new TestLoggingService(), controller);
        var request = new DesktopScanRequest(["C:\\Selected"]);

        var start = viewModel.StartProcessingAsync(request);
        var forwarded = await controller.Request.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(request.FolderPaths, forwarded.ScanRequest.RootDirectories);
        Assert.Empty(forwarded.Rules);
        Assert.True(viewModel.IsProcessing);
        Assert.True(viewModel.ScanProgress.IsActive);

        var file = new FileEntry("C:\\Selected\\document.txt");
        var directory = new DirectoryEntry("C:\\Selected");
        controller.Completion.SetResult(CompletedResult(file, directory));
        await start;

        Assert.False(viewModel.IsProcessing);
        Assert.Equal(NavigationDestination.Results, viewModel.SelectedDestination);
        Assert.NotNull(viewModel.Results.Snapshot);
        Assert.Equal([file.FullPath], viewModel.Results.Snapshot!.Files.Select(resultFile => resultFile.FullPath));
        Assert.Equal([directory.FullPath], viewModel.Results.Directories.Select(resultDirectory => resultDirectory.FullPath));
        Assert.Equal(["A test scan warning."], viewModel.Results.Warnings);
        Assert.Contains("1 file(s) and 1 folder(s)", viewModel.StatusText, StringComparison.Ordinal);
        Assert.Equal(ScanProgressStage.Completed, viewModel.ScanProgress.Stage);
        Assert.Equal(new DashboardStatistics(1, 1, 0, 1), viewModel.Dashboard.Statistics);
        Assert.True(viewModel.Dashboard.HasCompletedScan);
        Assert.True(viewModel.Dashboard.ViewResultsCommand.CanExecute(null));

        viewModel.Navigate(NavigationDestination.Dashboard);
        Assert.Equal(new DashboardStatistics(1, 1, 0, 1), viewModel.Dashboard.Statistics);
        viewModel.Dashboard.ViewResultsCommand.Execute(null);
        Assert.Equal(NavigationDestination.Results, viewModel.SelectedDestination);
        Assert.Equal([file.FullPath], viewModel.Results.Snapshot!.Files.Select(resultFile => resultFile.FullPath));
    }

    /// <summary>
    /// Verifies progress-view cancellation reaches the controller and never invokes a filesystem executor.
    /// </summary>
    [Fact]
    public async Task StartProcessingAsync_CancelRequest_ForwardsCancellationAndKeepsTheScanPage()
    {
        var controller = new RecordingController();
        using var viewModel = new MainViewModel(new TestConfigurationService(), new TestLoggingService(), controller);
        viewModel.Navigate(NavigationDestination.Scan);

        var start = viewModel.StartProcessingAsync(new DesktopScanRequest(["C:\\Selected"]));
        await controller.Request.Task.WaitAsync(TimeSpan.FromSeconds(5));
        viewModel.ScanProgress.RequestCancellation();
        await controller.CancellationRequested.Task.WaitAsync(TimeSpan.FromSeconds(5));
        controller.Completion.SetResult(new ProcessingSessionResult(
            new ProcessingSession("session:cancelled", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ProcessingSessionStatus.Cancelled, null),
            null));
        await start;

        Assert.False(viewModel.IsProcessing);
        Assert.Equal(NavigationDestination.Scan, viewModel.SelectedDestination);
        Assert.Equal(ScanProgressStage.Cancelled, viewModel.ScanProgress.Stage);
        Assert.Equal("Scan cancelled. Any partial discovery results were not processed further.", viewModel.StatusText);
    }

    private static ProcessingSessionResult CompletedResult(FileEntry file, DirectoryEntry directory)
    {
        var scan = new ScanResult(
            [file],
            [directory],
            new ScanStatistics(1, 1, 1),
            [new ScanIssue("C:\\Selected\\Skipped", ScanIssueKind.DirectoryUnavailable, "A test scan warning.")],
            ScanStatus.Completed,
            TimeSpan.Zero);
        var conflicts = new ConflictResolutionResult(
            [],
            new ConflictResolutionStatistics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
            []);
        var duplicates = new DuplicateDetectionResult(
            [file],
            [],
            new DuplicateDetectionStatistics(1, 1, 0, 0, 0, 0),
            []);
        var processing = new ProcessingResult(ProcessingStatus.Completed, scan, null, null, null, duplicates, null, null, conflicts);
        return new ProcessingSessionResult(
            new ProcessingSession("session:completed", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, ProcessingSessionStatus.Completed, null),
            processing);
    }

    private sealed class RecordingController : IApplicationController
    {
        public TaskCompletionSource<ProcessingSessionResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<ProcessingRequest> Request { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<bool> CancellationRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ProcessingSessionResult> StartProcessingAsync(
            ProcessingRequest request,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Request.TrySetResult(request);
            cancellationToken.Register(() => CancellationRequested.TrySetResult(true));
            return Completion.Task;
        }
    }

    private sealed class TestConfigurationService : IConfigurationService
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

    private sealed class TestLoggingService : ILoggingService
    {
        public void Initialize(LogLevel minimumLevel)
        {
        }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose()
        {
        }
    }
}
