namespace TidyMind.Scanner.Models;
/// <summary>Contains classified entries.</summary>
public sealed record FileClassificationResult(IReadOnlyList<FileEntry> Files, FileClassificationStatistics Statistics, IReadOnlyList<FileClassificationIssue> Issues);
