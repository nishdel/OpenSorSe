namespace OpenSorSe.Application.Models;

/// <summary>
/// Represents one user-safe issue carried into a completed result snapshot.
/// </summary>
public sealed record ResultIssue(
    string SourceStage,
    ResultIssueSeverity Severity,
    string Message,
    string? AssociatedFileId = null);
