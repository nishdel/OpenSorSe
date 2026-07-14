namespace TidyMind.Scanner.Models;

/// <summary>
/// Describes the final outcome of a scan operation.
/// </summary>
public enum ScanStatus
{
    /// <summary>The scan traversed all reachable requested locations.</summary>
    Completed,

    /// <summary>The scan ended after a cancellation request.</summary>
    Cancelled,
}
