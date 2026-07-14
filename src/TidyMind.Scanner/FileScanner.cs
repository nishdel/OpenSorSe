using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TidyMind.Core.Errors;
using TidyMind.Core.Logging;
using TidyMind.Scanner.Models;

namespace TidyMind.Scanner;

/// <summary>
/// Performs a read-only recursive traversal of selected filesystem directories.
/// </summary>
public sealed class FileScanner : IFileScanner
{
    private const string LoggerCategory = "Scanner";
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a file scanner that records diagnostics through shared Core infrastructure.
    /// </summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler used for unexpected operation-level failures.</param>
    public FileScanner(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public Task<ScanResult> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = NormalizeRequest(request);
        return Task.Run(
            () => Scan(normalizedRequest, progress, cancellationToken),
            CancellationToken.None);
    }

    private ScanResult Scan(
        NormalizedScanRequest request,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellationToken)
    {
        var files = new List<FileEntry>();
        var directories = new List<DirectoryEntry>();
        var issues = new List<ScanIssue>();
        var discoveredPaths = new HashSet<string>(PathComparer);
        var stopwatch = Stopwatch.StartNew();
        var lastProgressReport = TimeSpan.Zero;

        try
        {
            _logger.LogInformation("Started scanning {RootCount} root directories.", request.RootDirectories.Count);
            ReportProgress(null, progress, files.Count, directories.Count, issues.Count, stopwatch.Elapsed, ref lastProgressReport, request.Options, true);

            foreach (var rootDirectory in request.RootDirectories)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                ScanRoot(
                    rootDirectory,
                    discoveredPaths,
                    files,
                    directories,
                    issues,
                    stopwatch,
                    progress,
                    ref lastProgressReport,
                    request.Options,
                    cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            var status = cancellationToken.IsCancellationRequested ? ScanStatus.Cancelled : ScanStatus.Completed;
            var result = CreateResult(status, files, directories, issues, stopwatch.Elapsed);
            ReportProgress(null, progress, result.Statistics, stopwatch.Elapsed, ref lastProgressReport, request.Options, true);
            _logger.LogInformation(
                "Finished scanning with status {Status}. Discovered {FileCount} files and {DirectoryCount} directories.",
                result.Status,
                result.Statistics.FilesDiscovered,
                result.Statistics.DirectoriesDiscovered);
            return result;
        }
        catch (Exception exception)
        {
            _errorHandler.Report(new ApplicationError(
                LoggerCategory,
                "The scan could not be completed due to an unexpected error.",
                ApplicationErrorSeverity.Error,
                exception));
            throw;
        }
    }

    private void ScanRoot(
        string rootDirectory,
        HashSet<string> discoveredPaths,
        List<FileEntry> files,
        List<DirectoryEntry> directories,
        List<ScanIssue> issues,
        Stopwatch stopwatch,
        IProgress<ScanProgress>? progress,
        ref TimeSpan lastProgressReport,
        ScanOptions options,
        CancellationToken cancellationToken)
    {
        if (!TryGetAttributes(rootDirectory, true, issues, out var attributes))
        {
            return;
        }

        if (!attributes.HasFlag(FileAttributes.Directory))
        {
            RecordIssue(
                rootDirectory,
                ScanIssueKind.RootDirectoryUnavailable,
                "The requested root is not a directory.",
                issues);
            return;
        }

        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            RecordIssue(rootDirectory, ScanIssueKind.SymbolicLinkSkipped, "The symbolic link was skipped.", issues);
            return;
        }

        if (!discoveredPaths.Add(rootDirectory))
        {
            return;
        }

        var pendingDirectories = new Stack<string>();
        directories.Add(new DirectoryEntry(rootDirectory));
        ReportProgress(rootDirectory, progress, files.Count, directories.Count, issues.Count, stopwatch.Elapsed, ref lastProgressReport, options, false);
        pendingDirectories.Push(rootDirectory);
        while (pendingDirectories.Count > 0)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var currentDirectory = pendingDirectories.Pop();
            try
            {
                foreach (var entryPath in Directory.EnumerateFileSystemEntries(currentDirectory))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    ProcessEntry(
                        entryPath,
                        discoveredPaths,
                        pendingDirectories,
                        files,
                        directories,
                        issues,
                        stopwatch,
                        progress,
                        ref lastProgressReport,
                        options);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            catch (UnauthorizedAccessException exception)
            {
                RecordIssue(currentDirectory, ScanIssueKind.AccessDenied, "Access to the directory was denied.", issues, exception);
            }
            catch (IOException exception)
            {
                RecordIssue(currentDirectory, ScanIssueKind.DirectoryUnavailable, "The directory could not be enumerated.", issues, exception);
            }
        }
    }

