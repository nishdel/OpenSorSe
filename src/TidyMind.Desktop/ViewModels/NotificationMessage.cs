namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Represents one immutable notification currently displayed by the presentation layer.
/// </summary>
/// <param name="Id">The deterministic notification identifier for the current process.</param>
/// <param name="Severity">The user-facing severity.</param>
/// <param name="Message">The user-safe message text.</param>
/// <param name="CreatedAtUtc">The UTC creation time.</param>
/// <param name="ExpiresAtUtc">The optional UTC automatic-dismissal time.</param>
public sealed record NotificationMessage(string Id, NotificationSeverity Severity, string Message, DateTimeOffset CreatedAtUtc, DateTimeOffset? ExpiresAtUtc);
