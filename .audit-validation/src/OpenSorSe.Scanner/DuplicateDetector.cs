using Microsoft.Extensions.Logging;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>
/// Performs deterministic, read-free exact duplicate detection using normalized SHA-256 hashes.
/// </summary>
public sealed class DuplicateDetector : IDuplicateDetector
{
    private const string Algorithm = "SHA-256";
    private const string LoggerCategory = "Scanner";
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a duplicate detector using shared diagnostics infrastructure.
    /// </summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler for unexpected operation failures.</param>
    public DuplicateDetector(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public Task<DuplicateDetectionResult> DetectAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEntries(files);

        try
        {
            var result = Detect(files, cancellationToken);
            return Task.FromResult(result);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Duplicate detection could not be completed due to an unexpected error.");
            _errorHandler.Report(new ApplicationError(
                LoggerCategory,
                "Duplicate detection could not be completed due to an unexpected error.",
                ApplicationErrorSeverity.Error,
                exception));
            throw;
        }
    }

    private DuplicateDetectionResult Detect(IReadOnlyCollection<FileEntry> files, CancellationToken cancellationToken)
    {
        var preparedEntries = new List<PreparedEntry>(files.Count);
        var issues = new List<DuplicateDetectionIssue>();
        var bucketsByHash = new Dictionary<string, HashBucket>(StringComparer.Ordinal);
        var bucketsInInputOrder = new List<HashBucket>();

        foreach (var entry in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryNormalizeHash(entry, out var normalizedHash, out var issue))
            {
                issues.Add(issue!);
                _logger.LogWarning("Duplicate-detection issue {IssueKind}: {Message}", issue!.Kind, issue.Message);
                preparedEntries.Add(new PreparedEntry(entry, null));
                continue;
            }

            if (!bucketsByHash.TryGetValue(normalizedHash!, out var bucket))
            {
                bucket = new HashBucket(normalizedHash!);
                bucketsByHash.Add(normalizedHash!, bucket);
                bucketsInInputOrder.Add(bucket);
            }

            bucket.EntryIndexes.Add(preparedEntries.Count);
            preparedEntries.Add(new PreparedEntry(entry, normalizedHash));
        }

        var output = new List<FileEntry>(preparedEntries.Count);
        long filesUnique = 0;
        long filesDuplicate = 0;
        long filesUnknown = 0;

        foreach (var prepared in preparedEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (prepared.NormalizedHash is null)
            {
                filesUnknown++;
                output.Add(prepared.Entry with
                {
                    Duplicate = new DuplicateClassification(DuplicateStatus.Unknown),
                });
                continue;
            }

            var bucket = bucketsByHash[prepared.NormalizedHash];
            if (bucket.EntryIndexes.Count > 1)
            {
                filesDuplicate++;
                output.Add(prepared.Entry with
                {
                    Duplicate = new DuplicateClassification(DuplicateStatus.Duplicate, bucket.GroupId),
                });
            }
            else
            {
                filesUnique++;
                output.Add(prepared.Entry with
                {
                    Duplicate = new DuplicateClassification(DuplicateStatus.Unique),
                });
            }
        }

        var groups = new List<DuplicateGroup>();
        foreach (var bucket in bucketsInInputOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (bucket.EntryIndexes.Count < 2)
            {
                continue;
            }

            var groupFiles = new List<FileEntry>(bucket.EntryIndexes.Count);
            foreach (var index in bucket.EntryIndexes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                groupFiles.Add(output[index]);
            }

            groups.Add(new DuplicateGroup(bucket.GroupId, Algorithm, bucket.HashValue, groupFiles.ToArray()));
        }

        return new DuplicateDetectionResult(
            output.ToArray(),
            groups.ToArray(),
            new DuplicateDetectionStatistics(files.Count, filesUnique, filesDuplicate, filesUnknown, groups.Count, issues.Count),
            issues.ToArray());
    }

    private static void ValidateEntries(IReadOnlyCollection<FileEntry> files)
    {
        if (files.Any(entry => entry is null))
        {
            throw new ArgumentException("The input collection cannot contain null entries.", nameof(files));
        }
    }

    private static bool TryNormalizeHash(
        FileEntry entry,
        out string? normalizedHash,
        out DuplicateDetectionIssue? issue)
    {
        normalizedHash = null;
        issue = null;
        if (entry.Hash is null)
        {
            issue = new DuplicateDetectionIssue(entry.FullPath, DuplicateDetectionIssueKind.HashUnavailable, "No hash is available for this file.");
            return false;
        }

        if (!string.Equals(entry.Hash.Algorithm, Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            issue = new DuplicateDetectionIssue(entry.FullPath, DuplicateDetectionIssueKind.UnsupportedHashAlgorithm, "The file hash algorithm is unsupported.");
            return false;
        }

        var value = entry.Hash.Value;
        if (string.IsNullOrEmpty(value) || value.Length != 64 || !value.All(Uri.IsHexDigit))
        {
            issue = new DuplicateDetectionIssue(entry.FullPath, DuplicateDetectionIssueKind.InvalidHashValue, "The SHA-256 hash value is invalid.");
            return false;
        }

        normalizedHash = value.ToLowerInvariant();
        return true;
    }

    private sealed record PreparedEntry(FileEntry Entry, string? NormalizedHash);

    private sealed class HashBucket
    {
        public HashBucket(string hashValue)
        {
            HashValue = hashValue;
            GroupId = $"sha256:{hashValue}";
        }

        public List<int> EntryIndexes { get; } = [];

        public string GroupId { get; }

        public string HashValue { get; }
    }
}
