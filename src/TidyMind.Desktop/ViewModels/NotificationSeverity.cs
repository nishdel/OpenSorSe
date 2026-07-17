namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Identifies the user-facing severity of a non-blocking notification.
/// </summary>
public enum NotificationSeverity
{
    /// <summary>
    /// Describes neutral informational feedback.
    /// </summary>
    Information,

    /// <summary>
    /// Describes successful completion feedback.
    /// </summary>
    Success,

    /// <summary>
    /// Describes a recoverable warning.
    /// </summary>
    Warning,

    /// <summary>
    /// Describes a user-visible error.
    /// </summary>
    Error,
}
