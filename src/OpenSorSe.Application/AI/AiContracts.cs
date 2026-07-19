using OpenSorSe.Application.Models;
using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Application.AI;

/// <summary>
/// Describes the user-visible state of the optional AI integration.
/// </summary>
public enum AiAvailabilityState
{
    /// <summary>AI requests are disabled by the global setting.</summary>
    Disabled,
    /// <summary>The requested capability is independently disabled.</summary>
    CapabilityDisabled,
    /// <summary>The supplied user context is not eligible for the capability.</summary>
    InvalidContext,
    /// <summary>The configured provider could not be reached.</summary>
    Unavailable,
    /// <summary>The provider is being contacted.</summary>
    Connecting,
    /// <summary>The provider is reachable but no model selection has been made.</summary>
    Connected,
    /// <summary>The provider is reachable but exposes no installed models.</summary>
    NoModelsAvailable,
    /// <summary>The configured model is not installed or available.</summary>
    ModelUnavailable,
    /// <summary>A usable model has been selected.</summary>
    ModelSelected,
    /// <summary>An AI request is running.</summary>
    RequestRunning,
    /// <summary>The AI returned an explicit, valid no-suggestion result.</summary>
    NoSuggestion,
    /// <summary>An AI request was cancelled.</summary>
    RequestCancelled,
    /// <summary>The provider returned data that did not pass application validation.</summary>
    ResponseInvalid,
}

/// <summary>
/// Identifies the bounded suggestion workflow requested from an optional provider.
/// </summary>
public enum AiSuggestionKind
{
    /// <summary>Suggests a safe replacement filename for one known file.</summary>
    FileRename,
    /// <summary>Suggests a preview-only logical hierarchy for bounded known metadata.</summary>
    FolderStructure,
}

/// <summary>
/// Identifies a safe, user-visible provider failure category.
/// </summary>
public enum AiProviderFailureKind
{
    /// <summary>No failure occurred.</summary>
    None,
    /// <summary>The request configuration is incomplete or invalid.</summary>
    Configuration,
    /// <summary>The endpoint could not be reached.</summary>
    Unavailable,
    /// <summary>The configured model is unavailable.</summary>
    ModelUnavailable,
    /// <summary>The model or provider rejected the required structured behavior.</summary>
    UnsupportedResponse,
    /// <summary>The request exceeded its configured time limit.</summary>
    Timeout,
    /// <summary>The user cancelled the request.</summary>
    Cancelled,
    /// <summary>The provider returned an unsupported, empty, malformed, or oversized response.</summary>
    InvalidResponse,
    /// <summary>The provider returned another unsuccessful HTTP result.</summary>
    HttpFailure,
}

/// <summary>
/// Identifies the distinct user decisions retained for compatible local preference history.
/// </summary>
public enum AiSuggestionDecisionKind
{
    /// <summary>A filename suggestion was reviewed.</summary>
    Rename,
    /// <summary>A historical tag suggestion was reviewed.</summary>
    Tags,
    /// <summary>A historical category suggestion was reviewed.</summary>
    Category,
    /// <summary>A historical destination-folder suggestion was reviewed.</summary>
    DestinationFolder,
    /// <summary>A preview-only folder-structure plan was reviewed.</summary>
    FolderStructure,
}

/// <summary>
/// Identifies the result of a user review decision.
/// </summary>
public enum AiSuggestionDecisionOutcome
{
    /// <summary>The supplied value was accepted without an edit.</summary>
    Accepted,
    /// <summary>The supplied value was rejected.</summary>
    Rejected,
    /// <summary>The supplied value was accepted with an explicit edit.</summary>
    Edited,
}

/// <summary>Represents a model discovered from an optional AI provider.</summary>
public sealed record AiModel(string Id, string DisplayName);

/// <summary>Represents a user-safe provider connection result.</summary>
public sealed record AiConnectionResult(
    AiAvailabilityState State,
    string Message,
    IReadOnlyList<AiModel> Models);

/// <summary>Describes safe metadata for one explicit file-rename request.</summary>
public sealed record AiFileRenameRequest(
    ResultFile File,
    IReadOnlyList<string> SiblingFileNames);

/// <summary>Describes a bounded in-memory collection supplied to a folder-structure workflow.</summary>
public sealed record AiFolderStructureRequest(
    IReadOnlyList<ResultFile> Files,
    IReadOnlyList<string> ExistingFolderNames);

/// <summary>Represents an application-normalized tag retained for existing deterministic and historical workflows.</summary>
public sealed record SuggestedTag(string DisplayName, string NormalizedValue);

/// <summary>Represents one validated, review-only filename proposal.</summary>
public sealed record AiFileRenameSuggestion(
    string SuggestionId,
    string SourceFileId,
    string SuggestedFileName,
    string Reason,
    double? Confidence,
    string Provider,
    string Model,
    DateTimeOffset GeneratedAtUtc);

/// <summary>Represents one validated logical folder in a preview-only hierarchy.</summary>
public sealed record AiSuggestedFolder(
    string FolderId,
    string Name,
    string? ParentFolderId,
    string LogicalPath,
    string Reason,
    double? Confidence);

