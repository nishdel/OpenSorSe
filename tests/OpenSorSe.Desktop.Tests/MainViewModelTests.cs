using OpenSorSe.Desktop.ViewModels;

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
        Assert.False(viewModel.IsLogsSelected);
        Assert.False(viewModel.IsHistorySelected);
        Assert.False(viewModel.IsAboutSelected);
        Assert.Contains(nameof(MainViewModel.SelectedDestination), changedProperties);
        Assert.Contains(nameof(MainViewModel.CurrentPageTitle), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsDashboardSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsScanSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsResultsSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsRulesSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsSettingsSelected), changedProperties);
        Assert.Contains(nameof(MainViewModel.IsLogsSelected), changedProperties);
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
    public void DashboardQuickActions_NavigateWithoutChangingStatistics()
    {
        var viewModel = new MainViewModel();
        var initialStatistics = viewModel.Dashboard.Statistics;

        viewModel.Dashboard.ScanFolderCommand.Execute(null);
        Assert.Equal(NavigationDestination.Scan, viewModel.SelectedDestination);
        viewModel.Dashboard.ViewResultsCommand.Execute(null);
        Assert.Equal(NavigationDestination.Results, viewModel.SelectedDestination);
        viewModel.Dashboard.OpenSettingsCommand.Execute(null);

        Assert.Equal(NavigationDestination.Settings, viewModel.SelectedDestination);
        Assert.Equal(initialStatistics, viewModel.Dashboard.Statistics);
        Assert.Equal("Ready", viewModel.Dashboard.StatusText);
    }
}
