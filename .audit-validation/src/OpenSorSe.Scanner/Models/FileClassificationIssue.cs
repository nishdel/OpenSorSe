namespace OpenSorSe.Scanner.Models;
/// <summary>Describes a recoverable classification issue.</summary>
public sealed record FileClassificationIssue(string FilePath, FileClassificationIssueKind Kind, string Message);
