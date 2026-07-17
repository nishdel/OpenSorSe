using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>
/// Identifies exact-content duplicate entries from previously calculated SHA-256 hashes.
/// </summary>
public interface IDuplicateDetector
{
    /// <summary>
    /// Recalculates duplicate classifications and groups for the supplied entries.
    /// </summary>
    /// <param name="files">The entries to evaluate in their required output order.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The enriched entries, duplicate groups, statistics, and recoverable issues.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="files"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when an input collection contains a null entry.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Task<DuplicateDetectionResult> DetectAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken = default);
}
