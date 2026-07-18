using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Edits the supported application settings and persists only through the configuration service.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase, IDisposable
{
    private readonly IConfigurationService _configurationService;
    private readonly IAiSuggestionService? _aiSuggestionService;
    private readonly ObservableCollection<string> _availableAiModels = [];
    private SettingsDraft _draft;
    private bool _restartRequired;
    private bool _isAiBusy;
    private bool _isPreferenceHistoryResetPending;
    private CancellationTokenSource? _aiOperationCancellation;
    private long _aiOperationVersion;
    private bool _isDisposed;
    private string _statusText = "Ready";

    /// <summary>
    /// Initializes a settings editor over the already initialized configuration service.
    /// </summary>
    /// <param name="configurationService">The centralized configuration service.</param>
    /// <param name="aiSuggestionService">The optional application-owned local AI suggestion service.</param>
    public SettingsViewModel(IConfigurationService configurationService, IAiSuggestionService? aiSuggestionService = null)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _aiSuggestionService = aiSuggestionService;
        _draft = SettingsDraft.FromSettings(_configurationService.Current);
        _statusText = _configurationService.InitializationWarning ?? "Ready";
        AvailableAiModels = new ReadOnlyObservableCollection<string>(_availableAiModels);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        CancelCommand = new RelayCommand(DiscardChanges);
        TestAiConnectionCommand = new AsyncRelayCommand(TestAiConnectionAsync, CanStartAiOperation);
        DiscoverAiModelsCommand = new AsyncRelayCommand(DiscoverAiModelsAsync, CanStartAiOperation);
        CancelAiOperationCommand = new RelayCommand(CancelAiOperation, () => IsAiBusy);
        RequestPreferenceHistoryResetCommand = new RelayCommand(RequestPreferenceHistoryReset, CanRequestPreferenceHistoryReset);
        ConfirmPreferenceHistoryResetCommand = new AsyncRelayCommand(ConfirmPreferenceHistoryResetAsync, CanConfirmPreferenceHistoryReset);
        CancelPreferenceHistoryResetCommand = new RelayCommand(CancelPreferenceHistoryReset, () => IsPreferenceHistoryResetPending && !IsAiBusy);
    }

    /// <summary>
    /// Gets the supported editable configuration draft.
    /// </summary>
    public SettingsDraft Draft
    {
        get => _draft;
        private set => SetProperty(ref _draft, value);
    }

    /// <summary>
    /// Gets whether saved settings require application restart to reconfigure active services.
    /// </summary>
    public bool RestartRequired
    {
        get => _restartRequired;
        private set => SetProperty(ref _restartRequired, value);
    }

    /// <summary>
    /// Gets the current user-safe settings status.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the documented log levels users may select.
    /// </summary>
    public IReadOnlyList<LogLevel> AvailableLogLevels { get; } = Enum.GetValues<LogLevel>();

    /// <summary>Gets models discovered from the current draft endpoint.</summary>
    public ReadOnlyObservableCollection<string> AvailableAiModels { get; }

    /// <summary>Gets the current optional AI availability state.</summary>
    public AiAvailabilityState AiAvailabilityState { get; private set; } = AiAvailabilityState.Disabled;

    /// <summary>Gets a concise explanation of the optional AI integration state.</summary>
    public string AiStatusText { get; private set; } = "AI assistance is disabled until enabled and configured.";

    /// <summary>Gets whether an optional AI connection, discovery, or owned-history reset operation is active.</summary>
    public bool IsAiBusy
    {
        get => _isAiBusy;
        private set
        {
            if (SetProperty(ref _isAiBusy, value))
            {
                TestAiConnectionCommand.NotifyCanExecuteChanged();
                DiscoverAiModelsCommand.NotifyCanExecuteChanged();
                CancelAiOperationCommand.NotifyCanExecuteChanged();
                RequestPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
                ConfirmPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
                CancelPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Gets whether the user has requested but not confirmed deletion of owned decision history.</summary>
    public bool IsPreferenceHistoryResetPending
    {
        get => _isPreferenceHistoryResetPending;
        private set
        {
            if (SetProperty(ref _isPreferenceHistoryResetPending, value))
            {
                RequestPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
                ConfirmPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
                CancelPreferenceHistoryResetCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Gets the permanent user-facing label for daily diagnostic-log retention.
    /// </summary>
    public string DailyLogRetentionLabel => "Daily diagnostic log files to retain";

    /// <summary>
    /// Gets the user-facing explanation for the daily diagnostic-log retention setting.
    /// </summary>
    public string DailyLogRetentionDescription => "Controls how many OpenSorSe application diagnostic log files are kept. It does not affect scanned user files.";

    /// <summary>
    /// Gets the validation guidance for daily diagnostic-log retention.
    /// </summary>
    public string DailyLogRetentionValidation => "Enter a whole number of at least 1.";

    /// <summary>
    /// Gets the command that validates and persists the draft.
    /// </summary>
    public IAsyncRelayCommand SaveCommand { get; }

    /// <summary>
    /// Gets the command that restores an unsaved default draft.
    /// </summary>
    public IRelayCommand RestoreDefaultsCommand { get; }

    /// <summary>
    /// Gets the command that discards unsaved draft changes.
    /// </summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the command that checks the current draft's Ollama endpoint.</summary>
    public IAsyncRelayCommand TestAiConnectionCommand { get; }

    /// <summary>Gets the command that retrieves installed models from the current draft endpoint.</summary>
    public IAsyncRelayCommand DiscoverAiModelsCommand { get; }

    /// <summary>Gets the command that explicitly cancels active optional AI work.</summary>
    public IRelayCommand CancelAiOperationCommand { get; }

    /// <summary>Gets the command that requests confirmation before clearing local preference decisions.</summary>
    public IRelayCommand RequestPreferenceHistoryResetCommand { get; }

    /// <summary>Gets the command that confirms clearing only local preference decisions.</summary>
    public IAsyncRelayCommand ConfirmPreferenceHistoryResetCommand { get; }

    /// <summary>Gets the command that cancels a pending preference-history reset.</summary>
    public IRelayCommand CancelPreferenceHistoryResetCommand { get; }

    /// <summary>
    /// Reloads the editable draft from the current persisted configuration.
    /// </summary>
    public void Load()
    {
        IsPreferenceHistoryResetPending = false;
        Draft = SettingsDraft.FromSettings(_configurationService.Current);
        RestartRequired = false;
        StatusText = "Settings loaded.";
        SetAiStatus(_configurationService.Current.Ai.Enabled
            ? AiAvailabilityState.Connected
            : AiAvailabilityState.Disabled,
            _configurationService.Current.Ai.Enabled
                ? "AI settings loaded. Test the connection before requesting suggestions."
                : "AI assistance is disabled until enabled and configured.");
    }

    private async Task SaveAsync()
    {
        try
        {
            var settings = Draft.ToSettings();
            settings.Validate();
            await _configurationService.SaveAsync(settings, CancellationToken.None);
            RestartRequired = true;
            StatusText = "Settings saved. Restart the application to apply active-service changes.";
        }
        catch (ConfigurationValidationException)
        {
            StatusText = "Settings are invalid.";
        }
        catch (IOException)
        {
            StatusText = "Settings could not be saved.";
        }
        catch (UnauthorizedAccessException)
        {
            StatusText = "Settings could not be saved.";
        }
    }

    private void RestoreDefaults()
    {
        IsPreferenceHistoryResetPending = false;
        Draft = SettingsDraft.FromSettings(new ApplicationSettings());
        RestartRequired = false;
        StatusText = "Default settings restored. Save to persist them.";
        SetAiStatus(AiAvailabilityState.Disabled, "AI assistance is disabled until enabled and configured.");
    }

    private void DiscardChanges()
    {
        IsPreferenceHistoryResetPending = false;
        Draft = SettingsDraft.FromSettings(_configurationService.Current);
        RestartRequired = false;
        StatusText = "Unsaved changes discarded.";
    }

    private bool CanStartAiOperation() => _aiSuggestionService is not null && !IsAiBusy;

    private bool CanRequestPreferenceHistoryReset() =>
        _aiSuggestionService is not null && !IsAiBusy && !IsPreferenceHistoryResetPending;

    private bool CanConfirmPreferenceHistoryReset() =>
        _aiSuggestionService is not null && !IsAiBusy && IsPreferenceHistoryResetPending;

    private async Task TestAiConnectionAsync()
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        var (cancellation, version) = BeginAiOperation();
        try
        {
            SetAiStatus(AiAvailabilityState.Connecting, "Testing the configured Ollama endpoint…");
            var result = await _aiSuggestionService.TestConnectionAsync(Draft.ToSettings().Ai, cancellation.Token);
            if (IsCurrentAiOperation(cancellation, version))
            {
                PublishAiConnection(result, selectFirstModel: false);
            }
        }
        catch (ConfigurationValidationException)
        {
            SetAiStatus(AiAvailabilityState.Unavailable, "AI settings are invalid. Correct the endpoint or timeout and try again.");
        }
        catch (OperationCanceledException)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                SetAiStatus(AiAvailabilityState.RequestCancelled, "The AI connection test was cancelled.");
            }
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                SetAiStatus(AiAvailabilityState.Unavailable, "The AI connection test failed safely. Check the configured endpoint and try again.");
            }
        }
        finally
        {
            EndAiOperation(cancellation, version);
        }
    }

    private async Task DiscoverAiModelsAsync()
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        var (cancellation, version) = BeginAiOperation();
        try
        {
            SetAiStatus(AiAvailabilityState.Connecting, "Discovering installed Ollama models…");
            var result = await _aiSuggestionService.DiscoverModelsAsync(Draft.ToSettings().Ai, cancellation.Token);
            if (IsCurrentAiOperation(cancellation, version))
            {
                PublishAiConnection(result, selectFirstModel: true);
            }
        }
        catch (ConfigurationValidationException)
        {
            SetAiStatus(AiAvailabilityState.Unavailable, "AI settings are invalid. Correct the endpoint or timeout and try again.");
        }
        catch (OperationCanceledException)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                SetAiStatus(AiAvailabilityState.RequestCancelled, "The model discovery request was cancelled.");
            }
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                SetAiStatus(AiAvailabilityState.Unavailable, "Model discovery failed safely. Check the configured endpoint and try again.");
            }
        }
        finally
        {
            EndAiOperation(cancellation, version);
        }
    }

    private void RequestPreferenceHistoryReset()
    {
        IsPreferenceHistoryResetPending = true;
        StatusText = "Confirm reset to delete only OpenSorSe local AI decision history. Scanned files and other application data will not change.";
    }

    private void CancelPreferenceHistoryReset()
    {
        IsPreferenceHistoryResetPending = false;
        StatusText = "Local AI decision-history reset was cancelled.";
    }

    private async Task ConfirmPreferenceHistoryResetAsync()
    {
        if (_aiSuggestionService is null || !IsPreferenceHistoryResetPending)
        {
            return;
        }

        var (cancellation, version) = BeginAiOperation();
        try
        {
            await _aiSuggestionService.ResetDecisionHistoryAsync(cancellation.Token);
            if (IsCurrentAiOperation(cancellation, version))
            {
                IsPreferenceHistoryResetPending = false;
                StatusText = "Local AI decision history and preference signals were reset. No scanned file or other OpenSorSe store changed.";
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                StatusText = "Local AI decision-history reset was cancelled.";
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                StatusText = "Local AI decision history could not be reset. Existing application data was preserved.";
            }
        }
        catch (Exception)
        {
            if (version == Volatile.Read(ref _aiOperationVersion))
            {
                StatusText = "Local AI decision history could not be reset. Existing application data was preserved.";
            }
        }
        finally
        {
            EndAiOperation(cancellation, version);
        }
    }

    private void CancelAiOperation()
    {
        var cancellation = Interlocked.Exchange(ref _aiOperationCancellation, null);
        if (cancellation is null)
        {
            return;
        }

        Interlocked.Increment(ref _aiOperationVersion);
        cancellation.Cancel();
        IsAiBusy = false;
        SetAiStatus(AiAvailabilityState.RequestCancelled, "The active optional AI operation was cancelled.");
    }

    private (CancellationTokenSource Cancellation, long Version) BeginAiOperation()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref _aiOperationCancellation, cancellation);
        previous?.Cancel();
        var version = Interlocked.Increment(ref _aiOperationVersion);
        IsAiBusy = true;
        return (cancellation, version);
    }

    private bool IsCurrentAiOperation(CancellationTokenSource cancellation, long version) =>
        !cancellation.IsCancellationRequested &&
        ReferenceEquals(_aiOperationCancellation, cancellation) &&
        version == Volatile.Read(ref _aiOperationVersion);

    private void EndAiOperation(CancellationTokenSource cancellation, long version)
    {
        if (ReferenceEquals(_aiOperationCancellation, cancellation))
        {
            _aiOperationCancellation = null;
        }

        if (version == Volatile.Read(ref _aiOperationVersion))
        {
            IsAiBusy = false;
        }

        cancellation.Dispose();
    }

    /// <summary>Releases optional AI cancellation resources.</summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        var cancellation = Interlocked.Exchange(ref _aiOperationCancellation, null);
        Interlocked.Increment(ref _aiOperationVersion);
        cancellation?.Cancel();
        cancellation?.Dispose();
        _isDisposed = true;
    }

    private void PublishAiConnection(AiConnectionResult result, bool selectFirstModel)
    {
        _availableAiModels.Clear();
        foreach (var model in result.Models)
        {
            _availableAiModels.Add(model.Id);
        }

        if (selectFirstModel && string.IsNullOrWhiteSpace(Draft.SelectedAiModel) && _availableAiModels.Count > 0)
        {
            Draft.SelectedAiModel = _availableAiModels[0];
            SetAiStatus(AiAvailabilityState.ModelSelected, $"Ollama model '{Draft.SelectedAiModel}' selected. Save Settings to persist it.");
            return;
        }

        SetAiStatus(result.State, result.Message);
    }

    private void SetAiStatus(AiAvailabilityState state, string message)
    {
        AiAvailabilityState = state;
        AiStatusText = message;
        OnPropertyChanged(nameof(AiAvailabilityState));
        OnPropertyChanged(nameof(AiStatusText));
    }
}
