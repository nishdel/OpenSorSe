namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Represents entries that share a normalized supported content hash.
/// </summary>
/// <param name="GroupId">The deterministic group identifier.</param>
/// <param name="Algorithm">The normalized hash algorithm name.</param>
/// <param name="HashValue">The normalized lowercase hexadecimal hash value.</param>
/// <param name="Files">The enriched entries belonging to the group in input order.</param>
public sealed record DuplicateGroup(string GroupId, string Algorithm, string HashValue, IReadOnlyList<FileEntry> Files);
