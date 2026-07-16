namespace TidyMind.Scanner.Models;
/// <summary>Defines one deterministic classification match.</summary>
public sealed record FileClassificationRule(string Id, FileClassificationMatchKind MatchKind, string Pattern, FileCategory Category);
