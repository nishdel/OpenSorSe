using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Application.Content;

/// <summary>Identifies where a local metadata value or tag originated.</summary>
public enum ContentProvenance
{
    /// <summary>Read from ordinary filesystem properties.</summary>
    Filesystem,
    /// <summary>Read from embedded document or media metadata.</summary>
    EmbeddedMetadata,
    /// <summary>Extracted deterministically from native document text.</summary>
    NativeText,
    /// <summary>Extracted by a local OCR engine.</summary>
    Ocr,
    /// <summary>Derived by a deterministic application rule.</summary>
    DeterministicRule,
    /// <summary>Explicitly entered or accepted by the user.</summary>
    UserAccepted,
    /// <summary>Produced by an optional, unverified AI suggestion.</summary>
    AiSuggestion,
    /// <summary>Derived from local similarity indexing.</summary>
    SemanticInference,
}

/// <summary>Identifies the terminal or active state of one OCR attempt.</summary>
public enum OcrStatus
{
    /// <summary>No attempt has been queued.</summary>
    Pending,
    /// <summary>A local engine is processing the input.</summary>
    Processing,
    /// <summary>Useful bounded text was extracted.</summary>
    Completed,
    /// <summary>OCR was deliberately not needed or not enabled.</summary>
    Skipped,
    /// <summary>The attempt failed without stopping the scan.</summary>
    Failed,
    /// <summary>Some text was extracted but a bound or engine warning applied.</summary>
    PartiallyCompleted,
    /// <summary>No compatible local engine or rasterizer is available.</summary>
    Unavailable,
    /// <summary>Text was extracted but excluded from indexing by a configured bound.</summary>
    TextNotIndexedDueToBounds,
}

/// <summary>Classifies a controlled OCR failure without exposing raw process details.</summary>
public enum OcrFailureCategory
{
    /// <summary>No failure occurred.</summary>
    None,
    /// <summary>OCR is disabled by settings.</summary>
    Disabled,
    /// <summary>A compatible local engine is unavailable.</summary>
    EngineUnavailable,
    /// <summary>The input type is unsupported by the available capability.</summary>
    UnsupportedInput,
    /// <summary>The input exceeds the configured byte limit.</summary>
    FileTooLarge,
    /// <summary>The document exceeds the configured page bound.</summary>
    PageLimitExceeded,
    /// <summary>The bounded operation timed out.</summary>
    Timeout,
    /// <summary>The caller cancelled the operation.</summary>
    Cancelled,
    /// <summary>The local engine reported a controlled failure.</summary>
    EngineFailure,
    /// <summary>The local engine returned no useful text.</summary>
    EmptyText,
    /// <summary>The input was malformed or unreadable.</summary>
    MalformedInput,
}

/// <summary>Describes the locally detected OCR capability.</summary>
public sealed record OcrCapability(
    bool IsAvailable,
    string EngineIdentifier,
    string? EngineVersion,
    IReadOnlyList<string> SupportedExtensions,
    bool SupportsPdf,
    string Message);

/// <summary>Contains the bounded context for one local OCR request.</summary>
public sealed record OcrRequest(
    string FullPath,
    string Language,
    long MaximumFileBytes,
    int MaximumPages,
    TimeSpan Timeout,
    bool HasReliableNativeText);

/// <summary>Contains one controlled OCR outcome.</summary>
public sealed record OcrResult(
    OcrStatus Status,
    string? ExtractedText,
    string? Language,
    double? Confidence,
    int? PageCount,
    IReadOnlyList<string> Warnings,
    OcrFailureCategory FailureCategory,
    TimeSpan ProcessingDuration,
    string EngineIdentifier,
    string? EngineVersion,
    string Message)
{
    /// <summary>Gets whether bounded OCR text is available for local indexing.</summary>
    public bool HasText => !string.IsNullOrWhiteSpace(ExtractedText);
}

/// <summary>Represents one normalized metadata value and its provenance.</summary>
public sealed record ExtractedMetadataField(
    string Name,
    string Value,
    ContentProvenance Provenance,
    double Confidence = 1);

