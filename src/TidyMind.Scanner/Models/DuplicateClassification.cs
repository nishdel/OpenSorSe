namespace TidyMind.Scanner.Models;

/// <summary>
/// Contains the recalculated exact-content duplicate state of a file entry.
/// </summary>
/// <param name="Status">The duplicate status assigned to the entry.</param>
/// <param name="GroupId">The deterministic duplicate-group identifier when the status is <see cref="DuplicateStatus.Duplicate"/>.</param>
public sealed record DuplicateClassification(DuplicateStatus Status, string? GroupId = null);
