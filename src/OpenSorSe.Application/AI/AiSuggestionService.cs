using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenSorSe.Application.Features;
using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;

namespace OpenSorSe.Application.AI;

/// <summary>
/// Coordinates capability gates, bounded prompts, optional provider requests, complete response validation, and local review history.
/// </summary>
public sealed class AiSuggestionService : IAiSuggestionService
{
    private const string ProviderName = "Ollama";
    private readonly IDecisionHistoryStore _decisionHistoryStore;
    private readonly ILogger _logger;
    private readonly IAiPromptBuilder _promptBuilder;
    private readonly IAiSuggestionProvider _provider;
    private readonly IAiResponseParser _responseParser;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes the coordinator with default application-owned prompt and response components.</summary>
    public AiSuggestionService(
        IAiSuggestionProvider provider,
        IDecisionHistoryStore decisionHistoryStore,
        ILoggingService loggingService,
        TimeProvider? timeProvider = null)
        : this(provider, decisionHistoryStore, new AiPromptBuilder(), new AiResponseParser(), loggingService, timeProvider)
    {
    }

    /// <summary>Initializes the coordinator with explicit testable prompt and response components.</summary>
    public AiSuggestionService(
        IAiSuggestionProvider provider,
        IDecisionHistoryStore decisionHistoryStore,
        IAiPromptBuilder promptBuilder,
        IAiResponseParser responseParser,
        ILoggingService loggingService,
        TimeProvider? timeProvider = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _decisionHistoryStore = decisionHistoryStore ?? throw new ArgumentNullException(nameof(decisionHistoryStore));
        _promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
        _responseParser = responseParser ?? throw new ArgumentNullException(nameof(responseParser));
        _logger = (loggingService ?? throw new ArgumentNullException(nameof(loggingService))).CreateLogger(nameof(AiSuggestionService));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<AiConnectionResult> TestConnectionAsync(ApplicationSettings settings, CancellationToken cancellationToken) =>
        GetConnectionAsync(settings, cancellationToken);

    /// <inheritdoc />
    public Task<AiConnectionResult> DiscoverModelsAsync(ApplicationSettings settings, CancellationToken cancellationToken) =>
        GetConnectionAsync(settings, cancellationToken);

    /// <inheritdoc />
    public async Task<AiFileRenameResult> GenerateFileRenameAsync(
        AiFileRenameRequest request,
        AiSettings settings,
        CancellationToken cancellationToken)
    {
        if (!TryValidateReadySettings(settings, AiCapability.FileRenameSuggestions, out var state, out var message))
        {
            return new AiFileRenameResult(state, message, null);
        }

        if (!IsValidRenameContext(request))
        {
            return new AiFileRenameResult(
                AiAvailabilityState.InvalidContext,
                "Select one valid known result file before requesting a rename suggestion.",
                null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new AiFileRenameResult(AiAvailabilityState.RequestCancelled, "The AI rename request was cancelled.", null);
        }

        var preferences = await LoadPreferencesAsync(settings.PreferenceAdaptationEnabled, cancellationToken).ConfigureAwait(false);
        var prompt = _promptBuilder.BuildFileRenamePrompt(request, preferences);
        var providerResult = await GenerateSafelyAsync(
            new AiProviderGenerationRequest(
                AiSuggestionKind.FileRename,
                settings.Endpoint,
                settings.SelectedModel!,
                prompt.Prompt,
                TimeSpan.FromSeconds(settings.RequestTimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);
        if (!providerResult.IsSuccess)
        {
            return new AiFileRenameResult(MapFailure(providerResult.FailureKind), providerResult.Message, null, prompt.WasInputBounded);
        }

        var parsed = _responseParser.ParseFileRename(providerResult.StructuredJson!, request);
        if (!parsed.IsValid)
        {
            _logger.LogWarning("An AI file-rename response was rejected during validation: {Reason}", parsed.Message);
            return new AiFileRenameResult(AiAvailabilityState.ResponseInvalid, parsed.Message, null, prompt.WasInputBounded);
        }

        if (parsed.IsNoSuggestion)
        {
            return new AiFileRenameResult(AiAvailabilityState.NoSuggestion, parsed.Message, null, prompt.WasInputBounded);
        }

        var value = parsed.Value!;
        var suggestion = new AiFileRenameSuggestion(
            $"rename-suggestion:{Guid.NewGuid():N}",
            value.SourceFileId,
            value.SuggestedFileName,
            value.Reason,
            value.Confidence,
            ProviderName,
            settings.SelectedModel!,
            _timeProvider.GetUtcNow());
        var boundedSuffix = prompt.WasInputBounded ? " Some nearby-name context was deterministically bounded." : string.Empty;
        return new AiFileRenameResult(
            AiAvailabilityState.ModelSelected,
            $"AI-generated rename suggestion available for review. It is unverified and no file was changed.{boundedSuffix}",
            suggestion,
            prompt.WasInputBounded);
    }

    /// <inheritdoc />
    public async Task<AiFolderStructureResult> GenerateFolderStructureAsync(
        AiFolderStructureRequest request,
        AiSettings settings,
        CancellationToken cancellationToken)
    {
        if (!TryValidateReadySettings(settings, AiCapability.FolderStructureSuggestions, out var state, out var message))
        {
            return new AiFolderStructureResult(state, message, null);
        }

        if (!IsValidFolderContext(request))
        {
            return new AiFolderStructureResult(
                AiAvailabilityState.InvalidContext,
                "Select at least one valid known result file before requesting a folder-structure suggestion.",
                null);
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return new AiFolderStructureResult(AiAvailabilityState.RequestCancelled, "The AI folder-structure request was cancelled.", null);
        }

        var preferences = await LoadPreferencesAsync(settings.PreferenceAdaptationEnabled, cancellationToken).ConfigureAwait(false);
        var prompt = _promptBuilder.BuildFolderStructurePrompt(request, preferences);
        var providerResult = await GenerateSafelyAsync(
            new AiProviderGenerationRequest(
                AiSuggestionKind.FolderStructure,
                settings.Endpoint,
                settings.SelectedModel!,
                prompt.Prompt,
                TimeSpan.FromSeconds(settings.RequestTimeoutSeconds)),
            cancellationToken).ConfigureAwait(false);
        if (!providerResult.IsSuccess)
        {
            return new AiFolderStructureResult(MapFailure(providerResult.FailureKind), providerResult.Message, null, prompt.WasInputBounded);
        }

        var includedIds = prompt.IncludedSourceIds.ToHashSet(StringComparer.Ordinal);
        var includedFiles = Array.AsReadOnly(request.Files.Where(file => includedIds.Contains(file.Id)).ToArray());
        var parsed = _responseParser.ParseFolderStructure(providerResult.StructuredJson!, includedFiles);
        if (!parsed.IsValid)
        {
            _logger.LogWarning("An AI folder-structure response was rejected during validation: {Reason}", parsed.Message);
            return new AiFolderStructureResult(AiAvailabilityState.ResponseInvalid, parsed.Message, null, prompt.WasInputBounded);
        }

        if (parsed.IsNoSuggestion)
        {
            return new AiFolderStructureResult(AiAvailabilityState.NoSuggestion, parsed.Message, null, prompt.WasInputBounded);
        }

        var value = parsed.Value!;
        var plan = new AiFolderStructurePlan(
            $"folder-plan:{Guid.NewGuid():N}",
            value.Folders,
            value.Items,
            value.Reason,
            ProviderName,
            settings.SelectedModel!,
            _timeProvider.GetUtcNow());
        var boundedSuffix = prompt.WasInputBounded ? " The metadata context was deterministically bounded." : string.Empty;
        return new AiFolderStructureResult(
            AiAvailabilityState.ModelSelected,
            $"AI-generated folder-structure suggestion available for review. It is unverified and cannot create folders or move files.{boundedSuffix}",
            plan,
            prompt.WasInputBounded);
    }

    /// <inheritdoc />
    public async Task<AiDecisionResult> RecordDecisionAsync(
        AiSuggestionDecision decision,
        AiSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        var capability = decision.Kind switch
        {
            AiSuggestionDecisionKind.Rename => AiCapability.FileRenameSuggestions,
            AiSuggestionDecisionKind.FolderStructure => AiCapability.FolderStructureSuggestions,
            _ => (AiCapability?)null,
        };
        if (capability is null)
        {
            return new AiDecisionResult(AiAvailabilityState.InvalidContext, "That AI decision type is not supported in v0.9.1.");
        }

        if (!TryValidateEnabled(settings, capability.Value, out var state, out var message))
        {
            return new AiDecisionResult(state, message);
        }

        if (string.IsNullOrWhiteSpace(decision.SuggestedValue) || decision.SuggestedValue.Length > 4096 ||
            string.IsNullOrWhiteSpace(decision.Provider) || string.IsNullOrWhiteSpace(decision.Model))
        {
            return new AiDecisionResult(AiAvailabilityState.InvalidContext, "The AI review decision is incomplete and was not saved.");
        }

        try
        {
            await _decisionHistoryStore.AppendAsync(decision, cancellationToken).ConfigureAwait(false);
            return new AiDecisionResult(AiAvailabilityState.ModelSelected, "The local review decision was saved. No file or folder was changed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiDecisionResult(AiAvailabilityState.RequestCancelled, "Saving the local AI review decision was cancelled.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            _logger.LogWarning(exception, "The local AI review decision could not be saved.");
            return new AiDecisionResult(AiAvailabilityState.Unavailable, "The local AI review decision could not be saved. No file or folder was changed.");
        }
    }

    /// <inheritdoc />
    public async Task<AiDecisionResult> ResetDecisionHistoryAsync(ApplicationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!FeatureAccess.IsEnabled(settings, FeatureRequirement.ForAdvancedAi()))
        {
            return new AiDecisionResult(
                settings.Ai.Enabled ? AiAvailabilityState.CapabilityDisabled : AiAvailabilityState.Disabled,
                "Enable AI and advanced features before resetting local AI review history.");
        }

        try
        {
            await _decisionHistoryStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            return new AiDecisionResult(AiAvailabilityState.ModelSelected, "Local AI review history was reset. No scanned file or other OpenSorSe store changed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiDecisionResult(AiAvailabilityState.RequestCancelled, "Resetting local AI review history was cancelled.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
        {
            _logger.LogWarning(exception, "Local AI review history could not be reset.");
            return new AiDecisionResult(AiAvailabilityState.Unavailable, "Local AI review history could not be reset. Existing application data was preserved.");
        }
    }

    private async Task<AiConnectionResult> GetConnectionAsync(ApplicationSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!FeatureAccess.IsEnabled(settings, FeatureRequirement.ForAdvancedAi()))
        {
            return new AiConnectionResult(
                settings.Ai.Enabled ? AiAvailabilityState.CapabilityDisabled : AiAvailabilityState.Disabled,
                "Enable AI and advanced features before using provider diagnostics.",
                Array.Empty<AiModel>());
        }

        try
        {
            settings.Validate();
        }
        catch (ConfigurationValidationException)
        {
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "AI settings are invalid.", Array.Empty<AiModel>());
        }

        AiConnectionResult connection;
        try
        {
            connection = await _provider.GetConnectionAsync(settings.Ai, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiConnectionResult(AiAvailabilityState.RequestCancelled, "The Ollama connection test was cancelled.", Array.Empty<AiModel>());
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or InvalidDataException or NotSupportedException)
        {
            _logger.LogWarning(exception, "The AI provider failed during a connection test.");
            return new AiConnectionResult(AiAvailabilityState.Unavailable, "Ollama is unavailable at the configured endpoint.", Array.Empty<AiModel>());
        }

        if (connection.State is not (AiAvailabilityState.Connected or AiAvailabilityState.ModelSelected or AiAvailabilityState.NoModelsAvailable))
        {
            return connection;
        }

        if (connection.Models.Count == 0)
        {
            return connection with { State = AiAvailabilityState.NoModelsAvailable, Message = "Ollama is connected, but no installed models were found." };
        }

        if (!string.IsNullOrWhiteSpace(settings.Ai.SelectedModel) &&
            connection.Models.Any(model => string.Equals(model.Id, settings.Ai.SelectedModel, StringComparison.Ordinal)))
        {
            return connection with { State = AiAvailabilityState.ModelSelected, Message = $"Ollama is connected and '{settings.Ai.SelectedModel}' is selected." };
        }

        return connection with { State = AiAvailabilityState.Connected, Message = "Ollama is connected. Select one discovered model before requesting suggestions." };
    }

    private async Task<AiProviderGenerationResult> GenerateSafelyAsync(
        AiProviderGenerationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _provider.GenerateAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Cancelled, "The AI suggestion request was cancelled.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or InvalidDataException or NotSupportedException)
        {
            _logger.LogWarning(exception, "The AI provider failed before a response could be validated.");
            return new AiProviderGenerationResult(null, AiProviderFailureKind.Unavailable, "The AI provider failed safely. Check Ollama and the configured model.");
        }
    }

    private async Task<AiPreferenceSummary> LoadPreferencesAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (!enabled)
        {
            return EmptyPreferences();
        }

        try
        {
            return AiPreferenceAggregator.Build(await _decisionHistoryStore.LoadAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception, "Local AI decision history could not be loaded. Suggestions will continue without preference context.");
            return EmptyPreferences();
        }
    }

    private static AiPreferenceSummary EmptyPreferences() =>
        new(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

    private static bool TryValidateReadySettings(
        AiSettings settings,
        AiCapability capability,
        out AiAvailabilityState state,
        out string message)
    {
        if (!TryValidateEnabled(settings, capability, out state, out message))
        {
            return false;
        }

        try
        {
            settings.Validate();
        }
        catch (ConfigurationValidationException)
        {
            state = AiAvailabilityState.Unavailable;
            message = "AI settings are invalid. Check the endpoint and request timeout.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(settings.SelectedModel))
        {
            state = AiAvailabilityState.NoModelsAvailable;
            message = "Select an installed Ollama model in advanced Settings before requesting suggestions.";
            return false;
        }

        state = AiAvailabilityState.ModelSelected;
        message = string.Empty;
        return true;
    }

    private static bool TryValidateEnabled(
        AiSettings settings,
        AiCapability capability,
        out AiAvailabilityState state,
        out string message)
    {
        if (settings is null || !settings.Enabled)
        {
            state = AiAvailabilityState.Disabled;
            message = "AI features are disabled in Settings.";
            return false;
        }

        if (!settings.IsCapabilityEnabled(capability))
        {
            state = AiAvailabilityState.CapabilityDisabled;
            message = capability == AiCapability.FileRenameSuggestions
                ? "File rename suggestions are disabled in Settings."
                : "Folder structure suggestions are disabled in Settings.";
            return false;
        }

        state = AiAvailabilityState.ModelSelected;
        message = string.Empty;
        return true;
    }

    private static bool IsValidRenameContext(AiFileRenameRequest? request) =>
        request?.File is { } file &&
        request.SiblingFileNames is not null &&
        !string.IsNullOrWhiteSpace(file.Id) && file.Id.Length <= 128 &&
        !string.IsNullOrWhiteSpace(file.DisplayFileName) && file.DisplayFileName.Length <= 255 &&
        file.NormalizedExtension is not null && file.NormalizedExtension.Length <= 32;

    private static bool IsValidFolderContext(AiFolderStructureRequest? request)
    {
        if (request?.Files is null || request.ExistingFolderNames is null || request.Files.Count == 0)
        {
            return false;
        }

        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in request.Files)
        {
            if (file is null || string.IsNullOrWhiteSpace(file.Id) || file.Id.Length > 128 ||
                string.IsNullOrWhiteSpace(file.DisplayFileName) || file.DisplayFileName.Length > 255 ||
                !identities.Add(file.Id))
            {
                return false;
            }
        }

        return true;
    }

    private static AiAvailabilityState MapFailure(AiProviderFailureKind failure) => failure switch
    {
        AiProviderFailureKind.Cancelled => AiAvailabilityState.RequestCancelled,
        AiProviderFailureKind.ModelUnavailable => AiAvailabilityState.ModelUnavailable,
        AiProviderFailureKind.InvalidResponse or AiProviderFailureKind.UnsupportedResponse => AiAvailabilityState.ResponseInvalid,
        _ => AiAvailabilityState.Unavailable,
    };
}
