namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Identifies a recoverable issue encountered while reading file metadata.
/// </summary>
public enum FileMetadataIssueKind
{
    /// <summary>The file was missing, invalid, or otherwise unavailable.</summary>
    FileUnavailable,

    /// <summary>Access to the file or its metadata was denied.</summary>
    AccessDenied,

    /// <summary>The entry was a symbolic link, junction, or other reparse point and was skipped.</summary>
    ReparsePointSkipped,

    /// <summary>One or more optional metadata fields could not be retrieved.</summary>
    MetadataUnavailable,
}
