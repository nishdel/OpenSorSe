namespace TidyMind.Application.Models;

/// <summary>Contains a terminal session snapshot and optional reached pipeline result.</summary>
/// <param name="Session">The terminal session snapshot.</param>
/// <param name="Processing">The optional pipeline result reached before completion or cancellation.</param>
public sealed record ProcessingSessionResult(ProcessingSession Session, ProcessingResult? Processing);
