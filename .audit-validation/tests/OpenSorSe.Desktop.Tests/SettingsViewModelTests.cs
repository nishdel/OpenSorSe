using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Desktop.ViewModels;

namespace OpenSorSe.Desktop.Tests;

/// <summary>
/// Verifies configuration-backed settings presentation without application restart or filesystem work.
/// </summary>
public sealed class SettingsViewModelTests
{
    /// <summary>Verifies settings corruption recovery is visible rather than silently using defaults.</summary>
    [Fact]
    public void Constructor_ExposesConfigurationRecoveryWarning()
    {
        const string warning = "Invalid owned settings were preserved; defaults are active.";
        using var viewModel = new SettingsViewModel(new TestConfigurationService(warning));

        Assert.Equal(warning, viewModel.StatusText);
    }

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

    /// <summary>Verifies a custom log path must remain absolute even while file logging is disabled.</summary>
    [Fact]
    public async Task SaveAsync_DisabledLoggingWithRelativePath_DoesNotPersistLatentInvalidConfiguration()
    {
        var configuration = new TestConfigurationService();
        var viewModel = new SettingsViewModel(configuration);
        viewModel.Draft.FileLoggingEnabled = false;
        viewModel.Draft.LogDirectoryPath = "relative-logs";

        await viewModel.SaveCommand.ExecuteAsync(null);

        Assert.Equal(0, configuration.ReplacementSaveCount);
        Assert.Equal("Settings are invalid.", viewModel.StatusText);
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

    /// <summary>
    /// Verifies the daily diagnostic-log retention setting has permanent user-facing context and validation guidance.
    /// </summary>
    [Fact]
    public void Constructor_ExposesDailyDiagnosticLogRetentionContext()
    {
        var viewModel = new SettingsViewModel(new TestConfigurationService());

        Assert.Equal("Daily diagnostic log files to retain", viewModel.DailyLogRetentionLabel);
        Assert.Contains("OpenSorSe application diagnostic log files", viewModel.DailyLogRetentionDescription, StringComparison.Ordinal);
        Assert.Contains("does not affect scanned user files", viewModel.DailyLogRetentionDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Enter a whole number of at least 1.", viewModel.DailyLogRetentionValidation);
    }

    /// <summary>Verifies owned AI decision history cannot be cleared without a separate explicit confirmation.</summary>
    [Fact]
    public async Task PreferenceHistoryReset_RequiresConfirmationAndCanBeCancelled()
    {
        var ai = new RecordingAiSuggestionService();
        using var viewModel = new SettingsViewModel(new TestConfigurationService(), ai);

        viewModel.RequestPreferenceHistoryResetCommand.Execute(null);
        Assert.True(viewModel.IsPreferenceHistoryResetPending);
        Assert.Equal(0, ai.ResetCallCount);

        viewModel.CancelPreferenceHistoryResetCommand.Execute(null);
        Assert.False(viewModel.IsPreferenceHistoryResetPending);
        Assert.Equal(0, ai.ResetCallCount);

        viewModel.RequestPreferenceHistoryResetCommand.Execute(null);
        await viewModel.ConfirmPreferenceHistoryResetCommand.ExecuteAsync(null);

        Assert.False(viewModel.IsPreferenceHistoryResetPending);
        Assert.Equal(1, ai.ResetCallCount);
        Assert.Contains("No scanned file", viewModel.StatusText, StringComparison.Ordinal);
    }

    /// <summary>Verifies a user cancellation reaches active optional AI work and leaves commands usable.</summary>
    [Fact]
    public async Task TestAiConnection_CancelCommand_CancelsAndPreventsStalePublication()
    {
        var ai = new RecordingAiSuggestionService(blockConnection: true);
        using var viewModel = new SettingsViewModel(new TestConfigurationService(), ai);

        var running = viewModel.TestAiConnectionCommand.ExecuteAsync(null);
        await ai.ConnectionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(viewModel.IsAiBusy);

        viewModel.CancelAiOperationCommand.Execute(null);
        await running;

        Assert.False(viewModel.IsAiBusy);
        Assert.Equal(AiAvailabilityState.RequestCancelled, viewModel.AiAvailabilityState);
        Assert.True(viewModel.TestAiConnectionCommand.CanExecute(null));
    }

    private sealed class TestConfigurationService(string? initializationWarning = null) : IConfigurationService
    {
        public ApplicationSettings Current { get; private set; } = new();

        public string? InitializationWarning { get; } = initializationWarning;

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

    private sealed class RecordingAiSuggestionService(bool blockConnection = false) : IAiSuggestionService
    {
        public TaskCompletionSource<bool> ConnectionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ResetCallCount { get; private set; }

        public async Task<AiConnectionResult> TestConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
        {
            ConnectionStarted.TrySetResult(true);
            if (blockConnection)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return new AiConnectionResult(AiAvailabilityState.Connected, "Connected", []);
        }

        public Task<AiConnectionResult> DiscoverModelsAsync(AiSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult(new AiConnectionResult(AiAvailabilityState.NoModelsAvailable, "No models", []));

        public Task<AiFileSuggestionResult> GenerateFileSuggestionAsync(
            AiFileSuggestionRequest request,
            AiSettings settings,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AiFileSuggestionResult(AiAvailabilityState.Disabled, "Disabled", null));

        public Task<AiFolderStructureResult> GenerateFolderStructureAsync(
            AiFolderStructureRequest request,
            AiSettings settings,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AiFolderStructureResult(AiAvailabilityState.Disabled, "Disabled", null));

        public Task RecordDecisionAsync(AiSuggestionDecision decision, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task ResetDecisionHistoryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ResetCallCount++;
            return Task.CompletedTask;
        }
    }
}
