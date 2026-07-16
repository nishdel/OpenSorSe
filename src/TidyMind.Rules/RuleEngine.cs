using Microsoft.Extensions.Logging;
using TidyMind.Core.Errors;
using TidyMind.Core.Logging;
using TidyMind.Rules.Models;
using TidyMind.Scanner.Models;

namespace TidyMind.Rules;

/// <summary>
/// Performs deterministic, side-effect-free evaluation of configured file rules.
/// </summary>
public sealed class RuleEngine : IRuleEngine
{
    private const string LoggerCategory = "Rules";
    private readonly IErrorHandler _errorHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a rule engine using shared diagnostics infrastructure.
    /// </summary>
    /// <param name="loggingService">The centralized logging service.</param>
    /// <param name="errorHandler">The handler for unexpected operation failures.</param>
    public RuleEngine(ILoggingService loggingService, IErrorHandler errorHandler)
    {
        ArgumentNullException.ThrowIfNull(loggingService);
        _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));
        _logger = loggingService.CreateLogger(LoggerCategory);
    }

    /// <inheritdoc />
    public Task<RuleEvaluationResult> EvaluateAsync(
        IReadOnlyCollection<FileEntry> files,
        IReadOnlyList<FileRule> rules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(rules);
        cancellationToken.ThrowIfCancellationRequested();
        ValidateFiles(files);
        ValidateRules(rules, cancellationToken);

        try
        {
            return Task.FromResult(Evaluate(files, rules, cancellationToken));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Rule evaluation could not be completed due to an unexpected error.");
            _errorHandler.Report(new ApplicationError(
                LoggerCategory,
                "Rule evaluation could not be completed due to an unexpected error.",
                ApplicationErrorSeverity.Error,
                exception));
            throw;
        }
    }

    private static RuleEvaluationResult Evaluate(
        IReadOnlyCollection<FileEntry> files,
        IReadOnlyList<FileRule> rules,
        CancellationToken cancellationToken)
    {
        var decisions = new List<RuleDecision>(files.Count);
        long filesWithMatches = 0;
        long filesWithoutMatches = 0;
        long rulesEvaluated = 0;
        long ruleMatches = 0;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var matchingRules = new List<FileRule>();
            foreach (var rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!rule.IsEnabled)
                {
                    continue;
                }

                rulesEvaluated++;
                if (Matches(file, rule))
                {
                    ruleMatches++;
                    matchingRules.Add(rule);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (matchingRules.Count == 0)
            {
                filesWithoutMatches++;
                decisions.Add(new RuleDecision(file, new RuleAction(RuleActionKind.NoAction), null, null, null, Array.Empty<string>()));
                continue;
            }

            filesWithMatches++;
            var selected = matchingRules[0];
            foreach (var match in matchingRules.Skip(1))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (match.Priority > selected.Priority)
                {
                    selected = match;
                }
            }

            decisions.Add(new RuleDecision(
                file,
                selected.Action,
                selected.Id,
                selected.Name,
                selected.Priority,
                matchingRules.Select(rule => rule.Id).ToArray()));
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new RuleEvaluationResult(
            decisions.ToArray(),
            new RuleEvaluationStatistics(files.Count, filesWithMatches, filesWithoutMatches, rulesEvaluated, ruleMatches));
    }

    private static bool Matches(FileEntry file, FileRule rule) => rule.Conditions.All(condition => Matches(file, condition));

    private static bool Matches(FileEntry file, RuleCondition condition) => condition.Kind switch
    {
        RuleConditionKind.FileCategoryEquals => file.Classification?.Category == condition.CategoryValue,
        RuleConditionKind.DuplicateStatusEquals => file.Duplicate?.Status == condition.DuplicateStatusValue,
        RuleConditionKind.ExtensionEquals => string.Equals(file.Metadata?.Extension, condition.StringValue, StringComparison.OrdinalIgnoreCase),
        RuleConditionKind.ExactFileNameEquals => string.Equals(file.Metadata?.FileName, condition.StringValue, StringComparison.OrdinalIgnoreCase),
        RuleConditionKind.MinimumSizeInBytes => file.Metadata?.SizeInBytes is long size && size >= condition.LongValue,
        RuleConditionKind.MaximumSizeInBytes => file.Metadata?.SizeInBytes is long size && size <= condition.LongValue,
        _ => false,
    };

    private static void ValidateFiles(IReadOnlyCollection<FileEntry> files)
    {
        if (files.Any(file => file is null))
        {
            throw new ArgumentException("The file collection cannot contain null entries.", nameof(files));
        }
    }

    private static void ValidateRules(IReadOnlyList<FileRule> rules, CancellationToken cancellationToken)
    {
        var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (rule is null)
            {
                throw new ArgumentException("The rule collection cannot contain null entries.", nameof(rules));
            }

            if (string.IsNullOrWhiteSpace(rule.Id) || !ruleIds.Add(rule.Id) || string.IsNullOrWhiteSpace(rule.Name) ||
                rule.Conditions is null || rule.Conditions.Count == 0 || rule.Action is null)
            {
                throw new ArgumentException("A rule is invalid.", nameof(rules));
            }

            ValidateAction(rule.Action, rules);
            foreach (var condition in rule.Conditions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (condition is null)
                {
                    throw new ArgumentException("A rule cannot contain a null condition.", nameof(rules));
                }

                ValidateCondition(condition, rules);
            }
        }
    }

    private static void ValidateAction(RuleAction action, IReadOnlyList<FileRule> rules)
    {
        var isValid = action.Kind switch
        {
            RuleActionKind.NoAction => action.DestinationPath is null && action.NameTemplate is null,
            RuleActionKind.Move or RuleActionKind.Copy => !string.IsNullOrWhiteSpace(action.DestinationPath),
            RuleActionKind.Rename => !string.IsNullOrWhiteSpace(action.NameTemplate),
            RuleActionKind.Delete => action.DestinationPath is null && action.NameTemplate is null,
            _ => false,
        };

        if (!isValid)
        {
            throw new ArgumentException("A rule action is invalid.", nameof(rules));
        }
    }

    private static void ValidateCondition(RuleCondition condition, IReadOnlyList<FileRule> rules)
    {
        var noExtraValues = condition.StringValue is null && condition.LongValue is null && condition.CategoryValue is null && condition.DuplicateStatusValue is null;
        var isValid = condition.Kind switch
        {
            RuleConditionKind.FileCategoryEquals => condition.CategoryValue is not null && condition.CategoryValue != FileCategory.Unknown &&
                Enum.IsDefined(condition.CategoryValue.Value) && condition.StringValue is null && condition.LongValue is null && condition.DuplicateStatusValue is null,
            RuleConditionKind.DuplicateStatusEquals => condition.DuplicateStatusValue is not null && Enum.IsDefined(condition.DuplicateStatusValue.Value) &&
                condition.StringValue is null && condition.LongValue is null && condition.CategoryValue is null,
            RuleConditionKind.ExtensionEquals => !string.IsNullOrWhiteSpace(condition.StringValue) && condition.StringValue.StartsWith('.') &&
                condition.LongValue is null && condition.CategoryValue is null && condition.DuplicateStatusValue is null,
            RuleConditionKind.ExactFileNameEquals => !string.IsNullOrWhiteSpace(condition.StringValue) && condition.LongValue is null &&
                condition.CategoryValue is null && condition.DuplicateStatusValue is null,
            RuleConditionKind.MinimumSizeInBytes or RuleConditionKind.MaximumSizeInBytes => condition.LongValue is >= 0 &&
                condition.StringValue is null && condition.CategoryValue is null && condition.DuplicateStatusValue is null,
            _ => false,
        };

        if (!isValid || noExtraValues)
        {
            throw new ArgumentException("A rule condition is invalid.", nameof(rules));
        }
    }
}
