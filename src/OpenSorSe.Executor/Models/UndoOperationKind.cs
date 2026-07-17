namespace OpenSorSe.Executor.Models;

/// <summary>Identifies the reverse behavior represented by an undo record.</summary>
public enum UndoOperationKind
{
    /// <summary>Moves a result file back to its original path.</summary>
    Move,
    /// <summary>Deletes a copied result file.</summary>
    Copy,
    /// <summary>Renames a result file back to its original path.</summary>
    Rename,
}
