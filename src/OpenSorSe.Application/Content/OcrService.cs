using OpenSorSe.Core.Configuration;

namespace OpenSorSe.Application.Content;

/// <summary>Enforces local OCR settings and input bounds before invoking a concrete engine.</summary>
public sealed class OcrService : IOcrService
{
    private readonly IConfigurationService _configurationService;
    private readonly IOcrEngine _engine;

    /// <summary>Initializes the bounded local OCR service.</summary>
    public OcrService(IConfigurationService configurationService, IOcrEngine engine)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc />
    public Task<OcrCapability> GetCapabilityAsync(CancellationToken cancellationToken) =>
        _engine.DetectCapabilityAsync(cancellationToken);

    /// <inheritdoc />
    public Task<OcrCapability> RefreshCapabilityAsync(CancellationToken cancellationToken)
    {
        _engine.ResetCapability();
        return _engine.DetectCapabilityAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var settings = _configurationService.Current.Content;
        if (!settings.OcrEnabled)
        {
            return Skipped(request, OcrFailureCategory.Disabled, "OCR is disabled in Settings.");
        }

        if (settings.OcrOnlyWhenNativeTextUnavailable && request.HasReliableNativeText)
        {
            return Skipped(
                request,
                OcrFailureCategory.None,
                "OCR was skipped because reliable native text is available.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        FileInfo info;
        try
        {
            info = new FileInfo(request.FullPath);
            if (!info.Exists)
            {
                return Failed(OcrFailureCategory.MalformedInput, "OCR input is no longer available.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Failed(OcrFailureCategory.MalformedInput, "OCR input could not be read.");
        }

        var maximumBytes = Math.Min(
            request.MaximumFileBytes,
            settings.MaximumFileSizeMiB * 1024L * 1024L);
        if (info.Length > maximumBytes)
        {
            return Failed(OcrFailureCategory.FileTooLarge, "OCR was skipped because the file exceeds the configured size bound.");
        }

        var boundedRequest = request with
        {
            Language = settings.OcrLanguage,
            MaximumFileBytes = maximumBytes,
            MaximumPages = Math.Min(request.MaximumPages, settings.MaximumPagesPerDocument),
            RasterizationDpi = settings.PdfRasterizationDpi,
            MaximumRasterDimension = settings.MaximumRasterDimension,
            MaximumTextCharacters = Math.Min(
                request.MaximumTextCharacters,
                settings.MaximumOcrTextCharacters),
            MaximumTemporaryStorageBytes = Math.Min(
                request.MaximumTemporaryStorageBytes,
                settings.MaximumTemporaryStorageMiB * 1024L * 1024L),
            Timeout = TimeSpan.FromSeconds(Math.Min(
                request.Timeout.TotalSeconds,
                settings.MaximumOcrDurationSeconds)),
        };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(boundedRequest.Timeout);
        try
        {
            return await _engine.RecognizeAsync(boundedRequest, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Failed(OcrFailureCategory.Timeout, "Local OCR timed out.");
        }
        catch (Exception)
        {
            return Failed(OcrFailureCategory.EngineFailure, "Local OCR failed safely.");
        }
    }

    private static OcrResult Skipped(
        OcrRequest request,
        OcrFailureCategory category,
        string message)
    {
        var pages = request.PdfPages
            .OrderBy(page => page.PageNumber)
            .Select(page => page.HasReliableNativeText
                ? new OcrPageResult(
                    page.PageNumber,
                    OcrPageTextSource.NativeText,
                    OcrStatus.Skipped,
                    page.NativeText,
                    null,
                    "Reliable PDF-native text was retained.")
                : new OcrPageResult(
                    page.PageNumber,
                    OcrPageTextSource.Skipped,
                    OcrStatus.Skipped,
                    null,
                    null,
                    message))
            .ToArray();
        return new OcrResult(
            OcrStatus.Skipped,
            null,
            null,
            null,
            pages.Length == 0 ? null : pages.Length,
            [],
            category,
            TimeSpan.Zero,
            "none",
            null,
            message)
        {
            Pages = Array.AsReadOnly(pages),
        };
    }

    private static OcrResult Failed(OcrFailureCategory category, string message) => new(
        OcrStatus.Failed,
        null,
        null,
        null,
        null,
        [],
        category,
        TimeSpan.Zero,
        "none",
        null,
        message);
}