    private void ProcessEntry(
        string entryPath,
        HashSet<string> discoveredPaths,
        Stack<string> pendingDirectories,
        List<FileEntry> files,
        List<DirectoryEntry> directories,
        List<ScanIssue> issues,
        Stopwatch stopwatch,
        IProgress<ScanProgress>? progress,
        ref TimeSpan lastProgressReport,
        ScanOptions options)
    {
        if (!TryGetAttributes(entryPath, false, issues, out var attributes))
        {
            return;
        }

        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            RecordIssue(entryPath, ScanIssueKind.SymbolicLinkSkipped, "The symbolic link was skipped.", issues);
            return;
        }

        var normalizedEntryPath = NormalizePath(entryPath);
        if (!discoveredPaths.Add(normalizedEntryPath))
        {
            return;
        }

        if (attributes.HasFlag(FileAttributes.Directory))
        {
            directories.Add(new DirectoryEntry(normalizedEntryPath));
            pendingDirectories.Push(normalizedEntryPath);
        }
        else
        {
            files.Add(new FileEntry(normalizedEntryPath));
        }

        ReportProgress(
            normalizedEntryPath,
            progress,
            files.Count,
            directories.Count,
            issues.Count,
            stopwatch.Elapsed,
            ref lastProgressReport,
            options,
            false);
    }

    private bool TryGetAttributes(
        string path,
        bool isRoot,
        List<ScanIssue> issues,
        out FileAttributes attributes)
    {
        try
        {
            attributes = File.GetAttributes(path);
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            attributes = default;
            RecordIssue(
                path,
                ScanIssueKind.AccessDenied,
                "Access to the filesystem location was denied.",
                issues,
                exception);
            return false;
        }
        catch (IOException exception)
        {
            attributes = default;
            RecordIssue(
                path,
                isRoot ? ScanIssueKind.RootDirectoryUnavailable : ScanIssueKind.EntryUnavailable,
                "The filesystem location could not be inspected.",
                issues,
                exception);
            return false;
        }
    }

    private void RecordIssue(
        string path,
        ScanIssueKind kind,
        string message,
        List<ScanIssue> issues,
        Exception? exception = null)
    {
        issues.Add(new ScanIssue(path, kind, message));
        _logger.LogWarning(exception, "Scanner issue {IssueKind}: {Message}", kind, message);
    }

    private static NormalizedScanRequest NormalizeRequest(ScanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.RootDirectories);
        ArgumentNullException.ThrowIfNull(request.Options);
        if (request.RootDirectories.Count == 0)
        {
            throw new ArgumentException("At least one root directory is required.", nameof(request));
        }

        if (request.Options.ProgressReportInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "The progress report interval cannot be negative.");
        }

        var rootDirectories = new HashSet<string>(PathComparer);
        foreach (var rootDirectory in request.RootDirectories)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
            rootDirectories.Add(NormalizePath(rootDirectory));
        }

        return new NormalizedScanRequest(rootDirectories.ToArray(), request.Options);
    }

    private static string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }

    private static ScanResult CreateResult(
        ScanStatus status,
        List<FileEntry> files,
        List<DirectoryEntry> directories,
        List<ScanIssue> issues,
        TimeSpan elapsed) => new(
        files.ToArray(),
        directories.ToArray(),
        new ScanStatistics(files.Count, directories.Count, issues.Count),
        issues.ToArray(),
        status,
        elapsed);

    private static void ReportProgress(
        string? currentPath,
        IProgress<ScanProgress>? progress,
        int fileCount,
        int directoryCount,
        int issueCount,
        TimeSpan elapsed,
        ref TimeSpan lastProgressReport,
        ScanOptions options,
        bool force)
    {
        ReportProgress(
            currentPath,
            progress,
            new ScanStatistics(fileCount, directoryCount, issueCount),
            elapsed,
            ref lastProgressReport,
            options,
            force);
    }

    private static void ReportProgress(
        string? currentPath,
        IProgress<ScanProgress>? progress,
        ScanStatistics statistics,
        TimeSpan elapsed,
        ref TimeSpan lastProgressReport,
        ScanOptions options,
        bool force)
    {
        if (progress is null || (!force && elapsed - lastProgressReport < options.ProgressReportInterval))
        {
            return;
        }

        progress.Report(new ScanProgress(currentPath, statistics, elapsed));
        lastProgressReport = elapsed;
    }

    private sealed record NormalizedScanRequest(
        IReadOnlyList<string> RootDirectories,
        ScanOptions Options);
}
