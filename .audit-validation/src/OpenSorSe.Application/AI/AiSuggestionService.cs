using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.AI;

/// <summary>
/// Coordinates optional provider requests, untrusted-output validation, and local preference adaptation.
/// </summary>
public sealed class AiSuggestionService : IAiSuggestionService
{
    private const string ProviderName = "Ollama";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IDecisionHistoryStore _decisionHistoryStore;
    private readonly ILogger _logger;
    private readonly IAiSuggestionProvider _provider;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes the application-owned AI coordinator.
    /// </summary>
    /// <param name="provider">The provider-neutral transport boundary.</param>
    /// <param name="decisionHistoryStore">The local inspectable decision-history persistence boundary.</param>
    /// <param name="loggingService">The central redacted diagnostic logging service.</param>
    /// <param name="timeProvider">The time source used to stamp application-owned values.</param>
    public AiSuggestionService(
        IAiSuggestionProvider provider,
        IDecisionHistoryStore decisionHistoryStore,
        ILoggingService loggingService,
        TimeProvider? timeProvider = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _decisionHistoryStore = decisionHistoryStore ?? throw new ArgumentNullException(nameof(decisionHistoryStore));
        _logger = (loggingService ?? throw new ArgumentNullException(nameof(loggingService))).CreateLogger(nameof(AiSuggestionService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<AiConnectionResult> TestConnectionAsync(AiSettings settings, CancellationToken cancellationToken) =>
        GetConnectionAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task<AiConnectionResult> DiscoverModelsAsync(AiSettings settings, CancellationToken cancellationToken) =>
        GetConnectionAsync(settings, cancellationToken);

    /// <inheritdoc />
    public async Task<AiFileSuggestionResult> GenerateFileSuggestionAsync(
        AiFileSuggestionRequest request,
        AiSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.File);
        ArgumentNullException.ThrowIfNull(request.ExistingFolderNames);
        ArgumentNullException.ThrowIfNull(request.SiblingFileNames);
        if (!TryValidateReadySettings(settings, out var unavailable))
        {
            return new AiFileSuggestionResult(unavailable.State, unavailable.Message, null);
        }

        var preferences = await LoadPreferencesAsync(settings.PreferenceAdaptationEnabled, cancellationToken).ConfigureAwait(false);
        var providerResult = await _provider.GenerateAsync(
            new AiProviderGenerationRequest(
                AiSuggestionKind.FileOrganization,
                settings.Endpoint,
                settings.SelectedModel!,
                BuildFilePrompt(request, preferences),
                TimeSpan.FromSeconds(settings.RequestTimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);
        if (!providerResult.IsSuccess)
        {
            return new AiFileSuggestionResult(MapFailure(providerResult.FailureKind), providerResult.Message, null);
        }

        if (!TryParseFileSuggestion(providerResult.StructuredJson!, request, settings.SelectedModel!, out var suggestion, out var error))
        {
            _logger.LogWarning("An AI file-organization response was rejected during validation: {Reason}", error);
            return new AiFileSuggestionResult(AiAvailabilityState.ResponseInvalid, error, null);
        }

        return new AiFileSuggestionResult(AiAvailabilityState.ModelSelected, "Suggestion available for review. It will not rename or move a file.", suggestion);
    }

    /// <inheritdoc />
    public async Task<AiFolderStructureResult> GenerateFolderStructureAsync(
        AiFolderStructureRequest request,
        AiSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Files);
        ArgumentNullException.ThrowIfNull(request.ExistingFolderNames);
        if (request.Files.Count is 0 or > 25)
        {
            return new AiFolderStructureResult(AiAvailabilityState.ResponseInvalid, "Select between 1 and 25 result files for a folder-structure preview.", null);
        }

        if (!TryValidateReadySettings(settings, out var unavailable))
        {
            return new AiFolderStructureResult(unavailable.State, unavailable.Message, null);
        }

        var preferences = await LoadPreferencesAsync(settings.PreferenceAdaptationEnabled, cancellationToken).ConfigureAwait(false);
        var providerResult = await _provider.GenerateAsync(
            new AiProviderGenerationRequest(
                AiSuggestionKind.FolderStructure,
                settings.Endpoint,
                settings.SelectedModel!,
                BuildFolderStructurePrompt(request, preferences),
                TimeSpan.FromSeconds(settings.RequestTimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);
        if (!providerResult.IsSuccess)
        {
            return new AiFolderStructureResult(MapFailure(providerResult.FailureKind), providerResult.Message, null);
        }

        if (!TryParseFolderStructure(providerResult.StructuredJson!, request, settings.SelectedModel!, out var plan, out var error))
        {
            _logger.LogWarning("An AI folder-structure response was rejected during validation: {Reason}", error);
            return new AiFolderStructureResult(AiAvailabilityState.ResponseInvalid, error, null);
        }

        return new AiFolderStructureResult(AiAvailabilityState.ModelSelected, "Folder-structure preview available. It cannot create folders or move files.", plan);
    }

    /// <inheritdoc />
    public Task RecordDecisionAsync(AiSuggestionDecision decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (string.IsNullOrWhiteSpace(decision.SuggestedValue) || string.IsNullOrWhiteSpace(decision.Provider) || string.IsNullOrWhiteSpace(decision.Model))
        {
            throw new ArgumentException("A complete decision record is required.", nameof(decision));
        }

        return _decisionHistoryStore.AppendAsync(decision, cancellationToken);
    }

    /// <inheritdoc />
    public Task ResetDecisionHistoryAsync(CancellationToken cancellationToken) => _decisionHistoryStore.ClearAsync(cancellationToken);

    private async Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        try
        {
            settings.Validate();
        }
        catch (ConfigurationValidationException)
        {
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "AI settings are invalid.", Array.Empty<AiModel>());
        }

        if (!settings.Enabled)
        {
            return new AiConnectionResult(AiAvailabilityState.Disabled, "AI assistance is disabled in Settings.", Array.Empty<AiModel>());
        }

        var connection = await _provider.GetConnectionAsync(settings, cancellationToken).ConfigureAwait(false);
        if (connection.State != AiAvailabilityState.Connected && connection.State != AiAvailabilityState.ModelSelected && connection.State != AiAvailabilityState.NoModelsAvailable)
        {
            return connection;
        }

        if (connection.Models.Count == 0)
        {
            return connection with { State = AiAvailabilityState.NoModelsAvailable, Message = "Ollama is connected, but no installed models were found." };
        }

        if (!string.IsNullOrWhiteSpace(settings.SelectedModel) && connection.Models.Any(model => string.Equals(model.Id, settings.SelectedModel, StringComparison.Ordinal)))
        {
            return connection with { State = AiAvailabilityState.ModelSelected, Message = $"Ollama is connected and '{settings.SelectedModel}' is selected." };
        }

        return connection with { State = AiAvailabilityState.Connected, Message = "Ollama is connected. Select one discovered model before requesting suggestions." };
    }

    private async Task<AiPreferenceSummary> LoadPreferencesAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (!enabled)
        {
            return new AiPreferenceSummary(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        }

        try
        {
            return AiPreferenceAggregator.Build(await _decisionHistoryStore.LoadAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException)
        {
            _logger.LogWarning(exception, "Local AI decision history could not be loaded. Suggestions will continue without preference context.");
            return new AiPreferenceSummary(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        }
    }

    private static bool TryValidateReadySettings(AiSettings settings, out AiFileSuggestionResult unavailable)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!settings.Enabled)
        {
            unavailable = new AiFileSuggestionResult(AiAvailabilityState.Disabled, "AI assistance is disabled in Settings.", null);
            return false;
        }

        try
        {
            settings.Validate();
        }
        catch (ConfigurationValidationException)
        {
            unavailable = new AiFileSuggestionResult(AiAvailabilityState.Unavailable, "AI settings are invalid.", null);
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.SelectedModel))
        {
            unavailable = new AiFileSuggestionResult(AiAvailabilityState.NoModelsAvailable, "Select an installed Ollama model in Settings before requesting suggestions.", null);
            return false;
        }

        unavailable = default!;
        return true;
    }

    private static AiAvailabilityState MapFailure(AiProviderFailureKind failure) => failure switch
    {
        AiProviderFailureKind.Cancelled => AiAvailabilityState.RequestCancelled,
        AiProviderFailureKind.InvalidResponse => AiAvailabilityState.ResponseInvalid,
        _ => AiAvailabilityState.Unavailable,
    };

    private bool TryParseFileSuggestion(
        string response,
        AiFileSuggestionRequest request,
        string model,
        out AiFileOrganizationSuggestion suggestion,
        out string error)
    {
        suggestion = default!;
        error = string.Empty;
        FileSuggestionResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FileSuggestionResponse>(response, JsonOptions);
        }
        catch (JsonException)
        {
            error = "Ollama returned malformed structured data. No suggestion was used.";
            return false;
        }

        if (parsed is null || parsed.Tags is null || parsed.Explanation is null || !parsed.HasRequiredProperties)
        {
            error = "Ollama returned an incomplete structured response. No suggestion was used.";
            return false;
        }

        string? normalizedFileName = null;
        if (parsed.FileName is not null)
        {
            if (!AiSuggestionValidator.TryNormalizeFileName(parsed.FileName, request.File.NormalizedExtension, request.SiblingFileNames, out var fileName, out error))
            {
                return false;
            }

            normalizedFileName = fileName;
        }

        if (!AiSuggestionValidator.TryNormalizeTags(parsed.Tags, out var tags, out error) ||
            !AiSuggestionValidator.TryParseCategory(parsed.Category, out var category, out error) ||
            !AiSuggestionValidator.TryNormalizeDestinationFolder(parsed.DestinationFolder, out var destinationFolder, out error))
        {
            return false;
        }

        if (normalizedFileName is null && tags.Count == 0 && category is null && destinationFolder is null)
        {
            error = "Ollama returned no usable suggestion values.";
            return false;
        }

        suggestion = new AiFileOrganizationSuggestion(
            $"suggestion:{Guid.NewGuid():N}",
            request.File.Id,
            normalizedFileName,
            tags,
            category,
            destinationFolder,
            NormalizeExplanation(parsed.Explanation),
            ProviderName,
            model,
            _timeProvider.GetUtcNow());
        return true;
    }

    private bool TryParseFolderStructure(
        string response,
        AiFolderStructureRequest request,
        string model,
        out AiFolderStructurePlan plan,
        out string error)
    {
        plan = default!;
        error = string.Empty;
        FolderStructureResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<FolderStructureResponse>(response, JsonOptions);
        }
        catch (JsonException)
        {
            error = "Ollama returned malformed structured data. No folder structure was used.";
            return false;
        }

        if (parsed is null || parsed.Items is null || parsed.Explanation is null || !parsed.HasRequiredProperties || parsed.Items.Count == 0 || parsed.Items.Count > request.Files.Count)
        {
            error = "Ollama returned an incomplete folder-structure response. No plan was used.";
            return false;
        }

        var files = request.Files.ToDictionary(file => file.Id, StringComparer.Ordinal);
        var items = new List<AiFolderStructurePlanItem>();
        foreach (var item in parsed.Items)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.FileId) || !files.TryGetValue(item.FileId, out var file) ||
                !AiSuggestionValidator.TryNormalizeDestinationFolder(item.DestinationFolder, out var destination, out error) || destination is null ||
                items.Any(existing => string.Equals(existing.FileId, item.FileId, StringComparison.Ordinal)))
            {
                error = string.IsNullOrWhiteSpace(error) ? "The folder-structure plan contains an invalid file or destination." : error;
                return false;
            }

            items.Add(new AiFolderStructurePlanItem(file.Id, file.DisplayFileName, destination));
        }

        plan = new AiFolderStructurePlan(
            $"folder-plan:{Guid.NewGuid():N}",
            Array.AsReadOnly(items.ToArray()),
            NormalizeExplanation(parsed.Explanation),
            ProviderName,
            model,
            _timeProvider.GetUtcNow());
        return true;
    }

    private static string BuildFilePrompt(AiFileSuggestionRequest request, AiPreferenceSummary preferences)
    {
        var context = new
        {
            task = "Organize one file using only supplied metadata. Do not invent file contents.",
            file = new
            {
                id = request.File.Id,
                fileName = request.File.DisplayFileName,
                extension = request.File.NormalizedExtension,
                deterministicCategory = request.File.ClassificationDisplay,
            },
            existingFolderNames = request.ExistingFolderNames.Take(30).ToArray(),
            approvedPreferences = preferences,
            allowedCategories = Enum.GetNames<FileCategory>(),
            requiredResponse = new
            {
                fileName = "string or null; preserve the supplied extension exactly",
                tags = new[] { "string" },
                category = "one allowed category or null",
                destinationFolder = "relative folder path or null; never absolute and never use ..",
                explanation = "concise string; do not claim a confidence score",
            },
        };
        return JsonSerializer.Serialize(context);
    }

    private static string BuildFolderStructurePrompt(AiFolderStructureRequest request, AiPreferenceSummary preferences)
    {
        var context = new
        {
            task = "Propose a preview-only relative folder structure for supplied file metadata. Do not propose moves or filesystem actions.",
            files = request.Files.Select(file => new
            {
                id = file.Id,
                fileName = file.DisplayFileName,
                extension = file.NormalizedExtension,
                deterministicCategory = file.ClassificationDisplay,
            }).ToArray(),
            existingFolderNames = request.ExistingFolderNames.Take(30).ToArray(),
            approvedPreferences = preferences,
            requiredResponse = new
            {
                items = new[] { new { fileId = "input id", destinationFolder = "relative folder path" } },
                explanation = "concise string; do not claim a confidence score",
            },
        };
        return JsonSerializer.Serialize(context);
    }

    private static string NormalizeExplanation(string explanation) => explanation.Trim().Length > 280 ? explanation.Trim()[..280] : explanation.Trim();

    private sealed class FileSuggestionResponse
    {
        public string? FileName { get; init; }

        public List<string>? Tags { get; init; }

        public string? Category { get; init; }

        public string? DestinationFolder { get; init; }

        public string? Explanation { get; init; }

        public bool HasRequiredProperties => Tags is not null && Explanation is not null;
    }

    private sealed class FolderStructureResponse
    {
        public List<FolderStructureItemResponse>? Items { get; init; }

        public string? Explanation { get; init; }

        public bool HasRequiredProperties => Items is not null && Explanation is not null;
    }

    private sealed class FolderStructureItemResponse
    {
        public string? FileId { get; init; }

        public string? DestinationFolder { get; init; }
    }
}
