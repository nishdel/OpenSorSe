namespace OpenSorSe.Scanner.Models;

/// <summary>Summarizes work performed by file hashing.</summary>
public sealed record FileHashStatistics(long FilesProcessed, long FilesHashed, long BytesHashed, long IssuesEncountered);
