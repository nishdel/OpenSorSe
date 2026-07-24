using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Configuration;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

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
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var source = ReadSourceIdentity(file);
                var existing = await _contentStore.GetAsync(file.FullPath, cancellationToken).ConfigureAwait(false);
                if (existing is not null &&
                    existing.SourceLength == source.Length &&
                    existing.SourceLastWriteTimeUtc == source.LastWriteTimeUtc)
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
                        metadata.HasReliableNativeText),
                    cancellationToken).ConfigureAwait(false);
                if (ocr.Status is OcrStatus.Completed or OcrStatus.PartiallyCompleted)
                {
                    ocrCompleted++;
                }
                else if (ocr.Status == OcrStatus.Skipped)
                {
                    ocrSkipped++;
                }

                var record = new ContentRecord(
                    Path.GetFullPath(file.FullPath),
                    source.Length,
                    source.LastWriteTimeUtc,
                    DateTimeOffset.UtcNow,
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
                        .ToArray()));
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
}
