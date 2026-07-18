using System.Buffers;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>Generates read-only SHA-256 fingerprints using bounded asynchronous streaming.</summary>
public sealed class FileHasher : IFileHasher
{
    private const int BufferSize = 64 * 1024;
    private const string LoggerCategory = "Scanner";
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>Initializes a file hasher using shared diagnostics infrastructure.</summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler for unexpected operation failures.</param>
    public FileHasher(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public async Task<FileHashResult> HashAsync(IReadOnlyCollection<FileEntry> files, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        cancellationToken.ThrowIfCancellationRequested();
        var output = new List<FileEntry>(files.Count);
        var issues = new List<FileHashIssue>();
        long filesHashed = 0;
        long bytesHashed = 0;

        try
        {
            foreach (var entry in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var hashed = await HashEntryAsync(entry, issues, cancellationToken, bytes => bytesHashed += bytes).ConfigureAwait(false);
                if (hashed.Hash is not null)
                {
                    filesHashed++;
                }

                output.Add(hashed);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return new FileHashResult(output.ToArray(), new FileHashStatistics(files.Count, filesHashed, bytesHashed, issues.Count), issues.ToArray());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _errorHandler.Report(new ApplicationError(LoggerCategory, "File hashing could not be completed due to an unexpected error.", ApplicationErrorSeverity.Error, exception));
            throw;
        }
    }

    private async Task<FileEntry> HashEntryAsync(FileEntry? entry, List<FileHashIssue> issues, CancellationToken cancellationToken, Action<int> bytesRead)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.FullPath))
        {
            RecordIssue(entry?.FullPath ?? string.Empty, FileHashIssueKind.FileUnavailable, "The file path is invalid.", issues);
            return entry ?? new FileEntry(string.Empty);
        }

        var output = entry with { Hash = null };
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(entry.FullPath);
        }
        catch (UnauthorizedAccessException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.AccessDenied, "Access to the file was denied.", issues, exception);
            return output;
        }
        catch (FileNotFoundException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnavailable, "The file is unavailable.", issues, exception);
            return output;
        }
        catch (DirectoryNotFoundException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnavailable, "The file is unavailable.", issues, exception);
            return output;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnavailable, "The file path is invalid.", issues, exception);
            return output;
        }
        catch (IOException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnreadable, "The file attributes could not be read.", issues, exception);
            return output;
        }

        if (attributes.HasFlag(FileAttributes.ReparsePoint))
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.ReparsePointSkipped, "The reparse point was skipped.", issues);
            return output;
        }

        if (attributes.HasFlag(FileAttributes.Directory))
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.NonRegularFileSkipped, "The non-regular file entry was skipped.", issues);
            return output;
        }

        try
        {
            var before = ReadState(entry.FullPath);
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                await using var stream = new FileStream(entry.FullPath, new FileStreamOptions
                {
                    Access = FileAccess.Read,
                    Mode = FileMode.Open,
                    Share = FileShare.ReadWrite | FileShare.Delete,
                    Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
                    BufferSize = BufferSize,
                });

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var read = await stream.ReadAsync(buffer.AsMemory(0, BufferSize), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        break;
                    }

                    hash.AppendData(buffer, 0, read);
                    bytesRead(read);
                }

                var after = ReadState(entry.FullPath);
                if (before != after)
                {
                    RecordIssue(entry.FullPath, FileHashIssueKind.FileChangedDuringHashing, "The file changed while it was being hashed.", issues);
                    return output;
                }

                return output with { Hash = new FileHash("SHA-256", Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()) };
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.AccessDenied, "Access to the file was denied.", issues, exception);
        }
        catch (FileNotFoundException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnavailable, "The file is unavailable.", issues, exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnavailable, "The file is unavailable.", issues, exception);
        }
        catch (IOException exception)
        {
            RecordIssue(entry.FullPath, FileHashIssueKind.FileUnreadable, "The file stream could not be read.", issues, exception);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return output;
    }

    private static FileState ReadState(string path)
    {
        var fileInfo = new FileInfo(path);
        fileInfo.Refresh();
        return new FileState(fileInfo.Length, fileInfo.LastWriteTimeUtc);
    }

    private void RecordIssue(string path, FileHashIssueKind kind, string message, List<FileHashIssue> issues, Exception? exception = null)
    {
        issues.Add(new FileHashIssue(path, kind, message));
        _logger.LogWarning(exception, "Hashing issue {IssueKind}: {Message}", kind, message);
    }

    private sealed record FileState(long Length, DateTime LastWriteTimeUtc);
}
