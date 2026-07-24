using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Configuration;
using System.Globalization;

namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Holds editable application configuration independently from the persisted settings object.
/// </summary>
public sealed class SettingsDraft : ViewModelBase
{
    private bool _fileLoggingEnabled;
    private string? _logDirectoryPath;
    private LogLevel _minimumLogLevel;
    private int _retainedFileCount;
    private bool _showAdvancedFeatures;
    private bool _aiEnabled;
    private bool _fileRenameSuggestionsEnabled;
    private bool _folderStructureSuggestionsEnabled;
    private bool _aiRequestDiagnosticsEnabled;
    private string _aiEndpoint = "http://127.0.0.1:11434";
    private string? _selectedAiModel;
    private int _aiRequestTimeoutSeconds = 30;
    private string _aiRequestTimeoutText = "30";
    private bool _preferenceAdaptationEnabled = true;
    private bool _catalogEnabled;

    /// <summary>Gets or sets whether specialist and troubleshooting interface features are shown.</summary>
    public bool ShowAdvancedFeatures
    {
        get => _showAdvancedFeatures;
        set => SetProperty(ref _showAdvancedFeatures, value);
    }

    /// <summary>
    /// Gets or sets whether local daily file logging is enabled.
    /// </summary>
    public bool FileLoggingEnabled
    {
        get => _fileLoggingEnabled;
        set => SetProperty(ref _fileLoggingEnabled, value);
    }

    /// <summary>
    /// Gets or sets the optional absolute local log directory.
    /// </summary>
    public string? LogDirectoryPath
    {
        get => _logDirectoryPath;
        set => SetProperty(ref _logDirectoryPath, value);
    }

    /// <summary>
    /// Gets or sets the lowest log level retained by configured logging outputs.
    /// </summary>
    public LogLevel MinimumLogLevel
    {
        get => _minimumLogLevel;
        set => SetProperty(ref _minimumLogLevel, value);
    }

    /// <summary>
    /// Gets or sets the number of local daily log files retained.
    /// </summary>
    public int RetainedFileCount
    {
        get => _retainedFileCount;
        set => SetProperty(ref _retainedFileCount, value);
    }

    /// <summary>Gets or sets whether optional local AI suggestions may be requested.</summary>
    public bool AiEnabled
    {
        get => _aiEnabled;
        set => SetProperty(ref _aiEnabled, value);
    }

    /// <summary>Gets or sets whether review-only file-rename suggestions are enabled.</summary>
    public bool FileRenameSuggestionsEnabled
    {
        get => _fileRenameSuggestionsEnabled;
        set => SetProperty(ref _fileRenameSuggestionsEnabled, value);
    }

    /// <summary>Gets or sets whether review-only folder-structure suggestions are enabled.</summary>
    public bool FolderStructureSuggestionsEnabled
    {
        get => _folderStructureSuggestionsEnabled;
        set => SetProperty(ref _folderStructureSuggestionsEnabled, value);
    }

    /// <summary>Gets or sets whether bounded raw AI request diagnostics are retained for this session.</summary>
    public bool AiRequestDiagnosticsEnabled
    {
        get => _aiRequestDiagnosticsEnabled;
        set => SetProperty(ref _aiRequestDiagnosticsEnabled, value);
    }

    /// <summary>Gets or sets the user-configured Ollama-compatible endpoint.</summary>
    public string AiEndpoint
    {
        get => _aiEndpoint;
        set => SetProperty(ref _aiEndpoint, value);
    }

    /// <summary>Gets or sets the model selected from the provider's installed-model list.</summary>
    public string? SelectedAiModel
    {
        get => _selectedAiModel;
        set => SetProperty(ref _selectedAiModel, value);
    }

