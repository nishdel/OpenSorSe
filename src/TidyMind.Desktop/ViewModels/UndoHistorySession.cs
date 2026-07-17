using TidyMind.Executor.Models;

namespace TidyMind.Desktop.ViewModels;

/// <summary>
/// Represents caller-supplied undo records from one completed execution session.
/// </summary>
/// <param name="SessionId">The stable caller-supplied session identifier.</param>
/// <param name="CompletedAtUtc">The UTC time at which the source execution session completed.</param>
/// <param name="Records">The ordered explicit undo records available for review.</param>
public sealed record UndoHistorySession(string SessionId, DateTimeOffset CompletedAtUtc, IReadOnlyList<UndoRecord> Records);
