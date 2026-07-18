using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Rules.Tests;

/// <summary>
/// Verifies deterministic, side-effect-free rule evaluation.
/// </summary>
public sealed class RuleEngineTests
{
    /// <summary>
    /// Verifies one matching rule selects its proposed action.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_OneMatchingRule_SelectsAction()
    {
        var rule = Rule("documents", 1, Category(FileCategory.Document), new RuleAction(RuleActionKind.Move, "C:\\Documents"));
        var result = await CreateEngine().EvaluateAsync([File(category: FileCategory.Document)], [rule]);

        Assert.Equal(RuleActionKind.Move, Assert.Single(result.Decisions).Action.Kind);
        Assert.Equal("documents", result.Decisions[0].SelectedRuleId);
    }

    /// <summary>
    /// Verifies nonmatching and empty-rule evaluations produce no-action decisions.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_NoMatchesOrRules_ProducesNoAction()
    {
        var input = File(category: FileCategory.Image);
        var rule = Rule("documents", 1, Category(FileCategory.Document));
        var noMatch = await CreateEngine().EvaluateAsync([input], [rule]);
        var emptyRules = await CreateEngine().EvaluateAsync([input], Array.Empty<FileRule>());

        Assert.Equal(RuleActionKind.NoAction, noMatch.Decisions[0].Action.Kind);
        Assert.Null(noMatch.Decisions[0].SelectedRuleId);
        Assert.Equal(RuleActionKind.NoAction, emptyRules.Decisions[0].Action.Kind);
        Assert.Equal(0L, emptyRules.Statistics.RulesEvaluated);
    }

    /// <summary>
    /// Verifies rule conditions are combined using logical AND.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_MultipleConditions_RequireAllToMatch()
    {
        var rule = Rule("document-text", 1, Category(FileCategory.Document), Extension(".txt"));
        var result = await CreateEngine().EvaluateAsync([File(category: FileCategory.Document, extension: ".pdf")], [rule]);

        Assert.Equal(RuleActionKind.NoAction, result.Decisions[0].Action.Kind);
    }

    /// <summary>
    /// Verifies numeric priority wins and equal-priority ties retain supplied order.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SelectsHighestPriorityThenFirstRule()
    {
        var low = Rule("low", 1, Extension(".txt"), new RuleAction(RuleActionKind.Rename, NameTemplate: "low"));
        var high = Rule("high", 2, Extension(".txt"), new RuleAction(RuleActionKind.Rename, NameTemplate: "high"));
        var tie = Rule("tie", 2, Extension(".txt"), new RuleAction(RuleActionKind.Delete));
        var result = await CreateEngine().EvaluateAsync([File(extension: ".txt")], [low, high, tie]);

        Assert.Equal("high", result.Decisions[0].SelectedRuleId);
        Assert.Equal(["low", "high", "tie"], result.Decisions[0].MatchingRuleIds);
    }

    /// <summary>
    /// Verifies disabled rules are not evaluated or returned as matches.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DisabledRules_AreIgnored()
    {
        var disabled = Rule("disabled", 10, Extension(".txt"), isEnabled: false);
        var result = await CreateEngine().EvaluateAsync([File(extension: ".txt")], [disabled]);

        Assert.Equal(RuleActionKind.NoAction, result.Decisions[0].Action.Kind);
        Assert.Empty(result.Decisions[0].MatchingRuleIds);
        Assert.Equal(0L, result.Statistics.RulesEvaluated);
    }

    /// <summary>
    /// Verifies category and duplicate-status conditions use existing enriched values only.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_CategoryAndDuplicateConditions_MatchExistingValues()
    {
        var categoryRule = Rule("document", 1, Category(FileCategory.Document));
        var duplicateRule = Rule("duplicate", 2, Duplicate(DuplicateStatus.Duplicate));
        var matched = await CreateEngine().EvaluateAsync([File(category: FileCategory.Document, duplicate: DuplicateStatus.Duplicate)], [categoryRule, duplicateRule]);
        var missing = await CreateEngine().EvaluateAsync([File()], [categoryRule, duplicateRule]);

        Assert.Equal("duplicate", matched.Decisions[0].SelectedRuleId);
        Assert.Equal(RuleActionKind.NoAction, missing.Decisions[0].Action.Kind);
    }

