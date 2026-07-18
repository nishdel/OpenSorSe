namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Contains read-only filesystem metadata for a discovered file. Attributes are present whenever metadata is present.
/// </summary>
public sealed record FileMetadata(
    string FileName,
    string Extension,
    long? SizeInBytes,
    DateTimeOffset? CreationTimeUtc,
    DateTimeOffset? LastWriteTimeUtc,
    DateTimeOffset? LastAccessTimeUtc,
    FileAttributes Attributes);