    /// <summary>Gets or sets the bounded timeout for one optional local AI request.</summary>
    public int AiRequestTimeoutSeconds
    {
        get => _aiRequestTimeoutSeconds;
        set
        {
            if (SetProperty(ref _aiRequestTimeoutSeconds, value))
            {
                AiRequestTimeoutText = value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>Gets or sets timeout entry text so invalid input can be explained predictably.</summary>
    public string AiRequestTimeoutText
    {
        get => _aiRequestTimeoutText;
        set => SetProperty(ref _aiRequestTimeoutText, value ?? string.Empty);
    }

    /// <summary>Gets or sets whether local decision history may influence concise suggestion context.</summary>
    public bool PreferenceAdaptationEnabled
    {
        get => _preferenceAdaptationEnabled;
        set => SetProperty(ref _preferenceAdaptationEnabled, value);
    }

    /// <summary>Gets or sets whether OpenSorSe may retain bounded completed scan metadata in its own local application-data catalog.</summary>
    public bool CatalogEnabled
    {
        get => _catalogEnabled;
        set => SetProperty(ref _catalogEnabled, value);
    }

    /// <summary>
    /// Creates a draft copied from validated application settings.
    /// </summary>
    /// <param name="settings">The settings to copy.</param>
    /// <returns>An independently editable settings draft.</returns>
    public static SettingsDraft FromSettings(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new SettingsDraft
        {
            FileLoggingEnabled = settings.Logging.FileLoggingEnabled,
            LogDirectoryPath = settings.Logging.LogDirectoryPath,
            MinimumLogLevel = settings.Logging.MinimumLevel,
            RetainedFileCount = settings.Logging.RetainedFileCount,
            ShowAdvancedFeatures = settings.Features.ShowAdvancedFeatures,
            AiEnabled = settings.Ai.Enabled,
            FileRenameSuggestionsEnabled = settings.Ai.FileRenameSuggestionsEnabled,
            FolderStructureSuggestionsEnabled = settings.Ai.FolderStructureSuggestionsEnabled,
            AiRequestDiagnosticsEnabled = settings.Ai.RequestDiagnosticsEnabled,
            AiEndpoint = settings.Ai.Endpoint,
            SelectedAiModel = settings.Ai.SelectedModel,
            AiRequestTimeoutSeconds = settings.Ai.RequestTimeoutSeconds,
            AiRequestTimeoutText = settings.Ai.RequestTimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            PreferenceAdaptationEnabled = settings.Ai.PreferenceAdaptationEnabled,
            CatalogEnabled = settings.Catalog.Enabled,
        };
    }

    /// <summary>
    /// Creates validated application settings from this draft.
    /// </summary>
    /// <returns>The settings ready for persistence.</returns>
    public ApplicationSettings ToSettings()
    {
        if (!int.TryParse(AiRequestTimeoutText, NumberStyles.None, CultureInfo.InvariantCulture, out var timeoutSeconds))
        {
            throw new ConfigurationValidationException($"AI request timeout must be a whole number from {AiSettings.MinimumRequestTimeoutSeconds} through {AiSettings.MaximumRequestTimeoutSeconds} seconds.");
        }

        return new ApplicationSettings
        {
        Features = new FeatureSettings
        {
            ShowAdvancedFeatures = ShowAdvancedFeatures,
        },
        Logging = new LoggingSettings
        {
            FileLoggingEnabled = FileLoggingEnabled,
            LogDirectoryPath = string.IsNullOrWhiteSpace(LogDirectoryPath) ? null : LogDirectoryPath.Trim(),
            MinimumLevel = MinimumLogLevel,
            RetainedFileCount = RetainedFileCount,
        },
        Ai = new AiSettings
        {
            Enabled = AiEnabled,
            FileRenameSuggestionsEnabled = FileRenameSuggestionsEnabled,
            FolderStructureSuggestionsEnabled = FolderStructureSuggestionsEnabled,
            RequestDiagnosticsEnabled = AiRequestDiagnosticsEnabled,
            Endpoint = AiEndpoint?.Trim() ?? string.Empty,
            SelectedModel = string.IsNullOrWhiteSpace(SelectedAiModel) ? null : SelectedAiModel.Trim(),
            RequestTimeoutSeconds = timeoutSeconds,
            PreferenceAdaptationEnabled = PreferenceAdaptationEnabled,
        },
        Catalog = new CatalogSettings
        {
            Enabled = CatalogEnabled,
        },
        };
    }
}
