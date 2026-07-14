using Microsoft.Extensions.Logging;
using TidyMind.Core.Errors;
using TidyMind.Core.Logging;
using TidyMind.Scanner.Models;

namespace TidyMind.Scanner;

/// <summary>
/// Reads operating-system filesystem metadata without opening or modifying file contents.
/// </summary>
public sealed class FileMetadataReader : IFileMetadataReader
{
    private const string LoggerCategory = "Scanner";
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a metadata reader that records diagnostics through Core infrastructure.
    /// </summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler used for unexpected operation-level failures.</param>
    public FileMetadataReader(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public Task<FileMetadataResult> ReadAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => Read(files, cancellationToken), CancellationToken.None);
    }

    private FileMetadataResult Read(IReadOnlyCollection<FileEntry> files, CancellationToken cancellationToken)
    {
        var enrichedFiles = new List<FileEntry>(files.Count);
        var issues = new List<FileMetadataIssue>();
        long enrichedCount = 0;

        try
        {
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var enrichedFile = ReadFile(file, issues);
                if (enrichedFile.Metadata is not null)
                {
                    enrichedCount++;
                }

                enrichedFiles.Add(enrichedFile);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new FileMetadataResult(
                enrichedFiles.ToArray(),
                new FileMetadataStatistics(files.Count, enrichedCount, issues.Count),
                issues.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _errorHandler.Report(new ApplicationError(
                LoggerCategory,
                "File metadata processing could not be completed due to an unexpected error.",
                ApplicationErrorSeverity.Error,
                exception));
            throw;
        }
    }

    private FileEntry ReadFile(FileEntry? file, List<FileMetadataIssue> issues)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.FullPath))
        {
            RecordIssue(file?.FullPath ?? string.Empty, FileMetadataIssueKind.FileUnavailable, "The file path is invalid.", issues);
            return file ?? new FileEntry(string.Empty);
        }

        string fileName;
        string extension;
        try
        {
            fileName = Path.GetFileName(file.FullPath);
            extension = Path.GetExtension(file.FullPath).ToLowerInvariant();
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            RecordIssue(file.FullPath, FileMetadataIssueKind.FileUnavailable, "The file path is invalid.", issues, exception);
            return file;
        }
        var metadataUnavailable = false;
        FileAttributes? attributes = null;
        long? sizeInBytes = null;
        DateTimeOffset? creationTimeUtc = null;
        DateTimeOffset? lastWriteTimeUtc = null;
        DateTimeOffset? lastAccessTimeUtc = null;
        var fileInfo = new FileInfo(file.FullPath);

        try
        {
            attributes = File.GetAttributes(file.FullPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            RecordIssue(file.FullPath, FileMetadataIssueKind.AccessDenied, "Access to the file metadata was denied.", issues, exception);
            return file;
        }
        catch (Exception exception) when (IsRecoverableFilesystemException(exception))
        {
            RecordIssue(file.FullPath, FileMetadataIssueKind.FileUnavailable, "The file is unavailable.", issues, exception);
            return file;
        }

        TryRead(() => fileInfo.Length, value => sizeInBytes = value, ref metadataUnavailable);
        TryRead(() => ToUtcOffset(fileInfo.CreationTimeUtc), value => creationTimeUtc = value, ref metadataUnavailable);
        TryRead(() => ToUtcOffset(fileInfo.LastWriteTimeUtc), value => lastWriteTimeUtc = value, ref metadataUnavailable);
        TryRead(() => ToUtcOffset(fileInfo.LastAccessTimeUtc), value => lastAccessTimeUtc = value, ref metadataUnavailable);

        if (metadataUnavailable)
        {
            RecordIssue(file.FullPath, FileMetadataIssueKind.MetadataUnavailable, "Some filesystem metadata could not be retrieved.", issues);
        }

        return file with
        {
            Metadata = new FileMetadata(
                fileName,
                extension,
                sizeInBytes,
                creationTimeUtc,
                lastWriteTimeUtc,
                lastAccessTimeUtc,
                attributes.Value),
        };
    }

    private static void TryRead<T>(Func<T> read, Action<T> assign, ref bool metadataUnavailable)
    {
        try
        {
            assign(read());
        }
        catch (Exception exception) when (IsRecoverableFilesystemException(exception))
        {
            metadataUnavailable = true;
        }
    }

    private void RecordIssue(
        string filePath,
        FileMetadataIssueKind kind,
        string message,
        List<FileMetadataIssue> issues,
        Exception? exception = null)
    {
        issues.Add(new FileMetadataIssue(filePath, kind, message));
        _logger.LogWarning(exception, "Metadata issue {IssueKind}: {Message}", kind, message);
    }

    private static bool IsRecoverableFilesystemException(Exception exception) => exception is
        IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;

    private static DateTimeOffset ToUtcOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
