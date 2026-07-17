using TidyMind.Desktop.ViewModels;
using TidyMind.Scanner.Models;

namespace TidyMind.Desktop.Tests;

/// <summary>
/// Verifies passive scan-progress presentation and cancellation signaling.
/// </summary>
public sealed class ScanProgressViewModelTests
{
    /// <summary>
    /// Verifies starting and applying a scanner snapshot updates only presentation state.
    /// </summary>
    [Fact]
    public void ApplyProgress_UpdatesPresentedScannerSnapshot()
    {
        var viewModel = new ScanProgressViewModel();
        viewModel.Start();

        viewModel.ApplyProgress(new ScanProgress("C:\\Scan", new ScanStatistics(4, 2, 1), TimeSpan.FromSeconds(3)));

        Assert.Equal(ScanProgressStage.Scanning, viewModel.Stage);
        Assert.Equal("C:\\Scan", viewModel.CurrentFolder);
        Assert.Equal(4L, viewModel.FilesFound);
        Assert.Equal(2L, viewModel.FoldersScanned);
        Assert.Equal(TimeSpan.FromSeconds(3), viewModel.Elapsed);
    }

    /// <summary>
    /// Verifies terminal scanner statuses are mapped to deterministic presentation stages.
    /// </summary>
    [Theory]
    [InlineData(ScanStatus.Completed, ScanProgressStage.Completed, "Scan completed.")]
    [InlineData(ScanStatus.Cancelled, ScanProgressStage.Cancelled, "Scan cancelled.")]
    public void Complete_MapsTerminalScannerStatus(ScanStatus status, ScanProgressStage expectedStage, string expectedStatus)
    {
        var viewModel = new ScanProgressViewModel();

        viewModel.Complete(status);

        Assert.Equal(expectedStage, viewModel.Stage);
        Assert.Equal(expectedStatus, viewModel.StatusText);
        Assert.False(viewModel.IsActive);
    }

    /// <summary>
    /// Verifies cancellation is emitted only for an active scan presentation.
    /// </summary>
    [Fact]
    public void RequestCancellation_EmitsOnlyWhileActive()
    {
        var viewModel = new ScanProgressViewModel();
        var requests = 0;
        viewModel.CancelRequested += (_, _) => requests++;

        viewModel.RequestCancellation();
        viewModel.Start();
        viewModel.RequestCancellation();
        viewModel.Complete(ScanStatus.Cancelled);
        viewModel.RequestCancellation();

        Assert.Equal(1, requests);
    }

    /// <summary>
    /// Verifies unsupported scanner statuses are rejected without mutating progress state.
    /// </summary>
    [Fact]
    public void Complete_UnsupportedStatus_Throws()
    {
        var viewModel = new ScanProgressViewModel();

        Assert.Throws<ArgumentOutOfRangeException>(() => viewModel.Complete((ScanStatus)999));

        Assert.Equal(ScanProgressStage.Idle, viewModel.Stage);
    }
}
