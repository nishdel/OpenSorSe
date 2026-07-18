namespace OpenSorSe.Rules.Models;

/// <summary>
/// Identifies a recoverable per-decision action-planning problem.
/// </summary>
public enum ActionPlanningIssueKind
{
    /// <summary>The decision does not provide valid planning input.</summary>
    InvalidDecision,
    /// <summary>Required metadata is unavailable.</summary>
    MissingMetadata,
    /// <summary>A destination path is invalid for lexical planning.</summary>
    InvalidDestinationPath,
    /// <summary>A rename template is invalid.</summary>
    InvalidNameTemplate,
    /// <summary>The normalized source and destination paths are equal.</summary>
    SourceEqualsDestination,
    /// <summary>The action kind is not supported by this planner.</summary>
    UnsupportedAction,
}
