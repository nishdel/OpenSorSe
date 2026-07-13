namespace TidyMind.Core.Errors;

/// <summary>
/// Describes the impact of an application error.
/// </summary>
public enum ApplicationErrorSeverity
{
    /// <summary>A non-critical diagnostic condition.</summary>
    Information,

    /// <summary>A recoverable unexpected condition.</summary>
    Warning,

    /// <summary>A failed operation that allows the application to continue.</summary>
    Error,

    /// <summary>A failure that prevents safe continuation.</summary>
    Critical,
}
