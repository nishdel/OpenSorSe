using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Desktop.ViewModels;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies configuration-backed settings presentation without application restart or filesystem work.
/// </summary>
public sealed class SettingsViewModelTests
{
    /// <summary>
    /// Verifies a valid draft persists through the centralized configuration service and requests restart.
    /// </summary>
    [Fact]
    public async Task SaveAsync_PersistsValidatedDraftAndMarksRestartRequired()
    {
        var configuration = new TestConfigurationService();
        var viewModel = new SettingsViewModel(configuration);
        viewModel.Draft.MinimumLogLevel = LogLevel.Warning;
        viewModel.Draft.FileLoggingEnabled = false;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, configuration.ReplacementSaveCount);
        Assert.Equal(LogLevel.Warning, configuration.Current.Logging.MinimumLevel);
        Assert.False(configuration.Current.Logging.FileLoggingEnabled);
        Assert.True(viewModel.RestartRequired);
    }

    /// <summary>
    /// Verifies invalid input is retained for correction and is not persisted.
    /// </summary>
    [Fact]
    public async Task SaveAsync_InvalidDraft_DoesNotPersist()
    {
        var configuration = new TestConfigurationService();
        var viewModel = new SettingsViewModel(configuration);
        viewModel.Draft.RetainedFileCount = 0;

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(0, configuration.ReplacementSaveCount);
        Assert.Equal("Settings are invalid.", viewModel.StatusText);
        Assert.False(viewModel.RestartRequired);
    }

    /// <summary>
    /// Verifies restore and discard produce drafts without saving configuration.
    /// </summary>
    [Fact]
    public void RestoreAndDiscard_ChangeOnlyTheEditableDraft()
    {
        var configuration = new TestConfigurationService();
        var viewModel = new SettingsViewModel(configuration);
        viewModel.Draft.RetainedFileCount = 2;

        viewModel.RestoreDefaultsCommand.Execute(null);
        Assert.Equal(7, viewModel.Draft.RetainedFileCount);
        viewModel.Draft.RetainedFileCount = 2;
        viewModel.CancelCommand.Execute(null);

        Assert.Equal(configuration.Current.Logging.RetainedFileCount, viewModel.Draft.RetainedFileCount);
        Assert.Equal(0, configuration.ReplacementSaveCount);
    }

    private sealed class TestConfigurationService : IConfigurationService
    {
        public ApplicationSettings Current { get; private set; } = new();

        public int ReplacementSaveCount { get; private set; }

        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken)
        {
            Current = settings;
            ReplacementSaveCount++;
            return Task.CompletedTask;
        }
    }
}