    /// <summary>
    /// Verifies extension and exact filename comparisons are case-insensitive and need metadata.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_StringMetadataConditions_AreCaseInsensitive()
    {
        var extensionRule = Rule("extension", 1, Extension(".TXT"));
        var nameRule = Rule("name", 2, Name("README.TXT"));
        var result = await CreateEngine().EvaluateAsync([File(fileName: "readme.txt", extension: ".txt"), new FileEntry("missing")], [extensionRule, nameRule]);

        Assert.Equal("name", result.Decisions[0].SelectedRuleId);
        Assert.Equal(RuleActionKind.NoAction, result.Decisions[1].Action.Kind);
    }

    /// <summary>
    /// Verifies inclusive size boundaries and missing sizes do not match size rules.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_SizeConditions_AreInclusiveAndRequireSize()
    {
        var minimum = Rule("min", 1, Minimum(10));
        var maximum = Rule("max", 2, Maximum(10));
        var boundary = await CreateEngine().EvaluateAsync([File(size: 10)], [minimum, maximum]);
        var missing = await CreateEngine().EvaluateAsync([File(size: null)], [minimum, maximum]);

        Assert.Equal(["min", "max"], boundary.Decisions[0].MatchingRuleIds);
        Assert.Equal(RuleActionKind.NoAction, missing.Decisions[0].Action.Kind);
    }

    /// <summary>
    /// Verifies decisions preserve input order, duplicate entries, and all earlier file properties.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_PreservesInputsAndDoesNotModifyEntries()
    {
        var metadata = new FileMetadata("file.txt", ".txt", 1, null, null, null, FileAttributes.Normal);
        var hash = new FileHash("SHA-256", new string('a', 64));
        var classification = new FileClassification(FileCategory.Document);
        var duplicate = new DuplicateClassification(DuplicateStatus.Unique);
        var input = new FileEntry("non-filesystem-path", metadata, hash, classification, duplicate);
        var rule = Rule("match", 1, Extension(".txt"));
        var rules = new[] { rule };
        var result = await CreateEngine().EvaluateAsync([input, input], rules);

        Assert.Equal([input, input], result.Decisions.Select(decision => decision.File));
        Assert.Same(metadata, result.Decisions[0].File.Metadata);
        Assert.Same(hash, result.Decisions[0].File.Hash);
        Assert.Same(classification, result.Decisions[0].File.Classification);
        Assert.Same(duplicate, result.Decisions[0].File.Duplicate);
        Assert.Same(rule, rules[0]);
    }

    /// <summary>
    /// Verifies mixed-match statistics and empty-input statistics are accurate.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_StatisticsAndEmptyInput_AreAccurate()
    {
        var first = Rule("first", 1, Extension(".txt"));
        var second = Rule("second", 2, Category(FileCategory.Document));
        var mixed = await CreateEngine().EvaluateAsync([File(extension: ".txt", category: FileCategory.Document), File(extension: ".jpg")], [first, second]);
        var empty = await CreateEngine().EvaluateAsync(Array.Empty<FileEntry>(), [first]);

        Assert.Equal(new RuleEvaluationStatistics(2, 1, 1, 4, 2), mixed.Statistics);
        Assert.Equal(new RuleEvaluationStatistics(0, 0, 0, 0, 0), empty.Statistics);
    }

