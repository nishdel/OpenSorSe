using Microsoft.Extensions.Logging;
using TidyMind.Core.Logging;

namespace TidyMind.Core.Errors;

/// <summary>
/// Logs application errors and exposes them to interested presentation components.
/// </summary>
public sealed class ErrorHandler : IErrorHandler
{
    private readonly ILoggingService _loggingService;

    /// <summary>
    /// Initializes an error handler that uses centralized logging.
    /// </summary>
    /// <param name="loggingService">The logging service used for diagnostic output.</param>
    public ErrorHandler(ILoggingService loggingService)
    {
        _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
    }

    /// <inheritdoc />
    public event EventHandler<ApplicationError>? ErrorReported;

    /// <inheritdoc />
    public void Report(ApplicationError applicationError)
    {
        ArgumentNullException.ThrowIfNull(applicationError);
        var logger = _loggingService.CreateLogger(applicationError.Source);
        logger.Log(MapSeverity(applicationError.Severity), applicationError.Exception, "{Message}", applicationError.Message);
        ErrorReported?.Invoke(this, applicationError);
    }

    private static LogLevel MapSeverity(ApplicationErrorSeverity severity) => severity switch
    {
        ApplicationErrorSeverity.Information => LogLevel.Information,
        ApplicationErrorSeverity.Warning => LogLevel.Warning,
        ApplicationErrorSeverity.Error => LogLevel.Error,
        ApplicationErrorSeverity.Critical => LogLevel.Critical,
        _ => LogLevel.Error,
    };
}
