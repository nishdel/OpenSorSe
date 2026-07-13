namespace TidyMind.Core.Errors;

/// <summary>
/// Contains diagnostics for an error reported through the shared error handler.
/// </summary>
public sealed class ApplicationError : EventArgs
{
    /// <summary>
    /// Initializes an application error.
    /// </summary>
    /// <param name="source">The subsystem that reported the error.</param>
    /// <param name="message">A user-safe description of the failure.</param>
    /// <param name="severity">The impact of the failure.</param>
    /// <param name="exception">The optional underlying exception.</param>
    public ApplicationError(
        string source,
        string message,
        ApplicationErrorSeverity severity,
        Exception? exception = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Source = source;
        Message = message;
        Severity = severity;
        Exception = exception;
    }

    /// <summary>Gets the subsystem that reported the error.</summary>
    public string Source { get; }

    /// <summary>Gets the user-safe error message.</summary>
    public string Message { get; }

    /// <summary>Gets the error impact.</summary>
    public ApplicationErrorSeverity Severity { get; }

    /// <summary>Gets the optional underlying exception.</summary>
    public Exception? Exception { get; }
}
