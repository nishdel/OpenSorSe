using TidyMind.Rules.Models;

namespace TidyMind.Rules;

/// <summary>
/// Resolves lexical conflicts within planned operations without inspecting the filesystem.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    /// Resolves conflicts in supplied operation order using the requested deterministic strategy.
    /// </summary>
    /// <param name="operations">The planned operations to validate and resolve.</param>
    /// <param name="options">Optional conflict-resolution options.</param>
    /// <param name="cancellationToken">A token used to cancel resolution.</param>
    /// <returns>The accepted operations, statistics, and recoverable issues.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operations"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the collection contains null entries or options are invalid.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<ConflictResolutionResult> ResolveAsync(IReadOnlyCollection<PlannedOperation> operations, ConflictResolutionOptions? options = null, CancellationToken cancellationToken = default);
}
