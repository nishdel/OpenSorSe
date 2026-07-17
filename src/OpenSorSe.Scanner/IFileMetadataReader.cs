using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>
/// Enriches discovered file entries with filesystem metadata.
/// </summary>
public interface IFileMetadataReader
{
    /// <summary>
    /// Reads filesystem metadata for the supplied entries while preserving their order and multiplicity.
    /// </summary>
    /// <param name="files">The discovered file entries to enrich.</param>
    /// <param name="cancellationToken">A token used to cancel metadata processing.</param>
    /// <returns>A task containing the enriched entries and recoverable issues.</returns>
    Task<FileMetadataResult> ReadAsync(
        IReadOnlyCollection<FileEntry> files,
        CancellationToken cancellationToken = default);
}
