namespace TidyMind.Scanner.Models;

/// <summary>
/// Describes the exact-content duplicate state of a file entry.
/// </summary>
public enum DuplicateStatus
{
    /// <summary>No supported hash is available for the entry.</summary>
    Unknown,

    /// <summary>The entry has a valid hash occurring exactly once.</summary>
    Unique,

    /// <summary>The entry has a valid hash shared by multiple entries.</summary>
    Duplicate,
}
