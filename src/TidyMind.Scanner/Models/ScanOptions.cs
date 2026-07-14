namespace TidyMind.Scanner.Models;

/// <summary>
/// Defines scanner behavior that is configurable for a single operation.
/// </summary>
public sealed record ScanOptions(TimeSpan ProgressReportInterval)
{
    /// <summary>
    /// Gets the default scanner options.
    /// </summary>
    public static ScanOptions Default { get; } = new(TimeSpan.FromMilliseconds(250));
}
