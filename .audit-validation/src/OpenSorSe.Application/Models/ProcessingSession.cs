namespace OpenSorSe.Application.Models;

/// <summary>Provides immutable current state for one non-persistent processing session.</summary>
/// <param name="Id">The unique process-local session identifier.</param>
/// <param name="StartedAtUtc">The UTC session start time.</param>
/// <param name="CompletedAtUtc">The optional UTC terminal time.</param>
/// <param name="Status">The current session status.</param>
/// <param name="FailureMessage">The optional user-safe terminal failure message.</param>
public sealed record ProcessingSession(string Id, DateTimeOffset StartedAtUtc, DateTimeOffset? CompletedAtUtc, ProcessingSessionStatus Status, string? FailureMessage);
