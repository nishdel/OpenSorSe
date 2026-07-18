using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>
/// Discovers files and directories below user-selected root directories without modifying them.
/// </summary>
public interface IFileScanner
{
    /// <summary>
    /// Scans the requested root directories on a single background worker.
    /// </summary>
    /// <param name="request">The roots and options that define the scan.</param>
    /// <param name="progress">An optional receiver for scan progress updates.</param>
    /// <param name="cancellationToken">A token used to request cooperative cancellation.</param>
    /// <returns>A task containing completed or partial cancelled scan results.</returns>
    Task<ScanResult> ScanAsync(
        ScanRequest request,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