    /// <summary>
    /// Verifies null input collections and entries are rejected before processing.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_NullInputs_AreRejected()
    {
        IReadOnlyCollection<FileEntry> nullFiles = null!;
        IReadOnlyList<FileRule> nullRules = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateEngine().EvaluateAsync(nullFiles, Array.Empty<FileRule>()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateEngine().EvaluateAsync(Array.Empty<FileEntry>(), nullRules));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateEngine().EvaluateAsync(new List<FileEntry> { null! }, Array.Empty<FileRule>()));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateEngine().EvaluateAsync(Array.Empty<FileEntry>(), new List<FileRule> { null! }));
    }

    /// <summary>
    /// Verifies all documented invalid rule and action configurations are rejected before file evaluation.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidRules))]
    public async Task EvaluateAsync_InvalidRules_AreRejectedBeforeProcessing(FileRule rule)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateEngine().EvaluateAsync([File()], [rule]));
    }

    /// <summary>
    /// Verifies case-insensitive duplicate identifiers are rejected as one invalid request.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_DuplicateRuleIdentifiers_AreRejected()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateEngine().EvaluateAsync([File()], [Rule("same", 1, Extension(".txt")), Rule("SAME", 2, Extension(".txt"))]));
    }

    /// <summary>
    /// Verifies pre-cancellation and cancellation during evaluation produce no result.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_Cancellation_IsDeterministic()
    {
        using var preCancelled = new CancellationTokenSource();
        preCancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateEngine().EvaluateAsync([File()], Array.Empty<FileRule>(), preCancelled.Token));

        using var duringEvaluation = new CancellationTokenSource();
        var rules = new CancellingRuleList([Rule("rule", 1, Extension(".txt"))], duringEvaluation);
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateEngine().EvaluateAsync([File(extension: ".txt")], rules, duringEvaluation.Token));
    }

    /// <summary>
    /// Supplies invalid rules spanning every v0.1 validation boundary.
    /// </summary>
    public static IEnumerable<object[]> InvalidRules()
    {
        yield return [new FileRule("", "name", 0, [Extension(".txt")], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "", 0, [Extension(".txt")], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, Array.Empty<RuleCondition>(), new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, new List<RuleCondition> { null! }, new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition((RuleConditionKind)999)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.FileCategoryEquals)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.FileCategoryEquals, CategoryValue: FileCategory.Unknown)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.DuplicateStatusEquals, DuplicateStatusValue: (DuplicateStatus)999)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.ExtensionEquals, "txt")], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.ExactFileNameEquals, " ")], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.MinimumSizeInBytes, LongValue: -1)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.MaximumSizeInBytes, LongValue: -1)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.ExtensionEquals, ".txt", LongValue: 1)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [new RuleCondition(RuleConditionKind.FileCategoryEquals, StringValue: "extra", CategoryValue: FileCategory.Document)], new RuleAction(RuleActionKind.NoAction))];
        yield return [new FileRule("id", "name", 0, [Extension(".txt")], new RuleAction(RuleActionKind.Move))];
        yield return [new FileRule("id", "name", 0, [Extension(".txt")], new RuleAction(RuleActionKind.NoAction, "path"))];
        yield return [new FileRule("id", "name", 0, [Extension(".txt")], new RuleAction((RuleActionKind)999))];
    }

    private static RuleEngine CreateEngine() => new(new TestLoggingService(), new TestErrorHandler());

    private static FileEntry File(string fileName = "file.txt", string extension = ".txt", long? size = 1, FileCategory? category = null, DuplicateStatus? duplicate = null) =>
        new("not/a/filesystem/path", new FileMetadata(fileName, extension, size, null, null, null, FileAttributes.Normal), null,
            category is null ? null : new FileClassification(category.Value), duplicate is null ? null : new DuplicateClassification(duplicate.Value));

    private static FileRule Rule(string id, int priority, RuleCondition condition, RuleAction? action = null, bool isEnabled = true) =>
        new(id, id, priority, [condition], action ?? new RuleAction(RuleActionKind.Delete), isEnabled);

    private static FileRule Rule(string id, int priority, RuleCondition first, RuleCondition second, RuleAction? action = null, bool isEnabled = true) =>
        new(id, id, priority, [first, second], action ?? new RuleAction(RuleActionKind.Delete), isEnabled);

    private static RuleCondition Category(FileCategory value) => new(RuleConditionKind.FileCategoryEquals, CategoryValue: value);
    private static RuleCondition Duplicate(DuplicateStatus value) => new(RuleConditionKind.DuplicateStatusEquals, DuplicateStatusValue: value);
    private static RuleCondition Extension(string value) => new(RuleConditionKind.ExtensionEquals, StringValue: value);
    private static RuleCondition Name(string value) => new(RuleConditionKind.ExactFileNameEquals, StringValue: value);
    private static RuleCondition Minimum(long value) => new(RuleConditionKind.MinimumSizeInBytes, LongValue: value);
    private static RuleCondition Maximum(long value) => new(RuleConditionKind.MaximumSizeInBytes, LongValue: value);

    private sealed class CancellingRuleList : IReadOnlyList<FileRule>
    {
        private readonly CancellationTokenSource _source;
        private readonly IReadOnlyList<FileRule> _rules;
        private int _enumerations;

        public CancellingRuleList(IReadOnlyList<FileRule> rules, CancellationTokenSource source)
        {
            _rules = rules;
            _source = source;
        }

        public int Count => _rules.Count;

        public FileRule this[int index] => _rules[index];

        public IEnumerator<FileRule> GetEnumerator()
        {
            _enumerations++;
            if (_enumerations == 2)
            {
                _source.Cancel();
            }

            return _rules.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class TestLoggingService : ILoggingService
    {
        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
        public void Dispose() { }
        public void Initialize(LogLevel minimumLevel) { }
    }

    private sealed class TestErrorHandler : IErrorHandler
    {
        public event EventHandler<ApplicationError>? ErrorReported;
        public void Report(ApplicationError applicationError) => ErrorReported?.Invoke(this, applicationError);
    }
}
