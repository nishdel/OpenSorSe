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
    private bool _metadataExtractionEnabled = true;
    private bool _ocrEnabled;
    private bool _ocrOnlyWhenNativeTextUnavailable = true;
    private int _maximumOcrPages = 25;
    private int _maximumContentFileSizeMiB = 50;
    private string _ocrLanguage = "eng";
    private int _maximumOcrDurationSeconds = 120;
    private bool _backgroundContentProcessingEnabled;

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

    /// <summary>Gets or sets whether bounded local metadata and native text are extracted.</summary>
    public bool MetadataExtractionEnabled
    {
        get => _metadataExtractionEnabled;
        set => SetProperty(ref _metadataExtractionEnabled, value);
    }

    /// <summary>Gets or sets whether local OCR Beta is enabled.</summary>
    public bool OcrEnabled
    {
        get => _ocrEnabled;
        set => SetProperty(ref _ocrEnabled, value);
    }

    /// <summary>Gets or sets whether reliable native text prevents unnecessary OCR.</summary>
    public bool OcrOnlyWhenNativeTextUnavailable
    {
        get => _ocrOnlyWhenNativeTextUnavailable;
        set => SetProperty(ref _ocrOnlyWhenNativeTextUnavailable, value);
    }

    /// <summary>Gets or sets the maximum pages considered per OCR document.</summary>
    public int MaximumOcrPages
    {
        get => _maximumOcrPages;
        set => SetProperty(ref _maximumOcrPages, value);
    }

    /// <summary>Gets or sets the maximum local content input size in MiB.</summary>
    public int MaximumContentFileSizeMiB
    {
        get => _maximumContentFileSizeMiB;
        set => SetProperty(ref _maximumContentFileSizeMiB, value);
    }

    /// <summary>Gets or sets the local OCR language identifier.</summary>
    public string OcrLanguage
    {
        get => _ocrLanguage;
        set => SetProperty(ref _ocrLanguage, value ?? string.Empty);
    }

    /// <summary>Gets or sets the maximum OCR duration per file.</summary>
    public int MaximumOcrDurationSeconds
    {
        get => _maximumOcrDurationSeconds;
        set => SetProperty(ref _maximumOcrDurationSeconds, value);
    }

    /// <summary>Gets or sets whether bounded content work may continue in the background.</summary>
    public bool BackgroundContentProcessingEnabled
    {
        get => _backgroundContentProcessingEnabled;
        set => SetProperty(ref _backgroundContentProcessingEnabled, value);
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
            MetadataExtractionEnabled = settings.Content.MetadataExtractionEnabled,
            OcrEnabled = settings.Content.OcrEnabled,
            OcrOnlyWhenNativeTextUnavailable = settings.Content.OcrOnlyWhenNativeTextUnavailable,
            MaximumOcrPages = settings.Content.MaximumPagesPerDocument,
            MaximumContentFileSizeMiB = settings.Content.MaximumFileSizeMiB,
            OcrLanguage = settings.Content.OcrLanguage,
            MaximumOcrDurationSeconds = settings.Content.MaximumOcrDurationSeconds,
            BackgroundContentProcessingEnabled = settings.Content.BackgroundProcessingEnabled,
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
        Content = new ContentSettings
        {
            MetadataExtractionEnabled = MetadataExtractionEnabled,
            OcrEnabled = OcrEnabled,
            OcrOnlyWhenNativeTextUnavailable = OcrOnlyWhenNativeTextUnavailable,
            MaximumPagesPerDocument = MaximumOcrPages,
            MaximumFileSizeMiB = MaximumContentFileSizeMiB,
            OcrLanguage = OcrLanguage.Trim(),
            MaximumOcrDurationSeconds = MaximumOcrDurationSeconds,
            BackgroundProcessingEnabled = BackgroundContentProcessingEnabled,
        },
        };
    }
}
