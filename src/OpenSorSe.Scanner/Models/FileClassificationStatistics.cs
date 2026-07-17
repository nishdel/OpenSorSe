namespace OpenSorSe.Scanner.Models;
/// <summary>Summarizes classification work.</summary>
public sealed record FileClassificationStatistics(long FilesProcessed, long FilesClassified, long FilesUnknown, long IssuesEncountered);