/// <summary>Represents one validated known-file assignment in a preview-only hierarchy.</summary>
public sealed record AiFolderStructurePlanItem(string FileId, string FileName, string DestinationFolder);

/// <summary>Represents a validated, preview-only proposed folder structure.</summary>
public sealed record AiFolderStructurePlan(
    string PlanId,
    IReadOnlyList<AiSuggestedFolder> Folders,
    IReadOnlyList<AiFolderStructurePlanItem> Items,
    string Reason,
    string Provider,
    string Model,
    DateTimeOffset GeneratedAtUtc);

/// <summary>Contains a safe result of generating one filename suggestion.</summary>
public sealed record AiFileRenameResult(
    AiAvailabilityState State,
    string Message,
    AiFileRenameSuggestion? Suggestion,
    bool WasInputBounded = false);

/// <summary>Contains a safe result of generating one preview-only folder-structure plan.</summary>
public sealed record AiFolderStructureResult(
    AiAvailabilityState State,
    string Message,
    AiFolderStructurePlan? Plan,
    bool WasInputBounded = false);

/// <summary>Contains a safe result for a local AI review-history operation.</summary>
public sealed record AiDecisionResult(AiAvailabilityState State, string Message);

/// <summary>Records one local, inspectable user decision without storing a source path or document content.</summary>
public sealed record AiSuggestionDecision(
    AiSuggestionDecisionKind Kind,
    AiSuggestionDecisionOutcome Outcome,
    string? Extension,
    string SuggestedValue,
    string? FinalValue,
    string Provider,
    string Model,
    DateTimeOffset RecordedAtUtc);

/// <summary>Provides compact, deterministic preference signals derived from local decision history.</summary>
public sealed record AiPreferenceSummary(
    IReadOnlyList<string> PreferredTags,
    IReadOnlyList<string> PreferredFolders,
    IReadOnlyList<string> PreferredCategories,
    IReadOnlyList<string> RejectedValues);

/// <summary>Contains a provider-neutral generation request. Prompt text is never logged in full.</summary>
public sealed record AiProviderGenerationRequest(
    AiSuggestionKind Kind,
    string Endpoint,
    string Model,
    string Prompt,
    TimeSpan Timeout);

/// <summary>Contains a provider-neutral generation response before application-owned validation.</summary>
public sealed record AiProviderGenerationResult(
    string? StructuredJson,
    AiProviderFailureKind FailureKind,
    string Message)
{
    /// <summary>Gets whether a structured response is available for application validation.</summary>
    public bool IsSuccess => FailureKind == AiProviderFailureKind.None && !string.IsNullOrWhiteSpace(StructuredJson);
}

/// <summary>Defines the narrow provider boundary used by application-owned suggestion workflows.</summary>
public interface IAiSuggestionProvider
{
    /// <summary>Checks provider availability and lists installed models.</summary>
    Task<AiConnectionResult> GetConnectionAsync(AiSettings settings, CancellationToken cancellationToken);

    /// <summary>Requests one structured provider response without parsing it into application models.</summary>
    Task<AiProviderGenerationResult> GenerateAsync(AiProviderGenerationRequest request, CancellationToken cancellationToken);
}

/// <summary>Persists local, user-review decisions independently from scan results.</summary>
public interface IDecisionHistoryStore
{
    /// <summary>Loads valid persisted decisions in chronological order.</summary>
    Task<IReadOnlyList<AiSuggestionDecision>> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Appends one validated local decision.</summary>
    Task AppendAsync(AiSuggestionDecision decision, CancellationToken cancellationToken);

    /// <summary>Removes all persisted local decision history.</summary>
    Task ClearAsync(CancellationToken cancellationToken);
}

/// <summary>Provides application-owned, validated suggestion workflows and local preference adaptation.</summary>
public interface IAiSuggestionService
{
    /// <summary>Tests the configured optional provider without generating a suggestion.</summary>
    Task<AiConnectionResult> TestConnectionAsync(ApplicationSettings settings, CancellationToken cancellationToken);

    /// <summary>Discovers models from the configured optional provider.</summary>
    Task<AiConnectionResult> DiscoverModelsAsync(ApplicationSettings settings, CancellationToken cancellationToken);

    /// <summary>Generates one safe, review-only filename suggestion.</summary>
    Task<AiFileRenameResult> GenerateFileRenameAsync(AiFileRenameRequest request, AiSettings settings, CancellationToken cancellationToken);

    /// <summary>Generates one safe, preview-only folder-structure plan.</summary>
    Task<AiFolderStructureResult> GenerateFolderStructureAsync(AiFolderStructureRequest request, AiSettings settings, CancellationToken cancellationToken);

    /// <summary>Records a user review decision for optional local preference adaptation.</summary>
    Task<AiDecisionResult> RecordDecisionAsync(AiSuggestionDecision decision, AiSettings settings, CancellationToken cancellationToken);

    /// <summary>Removes all local preference signals and recorded decisions.</summary>
    Task<AiDecisionResult> ResetDecisionHistoryAsync(ApplicationSettings settings, CancellationToken cancellationToken);
}
