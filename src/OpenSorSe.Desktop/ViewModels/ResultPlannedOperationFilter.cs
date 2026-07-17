namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Selects whether results with accepted planned operations are shown.
/// </summary>
public enum ResultPlannedOperationFilter
{
    /// <summary>Shows results regardless of planned-operation presence.</summary>
    All,

    /// <summary>Shows results with one or more accepted planned operations.</summary>
    HasAcceptedOperation,

    /// <summary>Shows results with no accepted planned operation.</summary>
    NoAcceptedOperation,
}
