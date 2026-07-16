using Microsoft.Extensions.Logging;
using TidyMind.Core.Errors;
using TidyMind.Core.Logging;
using TidyMind.Scanner.Models;

namespace TidyMind.Scanner;

/// <summary>
/// Performs deterministic metadata-only classification.
/// </summary>
public sealed class FileClassifier : IFileClassifier
{
    private const string LoggerCategory = "Scanner";
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a file classifier using shared diagnostics infrastructure.
    /// </summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler for unexpected operation failures.</param>
    public FileClassifier(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public Task<FileClassificationResult> ClassifyAsync(
        IReadOnlyCollection<FileEntry> files,
        FileClassificationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        cancellationToken.ThrowIfCancellationRequested();
        var rules = (options ?? FileClassificationOptions.Default).Rules;
        Validate(rules);

        try
        {
            var output = new List<FileEntry>(files.Count);
            var issues = new List<FileClassificationIssue>();
            long classified = 0;
            long unknown = 0;
            foreach (var entry in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var category = FileCategory.Unknown;
                if (entry.Metadata is null)
                {
                    issues.Add(new FileClassificationIssue(entry.FullPath, FileClassificationIssueKind.MetadataUnavailable, "File metadata is unavailable."));
                }
                else
                {
                    foreach (var rule in rules)
                    {
                        var value = rule.MatchKind == FileClassificationMatchKind.Extension ? entry.Metadata.Extension : entry.Metadata.FileName;
                        if (string.Equals(value, rule.Pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            category = rule.Category;
                            break;
                        }
                    }
                }

                if (category == FileCategory.Unknown)
                {
                    unknown++;
                }
                else
                {
                    classified++;
                }

                output.Add(entry with { Classification = new FileClassification(category) });
            }

            return Task.FromResult(new FileClassificationResult(
                output.ToArray(),
                new FileClassificationStatistics(files.Count, classified, unknown, issues.Count),
                issues.ToArray()));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "File classification could not be completed due to an unexpected error.");
            _errorHandler.Report(new ApplicationError(
                LoggerCategory,
                "File classification could not be completed due to an unexpected error.",
                ApplicationErrorSeverity.Error,
                exception));
            throw;
        }
    }

    private static void Validate(IReadOnlyList<FileClassificationRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Id) || !ids.Add(rule.Id) || string.IsNullOrWhiteSpace(rule.Pattern) ||
                rule.Category == FileCategory.Unknown || !Enum.IsDefined(rule.MatchKind) ||
                (rule.MatchKind == FileClassificationMatchKind.Extension && !rule.Pattern.StartsWith('.')))
            {
                throw new ArgumentException("Invalid classification rules.");
            }
        }
    }
}
