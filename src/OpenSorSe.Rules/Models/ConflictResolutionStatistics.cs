namespace OpenSorSe.Rules.Models;

/// <summary>
/// Contains counts produced by intra-plan conflict resolution.
/// </summary>
/// <param name="OperationsProcessed">The number of input operations examined.</param>
/// <param name="OperationsAccepted">The number of returned accepted operations.</param>
/// <param name="OperationsRejected">The number of operations producing an issue.</param>
/// <param name="MoveOperationsAccepted">The number of accepted move operations.</param>
/// <param name="CopyOperationsAccepted">The number of accepted copy operations.</param>
/// <param name="RenameOperationsAccepted">The number of accepted rename operations.</param>
/// <param name="DeleteOperationsAccepted">The number of accepted delete operations.</param>
/// <param name="DuplicateOperationIds">The number of duplicate operation identifier issues.</param>
/// <param name="DuplicateOperations">The number of duplicate operation signature issues.</param>
/// <param name="DestinationConflicts">The number of destination conflict issues.</param>
/// <param name="SourceConflicts">The number of source conflict issues.</param>
/// <param name="IssuesEncountered">The total number of returned issues.</param>
public sealed record ConflictResolutionStatistics(long OperationsProcessed, long OperationsAccepted, long OperationsRejected, long MoveOperationsAccepted, long CopyOperationsAccepted, long RenameOperationsAccepted, long DeleteOperationsAccepted, long DuplicateOperationIds, long DuplicateOperations, long DestinationConflicts, long SourceConflicts, long IssuesEncountered);
