namespace TidyMind.Scanner.Models;

/// <summary>
/// Defines the root directories and options for one scan operation.
/// </summary>
public sealed record ScanRequest(IReadOnlyCollection<string> RootDirectories, ScanOptions Options);
