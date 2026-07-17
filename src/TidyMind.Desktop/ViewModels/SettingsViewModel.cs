using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TidyMind.Core.Configuration;

namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Edits the supported v0.1 application settings and persists only through the configuration service.
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configurationService;
    private SettingsDraft _draft;
    private bool _restartRequired;
    private string _statusText = "Ready";

    /// <summary>
    /// Initializes a settings editor over the already initialized configuration service.
    /// </summary>
    /// <param name="configurationService">The centralized configuration service.</param>
    public SettingsViewModel(IConfigurationService configurationService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _draft = SettingsDraft.FromSettings(_configurationService.Current);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        CancelCommand = new RelayCommand(DiscardChanges);
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

    /// <summary>
    /// Reloads the editable draft from the current persisted configuration.
    /// </summary>
    public void Load()
    {
        Draft = SettingsDraft.FromSettings(_configurationService.Current);
        RestartRequired = false;
        StatusText = "Settings loaded.";
    }

    private async Task SaveAsync()
    {
        try
        {
            var settings = Draft.ToSettings();
            settings.Validate();
            await _configurationService.SaveAsync(settings, CancellationToken.None).ConfigureAwait(false);
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
    }

    private void DiscardChanges()
    {
        Draft = SettingsDraft.FromSettings(_configurationService.Current);
        RestartRequired = false;
        StatusText = "Unsaved changes discarded.";
    }
}
