namespace TidyMind.Scanner.Models;

/// <summary>
/// Identifies a recoverable filesystem condition encountered during scanning.
/// </summary>
public enum ScanIssueKind
{
    /// <summary>A requested root could not be reached as a directory.</summary>
    RootDirectoryUnavailable,

    /// <summary>A directory could not be enumerated.</summary>
    DirectoryUnavailable,

    /// <summary>An individual filesystem entry could not be inspected.</summary>
    EntryUnavailable,

    /// <summary>Access to a filesystem location was denied.</summary>
    AccessDenied,

    /// <summary>A reparse point was skipped because symbolic-link support is deferred.</summary>
    SymbolicLinkSkipped,
}
