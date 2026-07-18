namespace OpenSorSe.Application.CatalogComparison;

/// <summary>Describes whether two captured catalog source scopes are comparable.</summary>
public enum CatalogScopeMatch
{
    /// <summary>At least one legacy or incomplete entry has no captured source roots.</summary>
    Unknown,

    /// <summary>The normalized source-root sets are equal.</summary>
    Same,

    /// <summary>The normalized source-root sets differ.</summary>
    Different,
}
