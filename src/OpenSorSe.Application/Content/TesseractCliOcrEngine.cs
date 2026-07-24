using System.Diagnostics;

namespace OpenSorSe.Application.Content;

/// <summary>Integrates an externally managed local Tesseract CLI for supported image inputs.</summary>
public sealed class TesseractCliOcrEngine : IOcrEngine
{
    private static readonly IReadOnlyList<string> Extensions =
        Array.AsReadOnly([".png", ".jpg", ".jpeg", ".tif", ".tiff"]);
    private readonly SemaphoreSlim _capabilityMutex = new(1, 1);
    private OcrCapability? _cachedCapability;

    /// <inheritdoc />
    public async Task<OcrCapability> DetectCapabilityAsync(CancellationToken cancellationToken)
    {
        if (_cachedCapability is not null)
        {
            return _cachedCapability;
        }

        await _capabilityMutex.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cachedCapability is not null)
            {
                return _cachedCapability;
            }

            try
            {
                using var process = CreateProcess("--version");
                if (!process.Start())
                {
                    return _cachedCapability = Unavailable();
                }

                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                var output = await ReadBoundedAsync(
                    process.StandardOutput,
                    512,
                    timeout.Token).ConfigureAwait(false);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    return _cachedCapability = Unavailable();
                }

                var version = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                return _cachedCapability = new OcrCapability(
                    true,
                    "tesseract-cli",
                    ContentText.NormalizeField(version, 128),
                    Extensions,
                    false,
                    "Local Tesseract image OCR is available. Scanned-PDF OCR requires a separate local rasterizer.");
            }
            catch (Exception exception) when (
                exception is System.ComponentModel.Win32Exception or
                InvalidOperationException or
                OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                return _cachedCapability = Unavailable();
            }
        }
        finally
        {
            _capabilityMutex.Release();
        }
    }

    /// <inheritdoc />
    public async Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var started = Stopwatch.StartNew();
        var capability = await DetectCapabilityAsync(cancellationToken).ConfigureAwait(false);
        if (!capability.IsAvailable)
        {
            return Failure(
                OcrStatus.Unavailable,
                OcrFailureCategory.EngineUnavailable,
                capability,
                started.Elapsed,
                capability.Message);
        }

        var extension = Path.GetExtension(request.FullPath).ToLowerInvariant();
        if (!capability.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Failure(
                OcrStatus.Unavailable,
                OcrFailureCategory.UnsupportedInput,
                capability,
                started.Elapsed,
                extension == ".pdf"
                    ? "Scanned-PDF OCR is unavailable because no local PDF rasterizer is configured."
                    : "The available local OCR engine does not support this file type.");
        }

        using var process = CreateProcess();
        process.StartInfo.ArgumentList.Add(request.FullPath);
        process.StartInfo.ArgumentList.Add("stdout");
        process.StartInfo.ArgumentList.Add("-l");
        process.StartInfo.ArgumentList.Add(request.Language);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(request.Timeout);
        try
        {
            if (!process.Start())
            {
                return Failure(
                    OcrStatus.Failed,
                    OcrFailureCategory.EngineFailure,
                    capability,
                    started.Elapsed,
                    "The local OCR process could not be started.");
            }

            var outputTask = ReadBoundedAsync(
                process.StandardOutput,
                ContentText.MaximumTextCharacters + 1,
                timeout.Token);
            var errorTask = ReadBoundedAsync(process.StandardError, 2048, timeout.Token);
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            var output = await outputTask.ConfigureAwait(false);
            _ = await errorTask.ConfigureAwait(false);
            if (process.ExitCode != 0)
            {
                return Failure(
                    OcrStatus.Failed,
                    OcrFailureCategory.EngineFailure,
                    capability,
                    started.Elapsed,
                    "The local OCR engine could not process this file.");
            }

            var wasBounded = output.Length > ContentText.MaximumTextCharacters;
            var normalized = ContentText.Normalize(output);
            if (normalized.Length == 0)
            {
                return Failure(
                    OcrStatus.Failed,
                    OcrFailureCategory.EmptyText,
                    capability,
                    started.Elapsed,
                    "The local OCR engine returned no usable text.");
            }

            return new OcrResult(
                wasBounded ? OcrStatus.PartiallyCompleted : OcrStatus.Completed,
                normalized,
                request.Language,
                null,
                1,
                wasBounded ? ["OCR text was truncated at the local content bound."] : [],
                OcrFailureCategory.None,
                started.Elapsed,
                capability.EngineIdentifier,
                capability.EngineVersion,
                wasBounded
                    ? "OCR completed with bounded text."
                    : "OCR completed locally.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            throw;
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            return Failure(
                OcrStatus.Failed,
                OcrFailureCategory.Timeout,
                capability,
                started.Elapsed,
                "Local OCR timed out.");
        }
        catch (Exception exception) when (
            exception is IOException or
            System.ComponentModel.Win32Exception or
            InvalidOperationException)
        {
            TryKill(process);
            return Failure(
                OcrStatus.Failed,
                OcrFailureCategory.EngineFailure,
                capability,
                started.Elapsed,
                "The local OCR engine could not process this file.");
        }
    }

    private static Process CreateProcess(params string[] arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("tesseract")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        return process;
    }

    private static async Task<string> ReadBoundedAsync(
        StreamReader reader,
        int maximumCharacters,
        CancellationToken cancellationToken)
    {
        var output = new System.Text.StringBuilder(Math.Min(4096, maximumCharacters));
        var buffer = new char[4096];
        while (output.Length < maximumCharacters)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(
                0,
                Math.Min(buffer.Length, maximumCharacters - output.Length)),
                cancellationToken).ConfigureAwait(false);
            if (count == 0)
            {
                break;
            }

            output.Append(buffer, 0, count);
        }

        return output.ToString();
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }

    private static OcrCapability Unavailable() => new(
        false,
        "tesseract-cli",
        null,
        Extensions,
        false,
        "No compatible local Tesseract CLI was detected. OCR remains unavailable until it is installed and on PATH.");

    private static OcrResult Failure(
        OcrStatus status,
        OcrFailureCategory category,
        OcrCapability capability,
        TimeSpan duration,
        string message) => new(
            status,
            null,
            null,
            null,
            null,
            [],
            category,
            duration,
            capability.EngineIdentifier,
            capability.EngineVersion,
            message);
}
