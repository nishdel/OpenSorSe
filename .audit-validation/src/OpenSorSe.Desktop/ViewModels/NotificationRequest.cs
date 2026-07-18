namespace OpenSorSe.Desktop.ViewModels;

/// <summary>
/// Describes a non-blocking notification requested by another presentation component.
/// </summary>
/// <param name="Severity">The user-facing notification severity.</param>
/// <param name="Message">The user-safe notification message.</param>
/// <param name="Lifetime">An optional positive display lifetime; null requires manual dismissal.</param>
public sealed record NotificationRequest(NotificationSeverity Severity, string Message, TimeSpan? Lifetime = null);
