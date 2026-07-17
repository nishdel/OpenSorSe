namespace OpenSorSe.Scanner.Models;

/// <summary>Identifies a recoverable hashing issue.</summary>
public enum FileHashIssueKind
{
    /// <summary>The file path was invalid or unavailable.</summary>
    FileUnavailable,
    /// <summary>Access to the file was denied.</summary>
    AccessDenied,
    /// <summary>The file changed during best-effort hashing validation.</summary>
    FileChangedDuringHashing,
    /// <summary>The file stream could not be opened or read.</summary>
    FileUnreadable,
    /// <summary>The entry was not a regular file.</summary>
    NonRegularFileSkipped,
    /// <summary>The entry was a symbolic link, junction, or other reparse point.</summary>
    ReparsePointSkipped,
}
