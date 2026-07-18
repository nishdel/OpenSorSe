namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Identifies a recoverable hash-validation problem during duplicate detection.
/// </summary>
public enum DuplicateDetectionIssueKind
{
    /// <summary>No hash was available for the entry.</summary>
    HashUnavailable,

    /// <summary>The entry used a hash algorithm unsupported by this detector.</summary>
    UnsupportedHashAlgorithm,

    /// <summary>The entry had a malformed SHA-256 hash value.</summary>
    InvalidHashValue,
}
