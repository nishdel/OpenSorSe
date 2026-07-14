namespace TidyMind.Scanner.Models;

/// <summary>Contains hashed entries and recoverable hashing issues.</summary>
public sealed record FileHashResult(IReadOnlyList<FileEntry> Files, FileHashStatistics Statistics, IReadOnlyList<FileHashIssue> Issues);
