namespace OpenSorSe.Rules.Models;

/// <summary>
/// Identifies a proposed future action without executing it.
/// </summary>
public enum RuleActionKind
{
    /// <summary>No action is proposed.</summary>
    NoAction,
    /// <summary>A future move action is proposed.</summary>
    Move,
    /// <summary>A future copy action is proposed.</summary>
    Copy,
    /// <summary>A future rename action is proposed.</summary>
    Rename,
    /// <summary>A future delete action is proposed.</summary>
    Delete,
}
