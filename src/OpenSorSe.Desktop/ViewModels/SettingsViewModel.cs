using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.AI;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Edits the supported v0.1 application settings and persists only through the configuration service.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private readonly IAiSuggestionService? _aiSuggestionService;
    private readonly ObservableCollection<string> _availableAiModels = [];
    private SettingsDraft _draft;
    private bool _restartRequired;
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
        AvailableAiModels = new ReadOnlyObservableCollection<string>(_availableAiModels);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        CancelCommand = new RelayCommand(DiscardChanges);
        TestAiConnectionCommand = new AsyncRelayCommand(TestAiConnectionAsync, CanUseAiService);
        DiscoverAiModelsCommand = new AsyncRelayCommand(DiscoverAiModelsAsync, CanUseAiService);
        ResetPreferenceHistoryCommand = new AsyncRelayCommand(ResetPreferenceHistoryAsync, CanUseAiService);
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
    /// Gets the documented v0.1 log levels users may select.
    /// </summary>
    public IReadOnlyList<LogLevel> AvailableLogLevels { get; } = Enum.GetValues<LogLevel>();

    /// <summary>Gets models discovered from the current draft endpoint.</summary>
    public ReadOnlyObservableCollection<string> AvailableAiModels { get; }

    /// <summary>Gets the current optional AI availability state.</summary>
    public AiAvailabilityState AiAvailabilityState { get; private set; } = AiAvailabilityState.Disabled;

    /// <summary>Gets a concise explanation of the optional AI integration state.</summary>
    public string AiStatusText { get; private set; } = "AI assistance is disabled until enabled and configured.";

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

    /// <summary>Gets the command that clears locally persisted preference-adaptation decisions.</summary>
    public IAsyncRelayCommand ResetPreferenceHistoryCommand { get; }

    /// <summary>
    /// Reloads the editable draft from the current persisted configuration.
    /// </summary>
    public void Load()
    {
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
        Draft = SettingsDraft.FromSettings(new ApplicationSettings());
        RestartRequired = false;
        StatusText = "Default settings restored. Save to persist them.";
        SetAiStatus(AiAvailabilityState.Disabled, "AI assistance is disabled until enabled and configured.");
    }

    private void DiscardChanges()
    {
        Draft = SettingsDraft.FromSettings(_configurationService.Current);
        RestartRequired = false;
        StatusText = "Unsaved changes discarded.";
    }

    private bool CanUseAiService() => _aiSuggestionService is not null;

    private async Task TestAiConnectionAsync()
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        try
        {
            SetAiStatus(AiAvailabilityState.Connecting, "Testing the configured Ollama endpoint…");
            var result = await _aiSuggestionService.TestConnectionAsync(Draft.ToSettings().Ai, CancellationToken.None);
            PublishAiConnection(result, selectFirstModel: false);
        }
        catch (ConfigurationValidationException)
        {
            SetAiStatus(AiAvailabilityState.Unavailable, "AI settings are invalid. Correct the endpoint or timeout and try again.");
        }
        catch (OperationCanceledException)
        {
            SetAiStatus(AiAvailabilityState.RequestCancelled, "The AI connection test was cancelled.");
        }
    }

    private async Task DiscoverAiModelsAsync()
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        try
        {
            SetAiStatus(AiAvailabilityState.Connecting, "Discovering installed Ollama models…");
            var result = await _aiSuggestionService.DiscoverModelsAsync(Draft.ToSettings().Ai, CancellationToken.None);
            PublishAiConnection(result, selectFirstModel: true);
        }
        catch (ConfigurationValidationException)
        {
            SetAiStatus(AiAvailabilityState.Unavailable, "AI settings are invalid. Correct the endpoint or timeout and try again.");
        }
        catch (OperationCanceledException)
        {
            SetAiStatus(AiAvailabilityState.RequestCancelled, "The model discovery request was cancelled.");
        }
    }

    private async Task ResetPreferenceHistoryAsync()
    {
        if (_aiSuggestionService is null)
        {
            return;
        }

        await _aiSuggestionService.ResetDecisionHistoryAsync(CancellationToken.None);
        StatusText = "Local AI decision history and preference signals were reset.";
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
