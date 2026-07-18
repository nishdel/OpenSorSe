using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSorSe.Core.Errors;
using OpenSorSe.Core.Logging;
using OpenSorSe.Rules.Models;
using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Rules.Tests;

/// <summary>
/// Verifies deterministic, intra-plan lexical conflict resolution.
/// </summary>
public sealed class ConflictResolverTests
{
    /// <summary>
    /// Verifies supported non-conflicting operations are preserved in input order.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_AcceptsSupportedOperationsInOrder()
    {
        var operations = new[]
        {
            Operation("plan:0", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\one.txt"),
            Operation("plan:1", PlannedOperationKind.Copy, "C:\\Source\\two.txt", "C:\\Destination\\two.txt"),
            Operation("plan:2", PlannedOperationKind.Rename, "C:\\Source\\three.txt", "C:\\Source\\renamed.txt"),
            Operation("plan:3", PlannedOperationKind.Delete, "C:\\Source\\four.txt"),
        };

        var result = await CreateResolver().ResolveAsync(operations);

        Assert.Equal(operations, result.Operations);
        Assert.Equal(new ConflictResolutionStatistics(4, 4, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0), result.Statistics);
    }

    /// <summary>
    /// Verifies repeated operation identifiers are distinguished from duplicate operation signatures.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_DuplicateOperationIdAndSignature_HaveDistinctIssues()
    {
        var first = Operation("plan:0", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\one.txt");
        var sameId = Operation("plan:0", PlannedOperationKind.Copy, "C:\\Source\\two.txt", "C:\\Destination\\two.txt");
        var sameSignature = Operation("plan:2", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\one.txt");
        var result = await CreateResolver().ResolveAsync([first, sameId, sameSignature]);

        Assert.Equal([ConflictResolutionIssueKind.DuplicateOperationId, ConflictResolutionIssueKind.DuplicateOperation], result.Issues.Select(issue => issue.Kind));
        Assert.Equal("plan:0", result.Issues[0].ConflictingOperationId);
        Assert.Equal("plan:0", result.Issues[1].ConflictingOperationId);
        Assert.Equal(1L, result.Statistics.DuplicateOperationIds);
        Assert.Equal(1L, result.Statistics.DuplicateOperations);
    }

    /// <summary>
    /// Verifies an identical signature has precedence over destination and source conflicts.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_DuplicateSignature_HasConflictPrecedence()
    {
        var first = Operation("plan:0", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\one.txt");
        var duplicate = Operation("plan:1", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\one.txt");
        var result = await CreateResolver().ResolveAsync([first, duplicate]);

        Assert.Equal(ConflictResolutionIssueKind.DuplicateOperation, Assert.Single(result.Issues).Kind);
    }

    /// <summary>
    /// Verifies destination ownership retains the earliest accepted operation.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_DestinationConflict_KeepsFirstOperation()
    {
        var first = Operation("plan:0", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\same.txt");
        var later = Operation("plan:1", PlannedOperationKind.Copy, "C:\\Source\\two.txt", "C:\\Destination\\same.txt");
        var result = await CreateResolver().ResolveAsync([first, later]);

        Assert.Same(first, Assert.Single(result.Operations));
        Assert.Equal(ConflictResolutionIssueKind.DestinationConflict, Assert.Single(result.Issues).Kind);
        Assert.Equal("plan:0", result.Issues[0].ConflictingOperationId);
    }

    /// <summary>
    /// Verifies copies from one source to distinct destinations are accepted, while source mutation is rejected.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_SourceConflicts_AreDeterministic()
    {
        var copyOne = Operation("plan:0", PlannedOperationKind.Copy, "C:\\Source\\one.txt", "C:\\Destination\\one.txt");
        var copyTwo = Operation("plan:1", PlannedOperationKind.Copy, "C:\\Source\\one.txt", "C:\\Destination\\two.txt");
        var move = Operation("plan:2", PlannedOperationKind.Move, "C:\\Source\\one.txt", "C:\\Destination\\three.txt");
        var result = await CreateResolver().ResolveAsync([copyOne, copyTwo, move]);

        Assert.Equal([copyOne, copyTwo], result.Operations);
        Assert.Equal(ConflictResolutionIssueKind.SourceConflict, Assert.Single(result.Issues).Kind);
        Assert.Equal("plan:0", result.Issues[0].ConflictingOperationId);
    }

    /// <summary>
    /// Verifies invalid operations are isolated and do not prevent later valid operations.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_InvalidOperation_ContinuesLaterOperations()
    {
        var invalid = Operation("plan:0", PlannedOperationKind.Move, "relative\\one.txt", "C:\\Destination\\one.txt");
        var valid = Operation("plan:1", PlannedOperationKind.Move, "C:\\Source\\two.txt", "C:\\Destination\\one.txt");
        var result = await CreateResolver().ResolveAsync([invalid, valid]);

        Assert.Same(valid, Assert.Single(result.Operations));
        Assert.Equal(ConflictResolutionIssueKind.InvalidOperation, Assert.Single(result.Issues).Kind);
    }

    /// <summary>
    /// Verifies malformed operation fields yield recoverable invalid-operation issues.
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidOperations))]
    public async Task ResolveAsync_MalformedOperation_ReturnsInvalidOperationIssue(PlannedOperation operation)
    {
        var result = await CreateResolver().ResolveAsync([operation]);

        Assert.Empty(result.Operations);
        Assert.Equal(ConflictResolutionIssueKind.InvalidOperation, Assert.Single(result.Issues).Kind);
    }

    /// <summary>
    /// Verifies normalized source and file paths must agree while accepted entries remain immutable.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_ValidatesSourceAgainstFilePathWithoutMutatingInput()
    {
        var file = new FileEntry("C:\\Source\\file.txt");
        var invalid = new PlannedOperation("plan:0", PlannedOperationKind.Delete, file, "C:\\Source\\other.txt", null, "rule", "Rule", 1);
        var valid = Operation("plan:1", PlannedOperationKind.Delete, "C:\\Source\\file.txt", file);
        var result = await CreateResolver().ResolveAsync([invalid, valid]);

        Assert.Same(file, Assert.Single(result.Operations).File);
        Assert.Equal("C:\\Source\\file.txt", file.FullPath);
        Assert.Equal(ConflictResolutionIssueKind.InvalidOperation, Assert.Single(result.Issues).Kind);
    }

    /// <summary>
    /// Verifies empty input, null collections, and null operation entries follow the collection contract.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CollectionValidationAndEmptyInput_AreDeterministic()
    {
        IReadOnlyCollection<PlannedOperation> nullOperations = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateResolver().ResolveAsync(nullOperations));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateResolver().ResolveAsync(new List<PlannedOperation> { null! }));
        await Assert.ThrowsAsync<ArgumentException>(() => CreateResolver().ResolveAsync(Array.Empty<PlannedOperation>(), new ConflictResolutionOptions((ConflictResolutionStrategy)999)));

        var result = await CreateResolver().ResolveAsync(Array.Empty<PlannedOperation>());
        Assert.Equal(new ConflictResolutionStatistics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), result.Statistics);
    }

