using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;
using OpenSorSe.Application.Tags;
using System.Security.Cryptography;
using System.Text;

namespace OpenSorSe.Application.Content;

/// <summary>Indexes bounded metadata and OCR text with cache reuse and per-file failure isolation.</summary>
public sealed class ContentIndexingService : IContentIndexingService
{
    private readonly IConfigurationService _configurationService;
    private readonly IContentStore _contentStore;
    private readonly ILogger _logger;
    private readonly IMetadataExtractionPipeline _metadataPipeline;
    private readonly IOcrService _ocrService;

    /// <summary>Initializes the local scan-content indexing stage.</summary>
    public ContentIndexingService(
        IConfigurationService configurationService,
        IMetadataExtractionPipeline metadataPipeline,
        IOcrService ocrService,
        IContentStore contentStore,
        ILoggingService loggingService)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _metadataPipeline = metadataPipeline ?? throw new ArgumentNullException(nameof(metadataPipeline));
        _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        _contentStore = contentStore ?? throw new ArgumentNullException(nameof(contentStore));
        _logger = (loggingService ?? throw new ArgumentNullException(nameof(loggingService)))
            .CreateLogger(nameof(ContentIndexingService));
    }

    /// <inheritdoc />
    public async Task<ContentIndexingSummary> IndexAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(files);
        var settings = _configurationService.Current.Content;
        if (!settings.MetadataExtractionEnabled && !settings.OcrEnabled)
        {
            return new ContentIndexingSummary(files.Count, 0, 0, 0, 0, files.Count);
        }

        var indexed = 0;
        var cacheHits = 0;
        var failed = 0;
        var ocrCompleted = 0;
        var ocrSkipped = 0;
        var maximumBytes = settings.MaximumFileSizeMiB * 1024L * 1024L;
        var capability = settings.OcrEnabled && files.Count > 0
            ? await _ocrService.GetCapabilityAsync(cancellationToken).ConfigureAwait(false)
            : null;
        var extractionFingerprint = ContentCacheFingerprint.Create(settings, capability);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var source = ReadSourceIdentity(file);
                var existing = await _contentStore.GetAsync(file.FullPath, cancellationToken).ConfigureAwait(false);
                if (existing is not null &&
                    existing.SourceLength == source.Length &&
                    existing.SourceLastWriteTimeUtc == source.LastWriteTimeUtc &&
                    string.Equals(
                        existing.ExtractionFingerprint,
                        extractionFingerprint,
                        StringComparison.Ordinal))
                {
                    cacheHits++;
                    continue;
                }

                var metadata = settings.MetadataExtractionEnabled
                    ? await _metadataPipeline.ExtractAsync(
                        file,
                        maximumBytes,
                        settings.MaximumPagesPerDocument,
                        cancellationToken).ConfigureAwait(false)
                    : new MetadataExtractionResult([], null, false, null, []);
                var ocr = await _ocrService.RecognizeAsync(
                    new OcrRequest(
                        file.FullPath,
                        settings.OcrLanguage,
                        maximumBytes,
                        settings.MaximumPagesPerDocument,
                        TimeSpan.FromSeconds(settings.MaximumOcrDurationSeconds),
                        metadata.HasReliableNativeText)
                    {
                        PdfPages = metadata.PdfPages,
                        RasterizationDpi = settings.PdfRasterizationDpi,
                        MaximumRasterDimension = settings.MaximumRasterDimension,
                        MaximumTextCharacters = settings.MaximumOcrTextCharacters,
                        MaximumTemporaryStorageBytes = settings.MaximumTemporaryStorageMiB * 1024L * 1024L,
                    },
                    cancellationToken).ConfigureAwait(false);
                if (ocr.Status is OcrStatus.Completed or OcrStatus.PartiallyCompleted)
                {
                    ocrCompleted++;
                }
                else if (ocr.Status == OcrStatus.Skipped)
                {
                    ocrSkipped++;
                }

                var indexedAt = DateTimeOffset.UtcNow;
                var generatedTags = ProvenanceTagGenerator.Generate(
                    Path.GetFullPath(file.FullPath),
                    $"{source.Length}:{source.LastWriteTimeUtc.UtcTicks}",
                    metadata.Fields,
                    metadata.NativeText,
                    ocr.ExtractedText,
                    indexedAt);
                var record = new ContentRecord(
                    Path.GetFullPath(file.FullPath),
                    source.Length,
                    source.LastWriteTimeUtc,
                    indexedAt,
                    metadata.Fields,
                    metadata.NativeText,
                    ocr.ExtractedText,
                    ocr.Status,
                    ocr.EngineIdentifier,
                    Array.AsReadOnly(metadata.Warnings
                        .Concat(ocr.Warnings)
                        .Append(ocr.Message)
                        .Distinct(StringComparer.Ordinal)
                        .Take(16)
                        .ToArray()))
                {
                    ExtractionFingerprint = extractionFingerprint,
                    OcrPages = ocr.Pages,
                    Tags = MergeTags(
                        generatedTags,
                        existing?.Tags ?? [],
                        $"{source.Length}:{source.LastWriteTimeUtc.UtcTicks}"),
                };
                await _contentStore.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
                indexed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                _logger.LogWarning(exception, "Local content extraction failed for one scanned file and the scan will continue.");
            }
        }

        return new ContentIndexingSummary(
            files.Count,
            indexed,
            cacheHits,
            failed,
            ocrCompleted,
            ocrSkipped);
    }

    private static (long Length, DateTimeOffset LastWriteTimeUtc) ReadSourceIdentity(FileEntry file)
    {
        if (file.Metadata?.SizeInBytes is { } length &&
            file.Metadata.LastWriteTimeUtc is { } modified)
        {
            return (length, modified.ToUniversalTime());
        }

        var info = new FileInfo(file.FullPath);
        return (info.Length, new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero));
    }

    private static IReadOnlyList<OpenSorSe.Application.Models.TagAssociation> MergeTags(
        IReadOnlyList<OpenSorSe.Application.Models.TagAssociation> generated,
        IReadOnlyList<OpenSorSe.Application.Models.TagAssociation> existing,
        string sourceFingerprint)
    {
        var retained = existing.Where(tag =>
            tag.Source == OpenSorSe.Application.Models.TagSource.UserApproved ||
            tag.AcceptanceState == OpenSorSe.Application.Models.TagAcceptanceState.Accepted && !tag.IsSystem ||
            tag.AcceptanceState == OpenSorSe.Application.Models.TagAcceptanceState.Rejected &&
            string.Equals(tag.SourceFingerprint, sourceFingerprint, StringComparison.Ordinal));
        return Array.AsReadOnly(generated
            .Concat(retained)
            .GroupBy(tag => tag.NormalizedValue, StringComparer.Ordinal)
            .Select(group => group
                .OrderByDescending(tag => tag.Source == OpenSorSe.Application.Models.TagSource.UserApproved)
                .ThenByDescending(tag => tag.AcceptanceState == OpenSorSe.Application.Models.TagAcceptanceState.Accepted)
                .ThenByDescending(tag => tag.AcceptanceState == OpenSorSe.Application.Models.TagAcceptanceState.Rejected)
                .ThenByDescending(tag => tag.Confidence)
                .First())
            .OrderByDescending(tag => tag.AcceptanceState == OpenSorSe.Application.Models.TagAcceptanceState.Accepted)
            .ThenBy(tag => tag.NormalizedValue, StringComparer.Ordinal)
            .Take(32)
            .ToArray());
    }
}

/// <summary>Builds the deterministic extraction-settings identity stored with local content.</summary>
public static class ContentCacheFingerprint
{
    private const int SchemaVersion = 2;

    /// <summary>Creates a stable non-secret fingerprint for settings and detected local OCR components.</summary>
    public static string Create(ContentSettings settings, OcrCapability? capability)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var value = string.Join(
            "|",
            SchemaVersion,
            settings.MetadataExtractionEnabled,
            settings.OcrEnabled,
            settings.OcrOnlyWhenNativeTextUnavailable,
            settings.MaximumPagesPerDocument,
            settings.MaximumFileSizeMiB,
            settings.OcrLanguage,
            settings.MaximumOcrDurationSeconds,
            settings.PdfRasterizationDpi,
            settings.MaximumRasterDimension,
            settings.MaximumOcrTextCharacters,
            settings.MaximumTemporaryStorageMiB,
            capability?.EngineIdentifier ?? "none",
            capability?.EngineVersion ?? "none",
            capability?.RasterizerIdentifier ?? "none",
            capability?.RasterizerVersion ?? "none");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
