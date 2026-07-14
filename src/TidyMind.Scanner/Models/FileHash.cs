namespace TidyMind.Scanner.Models;

/// <summary>Contains a normalized file fingerprint.</summary>
public sealed record FileHash(string Algorithm, string Value);
