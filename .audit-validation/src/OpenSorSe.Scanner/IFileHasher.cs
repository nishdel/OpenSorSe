using OpenSorSe.Scanner.Models;

namespace OpenSorSe.Scanner;

/// <summary>Generates SHA-256 fingerprints for eligible file entries.</summary>
public interface IFileHasher
{
    /// <summary>Hashes supplied entries while preserving input order and multiplicity.</summary>
    /// <param name="files">The entries to hash.</param>
    /// <param name="cancellationToken">A token used to cancel hashing.</param>
    /// <returns>A task containing hashing results.</returns>
    Task<FileHashResult> HashAsync(IReadOnlyCollection<FileEntry> files, CancellationToken cancellationToken = default);
}