    /// <summary>
    /// Verifies cancellation before and during controlled collection processing returns no result.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_Cancellation_IsDeterministic()
    {
        using var preCancelled = new CancellationTokenSource();
        preCancelled.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateResolver().ResolveAsync(Array.Empty<PlannedOperation>(), cancellationToken: preCancelled.Token));

        using var processingCancellation = new CancellationTokenSource();
        var operations = new CancellingOperationCollection([Operation("plan:0", PlannedOperationKind.Delete, "C:\\Source\\one.txt")], processingCancellation);
        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateResolver().ResolveAsync(operations, cancellationToken: processingCancellation.Token));
    }

    /// <summary>
    /// Supplies lexical operation-contract violations for validation coverage.
    /// </summary>
    public static IEnumerable<object[]> InvalidOperations()
    {
        yield return [Operation("", PlannedOperationKind.Delete, "C:\\Source\\one.txt")];
        yield return [Operation("plan:0", (PlannedOperationKind)999, "C:\\Source\\one.txt")];
        yield return [Operation("plan:0", PlannedOperationKind.Move, "C:\\Source\\one.txt")];
        yield return [Operation("plan:0", PlannedOperationKind.Delete, "C:\\Source\\one.txt", "C:\\Destination\\one.txt")];
        yield return [Operation("plan:0", PlannedOperationKind.Copy, "relative\\one.txt", "C:\\Destination\\one.txt")];
    }

    private static ConflictResolver CreateResolver() => new(new TestLoggingService(), new TestErrorHandler());

    private static PlannedOperation Operation(string id, PlannedOperationKind kind, string source, string? destination = null) =>
        Operation(id, kind, source, new FileEntry(source), destination);

    private static PlannedOperation Operation(string id, PlannedOperationKind kind, string source, FileEntry file, string? destination = null) =>
        new(id, kind, file, source, destination, "rule", "Rule", 1);

    private sealed class CancellingOperationCollection : IReadOnlyCollection<PlannedOperation>
    {
        private readonly CancellationTokenSource _source;
        private readonly IReadOnlyCollection<PlannedOperation> _operations;
        private int _enumerations;

        public CancellingOperationCollection(IReadOnlyCollection<PlannedOperation> operations, CancellationTokenSource source)
        {
            _operations = operations;
            _source = source;
        }

        public int Count => _operations.Count;

        public IEnumerator<PlannedOperation> GetEnumerator()
        {
            _enumerations++;
            if (_enumerations == 2)
            {
                _source.Cancel();
            }

            return _operations.GetEnumerator();
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
