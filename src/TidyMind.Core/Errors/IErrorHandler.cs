namespace TidyMind.Core.Errors;

/// <summary>
/// Reports cross-cutting application errors without implementing user notifications.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Occurs after an error is reported.
    /// </summary>
    event EventHandler<ApplicationError>? ErrorReported;

    /// <summary>
    /// Records and broadcasts an application error.
    /// </summary>
    /// <param name="applicationError">The error to report.</param>
    void Report(ApplicationError applicationError);
}
