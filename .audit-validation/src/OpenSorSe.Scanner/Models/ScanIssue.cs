namespace OpenSorSe.Scanner.Models;

/// <summary>
/// Describes a recoverable issue that prevented one filesystem location from being scanned.
/// </summary>
public sealed record ScanIssue(string Path, ScanIssueKind Kind, string Message);