/// <summary>Contains defensive metadata and native-text extraction for one known file.</summary>
public sealed record MetadataExtractionResult(
    IReadOnlyList<ExtractedMetadataField> Fields,
    string? NativeText,
    bool HasReliableNativeText,
    int? PageCount,
    IReadOnlyList<string> Warnings);

/// <summary>Contains one bounded, reusable local content record.</summary>
public sealed record ContentRecord(
    string FullPath,
    long SourceLength,
    DateTimeOffset SourceLastWriteTimeUtc,
    DateTimeOffset IndexedAtUtc,
    IReadOnlyList<ExtractedMetadataField> Metadata,
    string? NativeText,
    string? OcrText,
    OcrStatus OcrStatus,
    string? OcrEngineIdentifier,
    IReadOnlyList<string> Warnings)
{
    /// <summary>Gets a stable source fingerprint used for cache invalidation.</summary>
    public string SourceFingerprint => $"{SourceLength}:{SourceLastWriteTimeUtc.UtcTicks}";
}

/// <summary>Summarizes one isolated content-indexing pass.</summary>
public sealed record ContentIndexingSummary(
    int ExaminedCount,
    int IndexedCount,
    int CacheHitCount,
    int FailedCount,
    int OcrCompletedCount,
    int OcrSkippedCount);

/// <summary>Abstracts one concrete local OCR engine.</summary>
public interface IOcrEngine
{
    /// <summary>Detects capability without opening user content.</summary>
    Task<OcrCapability> DetectCapabilityAsync(CancellationToken cancellationToken);

    /// <summary>Extracts bounded text from one supported known file.</summary>
    Task<OcrResult> RecognizeAsync(OcrRequest request, CancellationToken cancellationToken);
}

/// <summary>Applies settings, cache, bounds, and normalization around a local OCR engine.</summary>
public interface IOcrService
{
    /// <summary>Gets the current local capability without sending network requests.</summary>
    Task<OcrCapability> GetCapabilityAsync(CancellationToken cancellationToken);

    /// <summary>Runs or skips one bounded OCR request.</summary>
    Task<OcrResult> RecognizeAsync(OcrRequest request, CancellationToken cancellationToken);
}

/// <summary>Extracts bounded metadata from a supported file type.</summary>
public interface IMetadataExtractor
{
    /// <summary>Gets whether this extractor supports the supplied normalized extension.</summary>
    bool Supports(string normalizedExtension);

    /// <summary>Extracts metadata without executing embedded content or fetching remote resources.</summary>
    Task<MetadataExtractionResult> ExtractAsync(
        FileEntry file,
        long maximumInputBytes,
        int maximumPages,
        CancellationToken cancellationToken);
}

/// <summary>Coordinates defensive extractors and normalized provenance.</summary>
public interface IMetadataExtractionPipeline
{
    /// <summary>Extracts bounded metadata and native text for one known file.</summary>
    Task<MetadataExtractionResult> ExtractAsync(
        FileEntry file,
        long maximumInputBytes,
        int maximumPages,
        CancellationToken cancellationToken);
}

/// <summary>Persists bounded local content independently from catalog snapshots.</summary>
public interface IContentStore
{
    /// <summary>Loads one exact-path record when present.</summary>
    Task<ContentRecord?> GetAsync(string fullPath, CancellationToken cancellationToken);

    /// <summary>Lists bounded records in deterministic path order.</summary>
    Task<IReadOnlyList<ContentRecord>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Adds or replaces one normalized record.</summary>
    Task UpsertAsync(ContentRecord record, CancellationToken cancellationToken);

    /// <summary>Removes stale records that are not in the supplied known-path set.</summary>
    Task RemoveMissingAsync(IReadOnlyCollection<string> knownPaths, CancellationToken cancellationToken);

    /// <summary>Clears the application-owned cache without changing source files.</summary>
    Task ClearAsync(CancellationToken cancellationToken);
}

/// <summary>Integrates metadata and OCR with the read-only scan pipeline.</summary>
public interface IContentIndexingService
{
    /// <summary>Indexes known scanned files with per-file failure isolation.</summary>
    Task<ContentIndexingSummary> IndexAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken);
}
