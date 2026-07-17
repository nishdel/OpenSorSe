namespace OpenSorSe.Application.Models;

/// <summary>
/// Describes the display severity of an immutable result issue.
/// </summary>
public enum ResultIssueSeverity
{
    /// <summary>Describes a recoverable limitation or warning.</summary>
    Warning,

    /// <summary>Describes a result-data problem that leaves the remaining snapshot usable.</summary>
    Error,
}
