namespace OpenSorSe.Rules.Models;

/// <summary>
/// Identifies a filesystem operation proposed for later execution.
/// </summary>
public enum PlannedOperationKind
{
    /// <summary>A planned move operation.</summary>
    Move,
    /// <summary>A planned copy operation.</summary>
    Copy,
    /// <summary>A planned rename operation.</summary>
    Rename,
    /// <summary>A planned delete operation.</summary>
    Delete,
}
