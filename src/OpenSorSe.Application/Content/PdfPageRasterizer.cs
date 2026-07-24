using PDFtoImage;

namespace OpenSorSe.Application.Content;

/// <summary>Renders individual PDF pages through the bundled permissively licensed PDFium wrapper.</summary>
public sealed class PdfPageRasterizer : IPdfPageRasterizer
{
    private const string RasterizerIdentifier = "pdftoimage-pdfium";
    private const string WorkspacePrefix = "job-";
    private readonly string _temporaryRoot;

    /// <summary>Initializes the renderer and removes stale application-owned OCR workspaces.</summary>
    public PdfPageRasterizer()
        : this(Path.Combine(Path.GetTempPath(), "OpenSorSe", "ocr"))
    {
    }

    internal PdfPageRasterizer(string temporaryRoot)
    {
        _temporaryRoot = Path.GetFullPath(temporaryRoot);
        CleanupStaleWorkspaces();
    }

    /// <inheritdoc />
    public Task<PdfRasterizerCapability> DetectCapabilityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsSupportedDesktopPlatform())
        {
            return Task.FromResult(new PdfRasterizerCapability(
                false,
                RasterizerIdentifier,
                null,
                "PDF page rendering is unavailable on this platform."));
        }

        var version = typeof(Conversion).Assembly.GetName().Version?.ToString();
        return Task.FromResult(new PdfRasterizerCapability(
            true,
            RasterizerIdentifier,
            ContentText.NormalizeField(version, 64),
            "Bundled local PDF page rendering is available."));
    }

    /// <inheritdoc />
    public async Task<int> GetPageCountAsync(string fullPath, CancellationToken cancellationToken)
    {
        ValidatePdfPath(fullPath);
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.Run(
            () =>
            {
                if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                {
                    throw new PlatformNotSupportedException("PDF page rendering is unavailable on this platform.");
                }

                using var stream = OpenPdf(fullPath);
#pragma warning disable CA1416 // Guarded above; the package declares the same desktop platform set.
                return Conversion.GetPageCount(stream, leaveOpen: false, password: null);
#pragma warning restore CA1416
            },
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RenderedPdfPage> RenderAsync(
        string fullPath,
        int pageNumber,
        int dpi,
        int maximumDimension,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        ValidatePdfPath(fullPath);
        if (pageNumber < 1 || dpi is < 72 or > 600 || maximumDimension is < 256 or > 16_384)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber), "PDF rendering bounds are invalid.");
        }

        var normalizedWorkspace = ValidateOwnedWorkspace(workspacePath);
        Directory.CreateDirectory(normalizedWorkspace);
        var outputPath = Path.Combine(
            normalizedWorkspace,
            $"page-{pageNumber.ToString("D4", System.Globalization.CultureInfo.InvariantCulture)}.png");
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Run(
            () =>
            {
                if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
                {
                    throw new PlatformNotSupportedException("PDF page rendering is unavailable on this platform.");
                }

                using var stream = OpenPdf(fullPath);
                var pageIndex = new Index(pageNumber - 1);
#pragma warning disable CA1416 // Guarded above; the package declares the same desktop platform set.
                var pageSize = Conversion.GetPageSize(stream, pageIndex, leaveOpen: true, password: null);
                var targetWidth = Math.Max(1, (int)Math.Ceiling(pageSize.Width * dpi / 72d));
                var targetHeight = Math.Max(1, (int)Math.Ceiling(pageSize.Height * dpi / 72d));
                var largest = Math.Max(targetWidth, targetHeight);
                if (largest > maximumDimension)
                {
                    var scale = maximumDimension / (double)largest;
                    targetWidth = Math.Max(1, (int)Math.Floor(targetWidth * scale));
                    targetHeight = Math.Max(1, (int)Math.Floor(targetHeight * scale));
                }

                var options = new RenderOptions
                {
                    Dpi = 72,
                    Width = targetWidth,
                    Height = targetHeight,
                    WithAspectRatio = false,
                    WithAnnotations = false,
                    WithFormFill = false,
                    Grayscale = true,
                };
                Conversion.SavePng(
                    outputPath,
                    stream,
                    pageIndex,
                    leaveOpen: false,
                    password: null,
                    options);
#pragma warning restore CA1416
            },
            cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        var output = new FileInfo(outputPath);
        if (!output.Exists || output.Length == 0)
        {
            throw new InvalidDataException("The local PDF renderer returned no page image.");
        }

        return new RenderedPdfPage(pageNumber, output.FullName, output.Length);
    }

    /// <inheritdoc />
    public string CreateWorkspace()
    {
        Directory.CreateDirectory(_temporaryRoot);
        var path = Path.Combine(_temporaryRoot, $"{WorkspacePrefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <inheritdoc />
    public void DeleteWorkspace(string workspacePath)
    {
        var normalized = ValidateOwnedWorkspace(workspacePath);
        if (Directory.Exists(normalized))
        {
            Directory.Delete(normalized, recursive: true);
        }
    }

    private static FileStream OpenPdf(string fullPath) => new(
        fullPath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite | FileShare.Delete,
        81920,
        FileOptions.SequentialScan);

    private static bool IsSupportedDesktopPlatform() =>
        OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

    private static void ValidatePdfPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) ||
            !Path.IsPathRooted(fullPath) ||
            !string.Equals(Path.GetExtension(fullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("An absolute PDF path is required.", nameof(fullPath));
        }
    }

    private string ValidateOwnedWorkspace(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Path.IsPathRooted(workspacePath))
        {
            throw new ArgumentException("An absolute OCR workspace is required.", nameof(workspacePath));
        }

        var normalized = Path.GetFullPath(workspacePath);
        var parent = Path.GetDirectoryName(normalized);
        var name = Path.GetFileName(normalized);
        if (!string.Equals(parent, _temporaryRoot, StringComparison.OrdinalIgnoreCase) ||
            !name.StartsWith(WorkspacePrefix, StringComparison.Ordinal) ||
            name.Length != WorkspacePrefix.Length + 32 ||
            !Guid.TryParseExact(name[WorkspacePrefix.Length..], "N", out _))
        {
            throw new InvalidOperationException("Only an isolated OpenSorSe OCR workspace may be deleted or written.");
        }

        return normalized;
    }

    private void CleanupStaleWorkspaces()
    {
        if (!Directory.Exists(_temporaryRoot))
        {
            return;
        }

        string[] directories;
        try
        {
            directories = Directory.GetDirectories(
                _temporaryRoot,
                $"{WorkspacePrefix}*",
                SearchOption.TopDirectoryOnly);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        foreach (var directory in directories)
        {
            try
            {
                var normalized = ValidateOwnedWorkspace(directory);
                if (Directory.GetLastWriteTimeUtc(normalized) < DateTime.UtcNow.AddDays(-1))
                {
                    Directory.Delete(normalized, recursive: true);
                }
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
            {
                // Stale cleanup is best effort and never broadens beyond validated owned directories.
            }
        }
    }
}
